using Signum.Entities.Translation;
using System.Globalization;
using Signum.Utilities.Reflection;
using Signum.Entities.Basics;
using System.Collections.Concurrent;
using System.IO;
using Signum.Engine.Excel;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.Intrinsics.Arm;

namespace Signum.Engine.Translation;

public static class TranslatedInstanceLogic
{
    static Func<CultureInfo> getDefaultCulture = null!;
    public static CultureInfo DefaultCulture { get { return getDefaultCulture(); } }

    public static Dictionary<Type, Dictionary<PropertyRoute, TranslateableRouteType>> TranslateableRoutes 
        = new Dictionary<Type, Dictionary<PropertyRoute, TranslateableRouteType>>();
    static ResetLazy<Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceEntity>>> LocalizationCache = null!;

    public static void Start(SchemaBuilder sb, Func<CultureInfo> defaultCulture)
    {
        if (sb.NotDefined(MethodInfo.GetCurrentMethod()))
        {
            sb.Include<TranslatedInstanceEntity>()
                .WithUniqueIndex(ti => new { ti.Culture, ti.PropertyRoute, ti.Instance, ti.RowId })
                .WithQuery(() => e => new
                {
                    Entity = e,
                    e.Id,
                    e.Culture,
                    e.PropertyRoute,
                    e.PropertyRoute.RootType,
                    e.Instance,
                    e.RowId,
                    e.TranslatedText,
                    e.OriginalText,
                });
            TranslatedInstanceLogic.getDefaultCulture = defaultCulture ?? throw new ArgumentNullException(nameof(defaultCulture));

            LocalizationCache = sb.GlobalLazy(() =>
                Database.Query<TranslatedInstanceEntity>()
                .ToList()
                .AgGroupToDictionary(a => a.Culture.ToCultureInfo(),
                gr2 => gr2.GroupBy(a => a.PropertyRoute)
                    .SelectMany(gr =>
                    {
                        PropertyRoute pr = gr.Key.ToPropertyRoute();

                        PropertyRoute? mListRoute = pr.GetMListItemsRoute();
                        if (mListRoute == null)
                            return gr.Select(ti => KeyValuePair.Create(new LocalizedInstanceKey(pr, ti.Instance, null), ti));

                        Type type = ((FieldMList)Schema.Current.Field(mListRoute.Parent!)).TableMList.PrimaryKey.Type;

                        return gr.Select(ti => KeyValuePair.Create(new LocalizedInstanceKey(pr, ti.Instance, new PrimaryKey((IComparable)ReflectionTools.Parse(ti.RowId!, type)!)), ti));

                    }).ToDictionary())
                    , new InvalidateWith(typeof(TranslatedInstanceEntity)));

            sb.Schema.SchemaCompleted += () =>
            {
                var s = Schema.Current;

                var prs = (from t in s.Tables.Keys
                           from pr in PropertyRoute.GenerateRoutes(t)
                           where pr.PropertyRouteType == PropertyRouteType.FieldOrProperty && pr.FieldInfo != null && pr.FieldInfo.FieldType == typeof(string) &&
                           s.Settings.FieldAttribute<TranslateFieldAttribute>(pr) != null &&
                           s.Settings.FieldAttribute<IgnoreAttribute>(pr) == null
                           select KeyValuePair.Create(pr, s.Settings.FieldAttribute<TranslateFieldAttribute>(pr)!.TranslatableRouteType)).ToList();

                foreach (var kvp in prs)
                {
                    AddRoute(kvp.Key, kvp.Value);
                }
            };
        }
    }

    public static void AddRoute<T, S>(Expression<Func<T, S>> propertyRoute, TranslateableRouteType type = TranslateableRouteType.Text) where T : Entity
    {
        AddRoute(PropertyRoute.Construct<T, S>(propertyRoute), type);
    }

    public static void AddRoute(PropertyRoute route, TranslateableRouteType type = TranslateableRouteType.Text)
    {
        if (route.PropertyRouteType != PropertyRouteType.FieldOrProperty)
            throw new InvalidOperationException("Routes of type {0} can not be traducibles".FormatWith(route.PropertyRouteType));

        if (route.Type != typeof(string))
            throw new InvalidOperationException("Only string routes can be traducibles");

        TranslateableRoutes.GetOrCreate(route.RootType).Add(route, type); 
    }

    public static TranslateableRouteType? RouteType(PropertyRoute route)
    {
        var dic = TranslateableRoutes.TryGetC(route.RootType);

        return dic?.TryGetS(route); 
    }

    public static List<TranslatedTypeSummary> TranslationInstancesStatus()
    {
        var cultures = TranslationLogic.CurrentCultureInfos(DefaultCulture);

        return (from type in TranslateableRoutes.Keys
                from ci in cultures
                select new TranslatedTypeSummary
                {
                    Type = type,
                    CultureInfo = ci,
                    State = ci.IsNeutralCulture && ci.Name != DefaultCulture.Name ? GetState(type, ci) : null,
                }).ToList();

    }


    public static Dictionary<LocalizedInstanceKey, string?> FromEntities(Type type)
    {
        Dictionary<LocalizedInstanceKey, string?>? result = null;

        foreach (var pr in TranslateableRoutes.GetOrThrow(type).Keys)
        {
            var mlist = pr.GetMListItemsRoute();

            var dic = mlist == null ?
                giFromRoute.GetInvoker(type)(pr) :
                giFromRouteMList.GetInvoker(type, mlist.Type)(pr);

            if (result == null)
                result = dic;
            else
                result.AddRange(dic);
        }

        return result!;
    }

    static GenericInvoker<Func<PropertyRoute, Dictionary<LocalizedInstanceKey, string?>>> giFromRoute =
        new(pr => FromRoute<Entity>(pr));
    static Dictionary<LocalizedInstanceKey, string?> FromRoute<T>(PropertyRoute pr) where T : Entity
    {
        var selector = pr.GetLambdaExpression<T, string?>(safeNullAccess: false);

        return (from e in Database.Query<T>()
                select KeyValuePair.Create(new LocalizedInstanceKey(pr, e.ToLite(), null), selector.Evaluate(e))).ToDictionary();
    }

    static GenericInvoker<Func<PropertyRoute, Dictionary<LocalizedInstanceKey, string?>>> giFromRouteMList =
        new(pr => FromRouteMList<Entity, EmbeddedEntity>(pr));
    static Dictionary<LocalizedInstanceKey, string?> FromRouteMList<T, M>(PropertyRoute pr) where T : Entity
    {
        var mlItemPr = pr.GetMListItemsRoute()!;
        var mListProperty = mlItemPr.Parent!.GetLambdaExpression<T, MList<M>>(safeNullAccess: false);
        var selector = pr.GetLambdaExpression<M, string>(safeNullAccess: false, skipBefore: mlItemPr);

        return (from mle in Database.MListQuery(mListProperty)
                select KeyValuePair.Create(new LocalizedInstanceKey(pr, mle.Parent.ToLite(), mle.RowId), selector.Evaluate(mle.Element))).ToDictionary();
    }

    public static Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceEntity>> TranslationsForType(Type type, CultureInfo? culture)
    {
        return LocalizationCache.Value.Where(c => culture == null || c.Key.Equals(culture)).ToDictionary(
            kvp => kvp.Key,
            kvp => kvp.Value.Where(a => a.Key.Route.RootType == type).ToDictionary());
    }

    static TranslatedSummaryState? GetState(Type type, CultureInfo ci)
    {
        using (HeavyProfiler.LogNoStackTrace("GetState", () => type.Name + " " + ci.Name))
        {
            if (!TranslateableRoutes.GetOrThrow(type).Keys.Any(pr => AnyNoTranslated(pr, ci)))
                return TranslatedSummaryState.Completed;

            if (Database.Query<TranslatedInstanceEntity>().Count(ti => ti.PropertyRoute.RootType.Is(type.ToTypeEntity()) && ti.Culture.Is(ci.ToCultureInfoEntity())) == 0)
                return TranslatedSummaryState.None;

            return TranslatedSummaryState.Pending;
        }
    }

    public static bool AnyNoTranslated(PropertyRoute pr, CultureInfo ci)
    {
        var mlist = pr.GetMListItemsRoute();

        if (mlist == null)
            return giAnyNoTranslated.GetInvoker(pr.RootType)(pr, ci);

        return giAnyNoTranslatedMList.GetInvoker(pr.RootType, mlist.Type)(pr, ci); 
    }

    static GenericInvoker<Func<PropertyRoute, CultureInfo, bool>> giAnyNoTranslated =
        new((pr, ci) => AnyNoTranslated<Entity>(pr, ci));
    static bool AnyNoTranslated<T>(PropertyRoute pr, CultureInfo ci) where T : Entity
    {
        var exp = pr.GetLambdaExpression<T, string?>(safeNullAccess: false);

        return (from e in Database.Query<T>()
                let str = exp.Evaluate(e)
                let ti = Database.Query<TranslatedInstanceEntity>().SingleOrDefault(ti =>
                    ti.Instance.Is(e) &
                    ti.PropertyRoute.IsPropertyRoute(pr) &&
                    ti.Culture.Is(ci.ToCultureInfoEntity()))

                where (str ?? "") != (ti!.OriginalText ?? "")
                select e).Any();
    }

    static GenericInvoker<Func<PropertyRoute, CultureInfo, bool>> giAnyNoTranslatedMList =
       new((pr, ci) => AnyNoTranslatedMList<Entity, string>(pr, ci));
    static bool AnyNoTranslatedMList<T, M>(PropertyRoute pr, CultureInfo ci) where T : Entity
    {
        var mlistItemPr = pr.GetMListItemsRoute()!;
        var mListProperty = mlistItemPr.Parent!.GetLambdaExpression<T, MList<M>>(safeNullAccess: false);

        var exp = pr.GetLambdaExpression<M, string?>(safeNullAccess: false, skipBefore: mlistItemPr);

        return (from mle in Database.MListQuery(mListProperty)
                let str = exp.Evaluate(mle.Element)
                let ti = Database.Query<TranslatedInstanceEntity>().SingleOrDefaultEx(ti =>
                    ti.Instance.Is(mle.Parent) &&
                    ti.PropertyRoute.IsPropertyRoute(pr) &&
                    ti.RowId == mle.RowId.ToString() &&
                    ti.Culture.Is(ci.ToCultureInfoEntity()))
                where (str ?? "") != (ti!.OriginalText ?? "")
                select mle)
                .Any();
    }

    public static int CleanTranslations(Type t)
    {
        var routeDic = TranslateableRoutes.GetOrThrow(t).Keys.ToDictionary(r => r, r => r.ToPropertyRouteEntity()).Where(kvp => !kvp.Value.IsNew).ToDictionary();

        int deletedPr = Database.Query<TranslatedInstanceEntity>().Where(a => a.PropertyRoute.RootType.Is(t.ToTypeEntity()) && !routeDic.Values.Contains(a.PropertyRoute)).UnsafeDelete();
        int deleteInconsistent = Database.Query<TranslatedInstanceEntity>().Where(a => a.PropertyRoute.RootType.Is(t.ToTypeEntity()) && (a.RowId != null) != a.PropertyRoute.Path.Contains("/")).UnsafeDelete();

        var routeGroups = routeDic.GroupBy(kvp => kvp.Key.GetMListItemsRoute());

        var mainGroup = routeGroups.SingleOrDefaultEx(a => a.Key == null);

        int deletedInstance = mainGroup == null ? 0 : giRemoveTranslationsForMissingEntities.GetInvoker(t)(mainGroup.ToDictionary());

        var deletedMList = 0; 
        foreach (var gr in routeGroups.Where(a => a.Key != null))
        {
            deletedMList += giRemoveTranslationsForMissingRowIds.GetInvoker(t, gr.Key!.Parent!.Type.ElementType()!)(gr.Key, gr.ToDictionary());
        }

        return deletedPr + deleteInconsistent + deletedInstance + deletedMList;
    }

    static GenericInvoker<Func<Dictionary<PropertyRoute, PropertyRouteEntity>, int>> giRemoveTranslationsForMissingEntities = new(dic => RemoveTranslationsForMissingEntities<Entity>(dic));
    static int RemoveTranslationsForMissingEntities<T>(Dictionary<PropertyRoute, PropertyRouteEntity> routes) where T : Entity
    {
        var result = (from ti in Database.Query<TranslatedInstanceEntity>()
                                where ti.PropertyRoute.RootType.Is(typeof(T).ToTypeEntity())
                                join e in Database.Query<T>().DefaultIfEmpty() on ti.Instance.Entity equals e
                                where e == null
                                select ti).UnsafeDelete();

        foreach (var item in routes)
        {
            if(((IColumn)Schema.Current.Field(item.Key)).Nullable != IsNullable.No)
            {
                var exp = item.Key.GetLambdaExpression<T, string?>(false, null);

                result += (from ti in Database.Query<TranslatedInstanceEntity>()
                           where ti.PropertyRoute.Is(item.Value)
                           join e in Database.Query<T>().DefaultIfEmpty() on ti.Instance.Entity equals e
                           where exp.Evaluate(e) == null || exp.Evaluate(e) == ""
                           select ti).UnsafeDelete();
            }
        }

        return result;

    }

    static GenericInvoker<Func<PropertyRoute, Dictionary<PropertyRoute, PropertyRouteEntity>, int>> giRemoveTranslationsForMissingRowIds = new((pr, dic) => RemoveTranslationsForMissingRowIds<Entity, EmbeddedEntity>(pr, dic));
    static int RemoveTranslationsForMissingRowIds<T, E>(PropertyRoute mlistRoute, Dictionary<PropertyRoute, PropertyRouteEntity> routes) where T : Entity
    {
        Expression<Func<T, MList<E>>> expression = mlistRoute.Parent!.GetLambdaExpression<T, MList<E>>(false);
        var prefix = mlistRoute.PropertyString();
        var result = (from ti in Database.Query<TranslatedInstanceEntity>()
                      where ti.PropertyRoute.RootType.Is(typeof(T).ToTypeEntity()) && ti.PropertyRoute.Path.StartsWith(prefix)
                      join mle in Database.MListQuery(expression).DefaultIfEmpty() on new { ti.Instance.Entity, ti.RowId } equals new { Entity = (Entity)mle.Parent, RowId = mle.RowId.ToString() }
                      where mle == null
                      select ti).UnsafeDelete();

        foreach (var item in routes)
        {
            if (((IColumn)Schema.Current.Field(item.Key)).Nullable != IsNullable.No)
            {
                var exp = item.Key.GetLambdaExpression<E, string?>(false, mlistRoute);

                result += (from ti in Database.Query<TranslatedInstanceEntity>()
                           where ti.PropertyRoute.Is(item.Value)
                           join mle in Database.MListQuery(expression).DefaultIfEmpty() on new { ti.Instance.Entity, ti.RowId } equals new { Entity = (Entity)mle.Parent, RowId = mle.RowId.ToString() }
                           where exp.Evaluate(mle.Element) == null || exp.Evaluate(mle.Element) == ""
                           select ti).UnsafeDelete();
            }
        }

        return result;
    }

    public static string TranslatedField<T>(this T entity, Expression<Func<T, string>> property) where T : Entity
    {
        string? fallbackString = TranslatedInstanceLogic.GetPropertyRouteAccesor(property)(entity);

        var pr = PropertyRoute.Construct(property);

        return entity.ToLite().TranslatedField(pr, fallbackString);
    }

    public static string? TranslatedFieldNullable<T>(this T entity, Expression<Func<T, string?>> property) where T : Entity
    {
        string? fallbackString = TranslatedInstanceLogic.GetPropertyRouteAccesor(property)(entity);

        var pr = PropertyRoute.Construct(property);

        return entity.ToLite().TranslatedField(pr, fallbackString);
    }

    public static IEnumerable<TranslatableElement<T>> TranslatedMList<E, T>(this E entity, Expression<Func<E, MList<T>>> mlistProperty) where E : Entity
    {
        var mlist = GetPropertyRouteAccesor(mlistProperty);

        PropertyRoute route = PropertyRoute.Construct(mlistProperty).Add("Item");

        var lite = entity.ToLite();

        foreach (var item in ((IMListPrivate<T>)mlist(entity)).InnerList)
        {
            yield return new TranslatableElement<T>(lite, route, item);
        }
    }

    public static string TranslatedElement<T>(this TranslatableElement<T> element, Expression<Func<T, string>> property)
    {
        string fallback = GetPropertyRouteAccesor(property)(element.Value);

        PropertyRoute route = element.ElementRoute.Continue(property);

        return TranslatedField(element.Lite, route, element.RowId, fallback);
    }

    public static string? TranslatedElementNullable<T>(this TranslatableElement<T> element, Expression<Func<T, string?>> property)
    {
        string? fallback = GetPropertyRouteAccesor(property)(element.Value);
        
        PropertyRoute route = element.ElementRoute.Continue(property); 

        return TranslatedField(element.Lite, route, element.RowId, fallback);
    }

    [return: NotNullIfNotNull("fallbackString")]
    public static string? TranslatedField<T>(this Lite<T> lite, Expression<Func<T, string?>> property, string? fallbackString) where T : Entity
    {
        PropertyRoute route = PropertyRoute.Construct(property);

        return TranslatedField(lite, route, fallbackString);
    }


    [return: NotNullIfNotNull("fallbackString")]
    public static string? TranslatedField(this Lite<Entity> lite, PropertyRoute route, string? fallbackString)
    {
        return TranslatedField(lite, route, null, fallbackString);
    }

    [return: NotNullIfNotNull("fallbackString")]
    public static string? TranslatedField(Lite<Entity> lite, PropertyRoute route, PrimaryKey? rowId, string? fallbackString)
    {
        var result = TranslatedInstanceLogic.GetTranslatedInstance(lite, route, rowId);

        if (result != null && (fallbackString  == null || result.OriginalText.Replace("\r", "").Replace("\n", "") == fallbackString.Replace("\r", "").Replace("\n", "")))
            return result.TranslatedText;

        return fallbackString;
    }


    public static TranslatedInstanceEntity? GetTranslatedInstance(Lite<Entity> lite, PropertyRoute route, PrimaryKey? rowId)
    {
        var hastMList = route.GetMListItemsRoute() != null;

        if (hastMList && !rowId.HasValue)
            throw new InvalidOperationException("Route {0} has MList so rowId should have a value".FormatWith(route));

        if (!hastMList && rowId.HasValue)
            throw new InvalidOperationException("Route {0} has not MList so rowId should be null".FormatWith(route));

        if (route.RootType != lite.EntityType)
            throw new InvalidOperationException("Route {0} belongs to type {1}, not {2}".FormatWith(route, route.RootType.TypeName(), lite.EntityType.TypeName()));

        var key = new LocalizedInstanceKey(route, lite, rowId);

        var result = LocalizationCache.Value.TryGetC(CultureInfo.CurrentUICulture)?.TryGetC(key);

        if (result != null)
            return result;

        if (CultureInfo.CurrentUICulture.IsNeutralCulture)
            return null;

        result = LocalizationCache.Value.TryGetC(CultureInfo.CurrentUICulture.Parent)?.TryGetC(key);

        if (result != null)
            return result;

        return null;
    }


    public static T SaveTranslation<T>(this T entity, CultureInfoEntity ci, Expression<Func<T, string?>> propertyRoute, string? translatedText)
        where T : Entity
    {
        entity.Save();

        if (translatedText.HasText())
            new TranslatedInstanceEntity
            {
                PropertyRoute = PropertyRoute.Construct(propertyRoute).ToPropertyRouteEntity(),
                Culture = ci,
                TranslatedText = translatedText,
                OriginalText = GetPropertyRouteAccesor(propertyRoute)(entity)!,
                Instance = entity.ToLite(),
            }.Save();

        return entity;
    }


    static ConcurrentDictionary<LambdaExpression, Delegate> compiledExpressions = new ConcurrentDictionary<LambdaExpression, Delegate>(ExpressionComparer.GetComparer<LambdaExpression>(false));

    public static Func<T, R> GetPropertyRouteAccesor<T, R>(Expression<Func<T, R>> propertyRoute)
    {
        return (Func<T, R>)compiledExpressions.GetOrAdd(propertyRoute, ld => ld.Compile());
    }

    public static string ExportExcelFile(Type type, CultureInfo culture, string folder)
    {
        var fc = ExportExcelFile(type, culture);
        var fileName = Path.Combine(folder, fc.FileName);
        File.WriteAllBytes(fileName, fc.Bytes);
        return fileName;
    }

    public static FileContent ExportExcelFile(Type type, CultureInfo culture)
    {
        var isAllowed = Schema.Current.GetInMemoryFilter<TranslatedInstanceEntity>(userInterface: true);
        var result = TranslatedInstanceLogic.TranslationsForType(type, culture).Single().Value
            .Where(a => isAllowed(a.Value))
            .ToDictionary();

        var list = result
            .OrderBy(a=>a.Key.Instance.Id)
            .ThenBy(a=>a.Key.Route.PropertyInfo!.MetadataToken).Select(r => new ExcelRow
        {
            Instance = r.Key.Instance.Key(),
            Path = r.Key.Route.PropertyString(),
            RowId = r.Key.RowId?.ToString(),
            Original = r.Value.OriginalText,
            Translated = r.Value.TranslatedText
        }).ToList();

        return new FileContent(
            fileName: "{0}.{1}.View.xlsx".FormatWith(TypeLogic.GetCleanName(type), culture.Name),
            bytes: PlainExcelGenerator.WritePlainExcel<ExcelRow>(list)
        );
    }

    public static FileContent ExportExcelFileSync(Type type, CultureInfo culture)
    {
        var changes = TranslatedInstanceLogic.GetInstanceChanges(type, culture, new List<CultureInfo> { TranslatedInstanceLogic.DefaultCulture });

        var list = (from ic in changes
                    from pr in ic.RouteConflicts
                    orderby ic.Instance, pr.Key.ToString()
                    select new ExcelRow
                    {
                        Instance = ic.Instance.Key(),
                        Path = pr.Key.Route.PropertyString(),
                        RowId = pr.Key.RowId?.ToString(),
                        Original = pr.Value.GetOrThrow(TranslatedInstanceLogic.DefaultCulture).Original,
                        Translated = null
                    }).ToList();

        return new FileContent(
            fileName: "{0}.{1}.Sync.xlsx".FormatWith(TypeLogic.GetCleanName(type), culture.Name),
            bytes: PlainExcelGenerator.WritePlainExcel(list)
        );
    }

    public static TypeCulturePair ImportExcelFile(string filePath)
    {
        using (var stream = File.OpenRead(filePath))
            return ImportExcelFile(stream, Path.GetFileName(filePath));
    }

    public static TypeCulturePair ImportExcelFile(Stream stream, string fileName)
    {
        Type type = TypeLogic.GetType(fileName.Before('.'));
        CultureInfo culture = CultureInfo.GetCultureInfo(fileName.After('.').Before('.'));

        var records = PlainExcelGenerator.ReadPlainExcel(stream, cellValues => new TranslationRecord
        {
            Culture = culture,
            Key = new LocalizedInstanceKey(PropertyRoute.Parse(type, cellValues[1]),
                Lite.Parse<Entity>(cellValues[0]),
                cellValues[2].DefaultToNull()?.Let(s => PrimaryKey.Parse(s, type))),
            OriginalText = cellValues[3],
            TranslatedText = cellValues[4]
        });

        SaveRecords(records, type, isSync: false, culture);

        return new TypeCulturePair(type, culture);
    }

    public static List<InstanceChanges> GetInstanceChanges(Type type, CultureInfo targetCulture, List<CultureInfo> cultures)
    {
        Dictionary<CultureInfo, Dictionary<LocalizedInstanceKey, TranslatedInstanceEntity>> support = TranslatedInstanceLogic.TranslationsForType(type, culture: null);

        Dictionary<LocalizedInstanceKey, TranslatedInstanceEntity>? target = support.TryGetC(targetCulture);

        var instances = TranslatedInstanceLogic.FromEntities(type).GroupBy(a => a.Key.Instance).Select(gr =>
        {
            Dictionary<LocalizedInstanceKey, string> routeConflicts =
                (from kvp in gr
                 let t = target?.TryGetC(kvp.Key)
                 where kvp.Value.HasText() && (t == null || t.OriginalText.Replace("\r", "").Replace("\n", "") != kvp.Value.Replace("\r", "").Replace("\n", ""))
                 select KeyValuePair.Create(kvp.Key, kvp.Value)).ToDictionary();

            if (routeConflicts.IsEmpty())
                return null;

            var result = (from rc in routeConflicts
                          from c in cultures
                          let str = c.Equals(DefaultCulture) ? rc.Value : support.TryGetC(c)?.TryGetC(rc.Key)?.Let(a => a.OriginalText == rc.Value ? a.TranslatedText : null)
                          where str.HasText()
                          let old = c.Equals(DefaultCulture) ? target?.TryGetC(rc.Key) : null
                          select new
                          {
                              rc.Key.Route,
                              rc.Key.RowId,
                              Culture = c,
                              Conflict = new PropertyRouteConflict
                              {
                                  OldOriginal = old?.OriginalText,
                                  OldTranslation = old?.TranslatedText,

                                  Original = str,
                              }
                          }).AgGroupToDictionary(a => new IndexedPropertyRoute(a.Route!, a.RowId), g => g.ToDictionary(a => a.Culture!, a => a.Conflict!));

            return new InstanceChanges
            {
                Instance = gr.Key,
                RouteConflicts = result
            };

        }).NotNull().OrderByDescending(ic => ic.RouteConflicts.Values.Any(dic => dic.Values.Any(rc => rc.OldOriginal != null))).ToList();
        return instances;
    }



    public static void SaveRecords(List<TranslationRecord> records, Type t, bool isSync, CultureInfo? c)
    {
        Dictionary<(CultureInfo culture, LocalizedInstanceKey instanceKey), TranslationRecord> should = records
            .Where(a => !isSync || a.TranslatedText.HasText())
            .ToDictionary(a => (a.Culture, a.Key));

        Dictionary<(CultureInfo culture, LocalizedInstanceKey instanceKey), TranslatedInstanceEntity> current =
            (from ci in TranslatedInstanceLogic.TranslationsForType(t, c)
             from key in ci.Value
             select KeyValuePair.Create((culture: ci.Key, instanceKey: key.Key), key.Value)).ToDictionary();

        using (var tr = new Transaction())
        {
            Dictionary<PropertyRoute, PropertyRouteEntity> routes = should.Keys.Select(a => a.instanceKey.Route).Distinct().ToDictionary(a => a, a => a.ToPropertyRouteEntity());

            routes.Values.Where(a => a.IsNew).SaveList();

            List<TranslatedInstanceEntity> toInsert = new List<TranslatedInstanceEntity>(); 

            Synchronizer.Synchronize(
                should,
                current,
                (k, n) => toInsert.Add(new TranslatedInstanceEntity
                {
                    Culture = n.Culture.ToCultureInfoEntity(),
                    PropertyRoute = routes.GetOrThrow(n.Key.Route),
                    Instance = n.Key.Instance,
                    RowId  = n.Key.RowId?.ToString(),
                    OriginalText = n.OriginalText,
                    TranslatedText = n.TranslatedText,
                }),
                (k, o) => { },
                (k, n, o) =>
                {
                    if (!n.TranslatedText.HasText())
                    {
                        o.Delete();
                    }
                    else if (o.TranslatedText != n.TranslatedText || o.OriginalText != n.OriginalText)
                    {
                        var r = o.ToLite().RetrieveAndRemember();
                        r.OriginalText = n.OriginalText;
                        r.TranslatedText = n.TranslatedText;
                        r.Save();
                    }
                });

            toInsert.BulkInsert(message: "auto");

            tr.Commit();
        }
    }
}

public class TypeCulturePair
{
    public Type Type;
    public CultureInfo Culture;

    public TypeCulturePair(Type type, CultureInfo culture)
    {
        Type = type;
        Culture = culture;
    }
}

public struct TranslatableElement<T>
{
    public readonly Lite<Entity> Lite;
    public readonly PropertyRoute ElementRoute;
    public readonly T Value;
    public readonly PrimaryKey RowId;

    internal TranslatableElement(Lite<Entity> entity, PropertyRoute route, MList<T>.RowIdElement item)
    {
        this.Lite = entity;
        this.ElementRoute = route;
        this.Value = item.Element;
        this.RowId = item.RowId!.Value;
    }
}

#pragma warning disable CS8618 // Non-nullable field is uninitialized.
public class TranslationRecord
{
    public CultureInfo Culture;
    public LocalizedInstanceKey Key;
    public string TranslatedText;
    public string OriginalText;

    public override string ToString()
    {
        return "{0} {1} {2} -> {3}".FormatWith(Culture, Key.Instance, Key.Route, TranslatedText);
    }
}

public class InstanceChanges
{
    public Lite<Entity> Instance { get; set; }

    public Dictionary<IndexedPropertyRoute, Dictionary<CultureInfo, PropertyRouteConflict>> RouteConflicts { get; set; }

    public override string ToString()
    {
        return "Changes for {0}".FormatWith(Instance);
    }

    public int TotalOriginalLength()
    {
        return RouteConflicts.Values.Sum(dic => dic[TranslatedInstanceLogic.DefaultCulture].Original.Length);
    }
}

public struct IndexedPropertyRoute : IEquatable<IndexedPropertyRoute>
{
    public readonly PropertyRoute Route;
    public readonly PrimaryKey? RowId;

    public IndexedPropertyRoute(PropertyRoute route, PrimaryKey? rowId)
    {
        this.Route = route;
        this.RowId = rowId;
    }

    public override string ToString()
    {
        return Route.PropertyString().Replace("/", "[" + RowId + "].");
    }

    public string RouteRowId()
    {
        return Route.PropertyString() + (RowId == null ? null : ";" + RowId);
    }

    public override bool Equals(object? obj) => obj is IndexedPropertyRoute ipr && base.Equals(ipr);

    public bool Equals(IndexedPropertyRoute other)
    {
        return Route.Equals(other.Route) && RowId.Equals(other.RowId);
    }

    public override int GetHashCode()
    {
        return Route.GetHashCode() ^ RowId.GetHashCode();
    }
}

public class PropertyRouteConflict
{
    public string? OldOriginal;
    public string? OldTranslation;

    public string Original;
    public List<AutomaticTranslation> AutomaticTranslations = new List<AutomaticTranslation>();

    public override string ToString()
    {
        return "Conflict {0} -> {1}".FormatWith(Original, AutomaticTranslations.Count);
    }
}

class ExcelRow
{
    public string Instance; 
    public string Path;
    public string? RowId; 
    public string Original; 
    public string? Translated; 
}

public struct LocalizedInstanceKey : IEquatable<LocalizedInstanceKey>
{
    public readonly PropertyRoute Route;
    public readonly Lite<Entity> Instance;
    public readonly PrimaryKey? RowId; 

    public LocalizedInstanceKey(PropertyRoute route, Lite<Entity> instance, PrimaryKey? rowId)
    {
        this.Route = route ?? throw new ArgumentNullException(nameof(route));
        this.Instance = instance ?? throw new ArgumentNullException("entity");
        this.RowId = rowId;
    }

    public bool Equals(LocalizedInstanceKey other)
    {
        return
            Route.Equals(other.Route) &&
            Instance.Equals(other.Instance) &&
            RowId.Equals(other.RowId);
    }

    public override int GetHashCode()
    {
        return Route.GetHashCode() ^ Instance.GetHashCode() ^ RowId.GetHashCode();
    }

    public override string ToString()
    {
        var result = "{0} {1}".FormatWith(Route, Instance);

        if (RowId.HasValue)
            result += " " + RowId;

        return result;
    }

    public string RouteAndRowId()
    {
        if(RowId.HasValue)
            return Route.PropertyString() + ";" + RowId;

        return Route.PropertyString();
    }
}

public class TranslatedTypeSummary
{
    public Type Type;
    public CultureInfo CultureInfo;
    public TranslatedSummaryState? State;
}
#pragma warning restore CS8618 // Non-nullable field is uninitialized.
