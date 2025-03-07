using Signum.Engine.Engine;
using Signum.Engine.Linq;
using Signum.Engine.Maps;
using Signum.Engine.PostgresCatalog;
using Signum.Engine.SchemaInfoTables;
using System.Collections.Concurrent;
using System.Collections.Immutable;
using System.Data.Common;
using System.Diagnostics;

namespace Signum.Engine;

public static class Administrator
{
    public static Func<bool>? OnTotalGeneration;

    public static void TotalGeneration()
    {
        CleanAllDatabases();

        ExecuteGenerationScript();
    }

    const int TimeoutCreateDatabase = 5 * 60; 

    public static void ExecuteGenerationScript()
    {
        using (Connector.CommandTimeoutScope(TimeoutCreateDatabase))
        {
            SqlPreCommandConcat totalScript = (SqlPreCommandConcat)Schema.Current.GenerationScipt()!;
            foreach (SqlPreCommand command in totalScript.Commands)
            {
                command.ExecuteLeaves();
                SafeConsole.WriteColor(ConsoleColor.DarkGray, '.');
            }
        }
    }

    private static void CleanAllDatabases()
    {
        using (Connector.CommandTimeoutScope(TimeoutCreateDatabase))
        {
            foreach (var db in Schema.Current.DatabaseNames())
            {
                Connector.Current.CleanDatabase(db);
                SafeConsole.WriteColor(ConsoleColor.DarkGray, '.');
            }
        }
    }

    public static string GenerateViewCodes(params string[] tableNames) => tableNames.ToString(tn => GenerateViewCode(tn), "\r\n\r\n");

    public static string GenerateViewCode(string tableName) => GenerateViewCode(ObjectName.Parse(tableName, Schema.Current.Settings.IsPostgres));

    public static string GenerateViewCode(ObjectName tableName)
    {
        using (OverrideDatabaseInSysViews(tableName.Schema.Database))
        {
            var columns =
                (from t in Database.View<SysTables>()
                 where t.name == tableName.Name && t.Schema().name == tableName.Schema.Name
                 from c in t.Columns()
                 select new DiffColumn
                 {
                     Name = c.name,
                     DbType = new AbstractDbType(SysTablesSchema.ToSqlDbType(c.Type()!.name)),
                     UserTypeName = null,
                     PrimaryKey = t.Indices().Any(i => i.is_primary_key && i.IndexColumns().Any(ic => ic.column_id == c.column_id)),
                     Nullable = c.is_nullable,
                 }).ToList();

            StringBuilder sb = new StringBuilder();
            sb.AppendLine($@"[TableName(""{tableName}"")]");
            sb.AppendLine($"public class {tableName.Name} : IView");
            sb.AppendLine(@"{");
            foreach (var c in columns)
            {
                sb.Append(GenerateColumnCode(c).Indent(4));
            }
            sb.AppendLine(@"}");
            return sb.ToString();
        }
    }

    private static string GenerateColumnCode(DiffColumn c)
    {
        var type = CodeGeneration.CodeGenerator.Entities.GetValueType(c);
       
        StringBuilder sb = new StringBuilder();
        if (c.PrimaryKey)
            sb.AppendLine("[ViewPrimaryKey]");
        sb.AppendLine($"public {type.TypeName()}{(c.Nullable ? "?" : "")} {c.Name};");
        return sb.ToString();
    }

    public static SqlPreCommand? TotalGenerationScript()
    {
        return Schema.Current.GenerationScipt();
    }


    public static Func<bool>? AvoidSimpleGenerate;

    public static void NewDatabase()
    {
        var databaseName = Connector.Current.DatabaseName();
        if (Connector.Current.HasTables())
        {
            SafeConsole.WriteLineColor(ConsoleColor.Red, $"Are you sure you want to delete all the data in the database '{databaseName}'?");
            Console.Write($"Confirm by writing the name of the database:");
            string val = Console.ReadLine()!;
            if (val.ToLower() != databaseName.ToLower())
            {
                Console.WriteLine($"Wrong name. No changes where made");
                Console.WriteLine();
                return;
            }
        }

        Console.Write("Cleaning database...");
        using(Connector.CommandTimeoutScope(5 * 60))
        CleanAllDatabases();
        Console.WriteLine("Done.");

        if (AvoidSimpleGenerate?.Invoke() == true)
            return;

        Console.Write("Generating new database database...");
        ExecuteGenerationScript();
        Console.WriteLine("Done.");
    }

    public static Func<bool> AvoidSimpleSynchronize = () => true;

    public static void Synchronize()
    {
        if (AvoidSimpleSynchronize())
            return; 

        Console.WriteLine();

        SqlPreCommand? command = Administrator.TotalSynchronizeScript();
        if (command == null)
        {
            SafeConsole.WriteLineColor(ConsoleColor.Green, "Already synchronized!");
            return;
        }

        command.OpenSqlFileRetry();
    }

    public static SqlPreCommand? TotalSynchronizeScript(bool interactive = true, bool schemaOnly = false)
    {

        var command = Schema.Current.SynchronizationScript(interactive, schemaOnly);

        if (command == null)
            return null;

        return SqlPreCommand.Combine(Spacing.Double,
            new SqlPreCommandSimple(SynchronizerMessage.StartOfSyncScriptGeneratedOn0.NiceToString().FormatWith(DateTime.Now)),

            Connector.Current.SqlBuilder.UseDatabase(),
            command,
            new SqlPreCommandSimple(SynchronizerMessage.EndOfSyncScript.NiceToString()));
    }

    public static void CreateTemporaryTable<T>()
      where T : IView
    {
        if (!Transaction.HasTransaction)
            throw new InvalidOperationException("You need to be inside of a transaction to create a Temporary table");

        var view = Schema.Current.View<T>();

        if (!view.Name.IsTemporal)
            throw new InvalidOperationException($"Temporary tables should start with # (i.e. #myTable). Consider using {nameof(TableNameAttribute)}");

        Connector.Current.SqlBuilder.CreateTableSql(view).ExecuteLeaves();
    }

    public static IDisposable TemporaryTable<T>() where T : IView
    {
        CreateTemporaryTable<T>();

        return new Disposable(() => DropTemporaryTable<T>());
    }

    public static void DropTemporaryTable<T>()
        where T : IView
    {
        if (!Transaction.HasTransaction)
            throw new InvalidOperationException("You need to be inside of a transaction to create a Temporary table");

        var view = Schema.Current.View<T>();

        if (!view.Name.IsTemporal)
            throw new InvalidOperationException($"Temporary tables should start with # (i.e. #myTable). Consider using {nameof(TableNameAttribute)}");

        Connector.Current.SqlBuilder.DropTable(view.Name).ExecuteNonQuery();
    }

    public static void CreateTemporaryIndex<T>(Expression<Func<T, object>> fields, bool unique = false)
         where T : IView
    {
        var view = Schema.Current.View<T>();

        IColumn[] columns = IndexKeyColumns.Split(view, fields);

        var index = unique ?
            new UniqueTableIndex(view, columns) :
            new TableIndex(view, columns);

        Connector.Current.SqlBuilder.CreateIndex(index, checkUnique: null).ExecuteLeaves();
    }

    internal static readonly AsyncThreadVariable<Func<ObjectName, ObjectName>?> registeredViewNameReplacer = Statics.ThreadVariable<Func<ObjectName, ObjectName>?>("overrideDatabase");
    public static IDisposable OverrideViewNameReplacer(Func<ObjectName, ObjectName> replacer)
    {
        registeredViewNameReplacer.Value += replacer;
        return new Disposable(() => registeredViewNameReplacer.Value -= replacer);
    }

    public static event Func<ObjectName, ObjectName>? GlobalViewNameReplacer;

    public static ObjectName ReplaceViewName(ObjectName name)
    {
        var scopeReplacer = registeredViewNameReplacer.Value;
        if(scopeReplacer != null)
        {
            foreach (var rep in scopeReplacer.GetInvocationListTyped())
            {
                var on = rep(name);
                if (on != null)
                    return on;
            }
        }

        var globalReplacer = GlobalViewNameReplacer;
        if (globalReplacer != null)
        {
            foreach (var rep in globalReplacer.GetInvocationListTyped())
            {
                var on = rep(name);
                if (on != null)
                    return on;
            }
        }

        return name;
    }

    public static IDisposable OverrideDatabaseInSysViews(DatabaseName? database)
    {
        return OverrideViewNameReplacer(n => n.Schema.Name == "sys" ? n.OnDatabase(database) : n);
    }

    public static bool ExistsTable<T>()
        where T : Entity
    {
        return ExistsTable(Schema.Current.Table<T>());
    }

    public static bool ExistsTable(Type type)
    {
        return ExistsTable(Schema.Current.Table(type));
    }

    public static bool ExistsTable(ITable table)
    {
        SchemaName schema = table.Name.Schema;
        if (Schema.Current.Settings.IsPostgres)
        {
            return (from t in Database.View<PgClass>()
                    join ns in Database.View<PgNamespace>() on t.relnamespace equals ns.oid
                    where t.relname == table.Name.Name && ns.nspname == schema.Name
                    select t).Any();
        }

        if (schema.Database != null && schema.Database.Server != null && !Database.View<SysServers>().Any(ss => ss.name == schema.Database!.Server!.Name))
            return false;

        if (schema.Database != null && !Database.View<SysDatabases>().Any(ss => ss.name == schema.Database!.Name))
            return false;

        using (schema.Database == null ? null : Administrator.OverrideDatabaseInSysViews(schema.Database))
        {
            return (from t in Database.View<SysTables>()
                    join s in Database.View<SysSchemas>() on t.schema_id equals s.schema_id
                    where t.name == table.Name.Name && s.name == schema.Name
                    select t).Any();
        }
    }

    public static bool ExistSchema(SchemaName name)
    {
        if (Schema.Current.Settings.IsPostgres)
            return Database.View<PgNamespace>().Any(ns => ns.nspname == name.Name);

        return Database.View<SysSchemas>().Any(s => s.name == name.Name);
    }

    public static List<T> TryRetrieveAll<T>(Replacements replacements)
        where T : Entity
    {
        return TryRetrieveAll(typeof(T), replacements).Cast<T>().ToList();
    }

    public static List<Entity> TryRetrieveAll(Type type, Replacements replacements)
    {
        Table table = Schema.Current.Table(type);

        using (Synchronizer.UseOldTableName(table, replacements))
        using (ExecutionMode.DisableCache())
        {
            if (ExistsTable(table))
                return Database.RetrieveAll(type);
            return new List<Entity>();
        }
    }



    public static IDisposable SaveDisableIdentity<T>()
        where T : Entity
    {
        Table table = Schema.Current.Table<T>();
        return DisableIdentity(table);
    }

    public static IDisposable? DisableIdentity<T, V>(Expression<Func<T, MList<V>>> mListField)
      where T : Entity
    {
        TableMList table = ((FieldMList)Schema.Current.Field(mListField)).TableMList;
        return DisableIdentity(table);
    }

    public static bool IsIdentityBehaviourDisabled(ITable table)
    {
        return identityBehaviourDisabled.Value?.Contains(table) == true;
    }

    [DebuggerStepThrough]
    public static IQueryable<T> QueryDisableAssertAllowed<T>() where T : Entity
    {
        return new SignumTable<T>(DbQueryProvider.Single, Schema.Current.Table<T>(), disableAssertAllowed: true);
    }

    static AsyncThreadVariable<ImmutableStack<ITable>?> identityBehaviourDisabled = Statics.ThreadVariable<ImmutableStack<ITable>?>("identityBehaviourOverride");
    public static IDisposable DisableIdentity(ITable table, bool behaviourOnly = false)
    {
        if (!table.IdentityBehaviour)
            throw new InvalidOperationException("Identity is false already");

        var sqlBuilder = Connector.Current.SqlBuilder;
        var oldValue = identityBehaviourDisabled.Value ?? ImmutableStack<ITable>.Empty;

        identityBehaviourDisabled.Value = oldValue.Push(table);
        if (table.PrimaryKey.Default == null && !sqlBuilder.IsPostgres && !behaviourOnly)
            sqlBuilder.SetIdentityInsert(table.Name, true).ExecuteNonQuery();

        return new Disposable(() =>
        {
            identityBehaviourDisabled.Value = oldValue.IsEmpty ? null : oldValue;

            if (table.PrimaryKey.Default == null && !sqlBuilder.IsPostgres && !behaviourOnly)
                sqlBuilder.SetIdentityInsert(table.Name, false).ExecuteNonQuery();
        });
    }

    public static T SaveDisableIdentity<T>(T entity)
        where T : Entity
    {
        using (var tr = new Transaction())
        using (Administrator.SaveDisableIdentity<T>())
        {
            Database.Save(entity);
            return tr.Commit(entity);
        }
    }

    public static void SaveListDisableIdentity<T>(IEnumerable<T> entities)
        where T : Entity
    {
        using (var tr = new Transaction())
        using (Administrator.SaveDisableIdentity<T>())
        {
            Database.SaveList(entities);
            tr.Commit();
        }
    }

    public static int UnsafeDeleteDuplicates<E, K>(this IQueryable<E> query, Expression<Func<E, K>> key, string? message = null)
       where E : Entity
    {
        return (from e in query
                where !query.GroupBy(key).Select(gr => gr.Min(a => a.id)).Contains(e.Id)
                select e).UnsafeDelete(message);
    }

    public static int UnsafeDeleteMListDuplicates<E, V, K>(this IQueryable<MListElement<E,V>> query, Expression<Func<MListElement<E, V>, K>> key, string? message = null)
        where E : Entity
    {
        return (from e in query
                where !query.GroupBy(key).Select(gr => gr.Min(a => a.RowId)).Contains(e.RowId)
                select e).UnsafeDeleteMList(message);
    }

    public static SqlPreCommandSimple QueryPreCommand<T>(IQueryable<T> query)
    {
        var prov = ((DbQueryProvider)query.Provider);

        return prov.Translate(query.Expression, tr => tr.MainCommand);
    }

    public static SqlPreCommandSimple? UnsafeDeletePreCommand<T>(IQueryable<T> query, bool force = false, bool avoidMList = false)
        where T : Entity
    {
        if (!Administrator.ExistsTable<T>() || (!query.Any() && !force))
            return null;

        var prov = ((DbQueryProvider)query.Provider);
        using (PrimaryKeyExpression.PreferVariableName())
            return prov.Delete<SqlPreCommandSimple>(query, cm => cm, removeSelectRowCount: true, avoidMList);
    }

    public static SqlPreCommandSimple? UnsafeDeletePreCommandMList<E, V>(Expression<Func<E, MList<V>>> mListProperty, IQueryable<MListElement<E, V>> query)
        where E : Entity
    {
        if (!Administrator.ExistsTable(Schema.Current.TableMList(mListProperty)) || !query.Any())
            return null;

        var prov = ((DbQueryProvider)query.Provider);
        using (PrimaryKeyExpression.PreferVariableName())
            return prov.Delete<SqlPreCommandSimple>(query, cm => cm, removeSelectRowCount: true);
    }

    public static SqlPreCommandSimple UnsafeUpdatePartPreCommand(IUpdateable update)
    {
        var prov = ((DbQueryProvider)update.Query.Provider);

        return prov.Update(update, sql => sql, removeSelectRowCount: true);
    }

    public static void UpdateToStrings<T>() where T : Entity, new()
    {
        UpdateToStrings(Database.Query<T>());
    }

    public static void UpdateToStrings<T>(IQueryable<T> query) where T : Entity, new()
    {
        SafeConsole.WriteLineColor(ConsoleColor.Cyan, "Saving toStr for {0}".FormatWith(typeof(T).TypeName()));

        if (!query.Any())
            return;

        query.Select(a => a.Id).IntervalsOf(100).ProgressForeach(inter => inter.ToString(), (interva) =>
        {
            var list = query.Where(a => interva.Contains(a.Id)).ToList();

            foreach (var item in list)
            {
                if (item.ToString() != item.toStr)
                    item.InDB().UnsafeUpdate()
                        .Set(a => a.toStr, a => item.ToString())
                        .Execute();
            }
        });
    }

    public static void UpdateToStrings<T>(Expression<Func<T, string?>> expression) where T : Entity, new()
    {
        UpdateToStrings(Database.Query<T>(), expression);
    }

    public static void UpdateToStrings<T>(IQueryable<T> query, Expression<Func<T, string?>> expression) where T : Entity, new()
    {
        SafeConsole.WaitRows("UnsafeUpdate toStr for {0}".FormatWith(typeof(T).TypeName()), () =>
            query.UnsafeUpdate().Set(a => a.toStr, expression).Execute());
    }

    public static void UpdateToString<T>(T entity) where T : Entity, new()
    {
        entity.InDB().UnsafeUpdate()
            .Set(e => e.toStr, e => entity.ToString())
            .Execute();
    }

    public static void UpdateToString<T>(T entity, Expression<Func<T, string?>> expression) where T : Entity, new()
    {
        entity.InDB().UnsafeUpdate()
            .Set(e => e.toStr, expression)
            .Execute();
    }

    public static IDisposable PrepareForBatchLoadScope<T>(bool disableForeignKeys = true, bool disableMultipleIndexes = true, bool disableUniqueIndexes = false) where T : Entity
    {
        Table table = Schema.Current.Table(typeof(T));

        return table.PrepareForBathLoadScope(disableForeignKeys, disableMultipleIndexes, disableUniqueIndexes);
    }

    static IDisposable PrepareForBathLoadScope(this Table table, bool disableForeignKeys, bool disableMultipleIndexes, bool disableUniqueIndexes)
    {
        IDisposable disp = PrepareTableForBatchLoadScope(table, disableForeignKeys, disableMultipleIndexes, disableUniqueIndexes);

        var list = table.TablesMList().Select(rt => PrepareTableForBatchLoadScope(rt, disableForeignKeys, disableMultipleIndexes, disableUniqueIndexes)).ToList();

        return new Disposable(() =>
        {
            disp.Dispose();

            foreach (var d in list)
                d.Dispose();
        });
    }

    public static IDisposable PrepareTableForBatchLoadScope(ITable table, bool disableForeignKeys, bool disableMultipleIndexes, bool disableUniqueIndexes)
    {
        var sqlBuilder = Connector.Current.SqlBuilder;
        SafeConsole.WriteColor(ConsoleColor.Magenta, table.Name + ":");
        Action onDispose = () => SafeConsole.WriteColor(ConsoleColor.Magenta, table.Name + ":");

        if (disableForeignKeys)
        {
            SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " NOCHECK  Foreign Keys");
            Executor.ExecuteNonQuery("ALTER TABLE {0} NOCHECK CONSTRAINT ALL".FormatWith(table.Name));

            onDispose += () =>
            {
                SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " RE-CHECK Foreign Keys");
                Executor.ExecuteNonQuery("ALTER TABLE {0}  WITH CHECK CHECK CONSTRAINT ALL".FormatWith(table.Name));
            };
        }

        if (disableMultipleIndexes)
        {
            var multiIndexes = GetIndixesNames(table, unique: false);

            if (multiIndexes.Any())
            {
                SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " DISABLE Multiple Indexes");
                multiIndexes.Select(i => sqlBuilder.DisableIndex(table.Name, i)).Combine(Spacing.Simple)!.ExecuteLeaves();
                Executor.ExecuteNonQuery(multiIndexes.ToString(i => "ALTER INDEX [{0}] ON {1} DISABLE".FormatWith(i, table.Name), "\r\n"));

                onDispose += () =>
                {
                    SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " REBUILD Multiple Indexes");
                    multiIndexes.Select(i => sqlBuilder.RebuildIndex(table.Name, i)).Combine(Spacing.Simple)!.ExecuteLeaves();
                };
            }
        }

        if (disableUniqueIndexes)
        {
            var uniqueIndexes = GetIndixesNames(table, unique: true);

            if (uniqueIndexes.Any())
            {
                SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " DISABLE Unique Indexes");
                uniqueIndexes.Select(i => sqlBuilder.DisableIndex(table.Name, i)).Combine(Spacing.Simple)!.ExecuteLeaves();
                onDispose += () =>
                {
                    SafeConsole.WriteColor(ConsoleColor.DarkMagenta, " REBUILD Unique Indexes");
                    uniqueIndexes.Select(i => sqlBuilder.RebuildIndex(table.Name, i)).Combine(Spacing.Simple)!.ExecuteLeaves();
                };
            }
        }

        Console.WriteLine();
        onDispose += () => Console.WriteLine();

        return new Disposable(onDispose);
    }

    public static void TruncateTable<T>() where T : Entity => TruncateTable(typeof(T));
    public static void TruncateTable(Type type)
    {
        var table = Schema.Current.Table(type);

        using (var tr = new Transaction())
        {
            table.TablesMList().ToList().ForEach(mlist =>
            {
                TruncateTableSystemVersioning(mlist);
            });

            using (DropAndCreateIncomingForeignKeys(table))
                TruncateTableSystemVersioning(table);

            tr.Commit();
        }
    }

    public static void TruncateTableSystemVersioning(ITable table)
    {
        var sqlBuilder = Connector.Current.SqlBuilder;

        if(table.SystemVersioned == null)
            sqlBuilder.TruncateTable(table.Name).ExecuteLeaves();
        else
        {
            sqlBuilder.AlterTableDisableSystemVersioning(table.Name).ExecuteLeaves();
            sqlBuilder.TruncateTable(table.Name).ExecuteLeaves();
            sqlBuilder.TruncateTable(table.SystemVersioned.TableName).ExecuteLeaves();
            sqlBuilder.AlterTableEnableSystemVersioning(table).ExecuteLeaves();
        }
    }

    public static IDisposable DropAndCreateIncomingForeignKeys(Table table)
    {
        var sqlBuilder = Connector.Current.SqlBuilder;
        var isPostgres = Schema.Current.Settings.IsPostgres;

        var foreignKeys = Administrator.OverrideDatabaseInSysViews(table.Name.Schema.Database).Using(_ =>
        (from targetTable in Database.View<SysTables>()
         where targetTable.name == table.Name.Name && targetTable.Schema().name == table.Name.Schema.Name
         from ifk in targetTable.IncomingForeignKeys()
         let parentTable = ifk.ParentTable()
         select new
         {
             Name = ifk.name,
             ParentTable = new ObjectName(new SchemaName(table.Name.Schema.Database, parentTable.Schema().name, isPostgres), parentTable.name, isPostgres),
             ParentColumn = parentTable.Columns().SingleEx(c => c.column_id == ifk.ForeignKeyColumns().SingleEx().parent_column_id).name,
         }).ToList());

        foreignKeys.ForEach(fk => sqlBuilder.AlterTableDropConstraint(fk.ParentTable!, fk.Name).ExecuteLeaves());

        return new Disposable(() =>
        {
            foreignKeys.ToList().ForEach(fk => sqlBuilder.AlterTableAddConstraintForeignKey(fk.ParentTable!, fk.ParentColumn!, table.Name, table.PrimaryKey.Name)!.ExecuteLeaves());
        });
    }

    public static IDisposable DisableUniqueIndex(UniqueTableIndex index)
    {
        var sqlBuilder = Connector.Current.SqlBuilder;
        SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, " DISABLE Unique Index "  + index.IndexName);
        sqlBuilder.DisableIndex(index.Table.Name, index.IndexName).ExecuteLeaves();
        return new Disposable(() =>
        {
            SafeConsole.WriteLineColor(ConsoleColor.DarkMagenta, " REBUILD Unique Index " + index.IndexName);
            sqlBuilder.RebuildIndex(index.Table.Name, index.IndexName).ExecuteLeaves();
        });
    }

    public static List<string> GetIndixesNames(this ITable table, bool unique)
    {
        using (OverrideDatabaseInSysViews(table.Name.Schema.Database))
        {
            return (from s in Database.View<SysSchemas>()
                    where s.name == table.Name.Schema.Name
                    from t in s.Tables()
                    where t.name == table.Name.Name
                    from i in t.Indices()
                    where i.is_unique == unique && !i.is_primary_key
                    select i.name).ToList();
        }
    }

    public static void DropUniqueIndexes<T>() where T : Entity
    {
        var sqlBuilder = Connector.Current.SqlBuilder;
        var table = Schema.Current.Table<T>();
        var indexesNames = Administrator.GetIndixesNames(table, unique: true);

        if (indexesNames.HasItems())
            indexesNames.Select(n => sqlBuilder.DropIndex(table.Name, n)).Combine(Spacing.Simple)!.ExecuteLeaves();
    }


    public static void MoveAllForeignKeys<T>(Lite<T> fromEntity, Lite<T> toEntity, Func<ITable, IColumn, bool>? shouldMove = null)
    where T : Entity
    {
        using (var tr = new Transaction())
        {
            MoveAllForeignKeysPrivate<T>(fromEntity, toEntity, shouldMove).Select(a => a.UpdateScript).Combine(Spacing.Double)!.ExecuteLeaves();
            tr.Commit();
        }
    }

    public static SqlPreCommand? MoveAllForeignKeysScript<T>(Lite<T> fromEntity, Lite<T> toEntity, Func<ITable, IColumn, bool>? shouldMove = null)
    where T : Entity
    {
        return MoveAllForeignKeysPrivate<T>(fromEntity, toEntity, shouldMove).Select(a => a.UpdateScript).Combine(Spacing.Double);
    }

    public static void MoveAllForeignKeysConsole<T>(Lite<T> fromEntity, Lite<T> toEntity, Func<ITable, IColumn, bool>? shouldMove = null)
        where T : Entity
    {
        var tuples = MoveAllForeignKeysPrivate<T>(fromEntity, toEntity, shouldMove);
        foreach (var t in tuples)
        {
            SafeConsole.WaitRows("{0}.{1}".FormatWith(t.ColumnTable.Table.Name.Name, t.ColumnTable.Column.Name), () => t.UpdateScript.ExecuteNonQuery());
        }
    }

    class ColumnTableScript
    {
        public ColumnTable ColumnTable;
        public SqlPreCommandSimple UpdateScript;

        public ColumnTableScript(ColumnTable columnTable, SqlPreCommandSimple updateScript)
        {
            ColumnTable = columnTable;
            UpdateScript = updateScript;
        }
    }

    static List<ColumnTableScript> MoveAllForeignKeysPrivate<T>(Lite<T> fromEntity, Lite<T> toEntity, Func<ITable, IColumn, bool>? shouldMove)
    where T : Entity
    {
        if (fromEntity.GetType() != toEntity.GetType())
            throw new ArgumentException("fromEntity and toEntity should have the same type");

        if (fromEntity.Is(toEntity))
            throw new ArgumentException("fromEntity and toEntity should not be the same ");

        Schema s = Schema.Current;

        Table refTable = s.Table(typeof(T));

        List<ColumnTable> columns = GetColumnTables(s, refTable);
        if (shouldMove != null)
            columns = columns.Where(p => shouldMove!(p.Table, p.Column)).ToList();

        var isPostgres = Schema.Current.Settings.IsPostgres;
        var pb = Connector.Current.ParameterBuilder;
        return columns.Select(ct => new ColumnTableScript(ct, new SqlPreCommandSimple("UPDATE {0}\r\nSET {1} = @toEntity\r\nWHERE {1} = @fromEntity".FormatWith(ct.Table.Name, ct.Column.Name.SqlEscape(isPostgres)), new List<DbParameter>
        {
            pb.CreateReferenceParameter("@fromEntity", fromEntity.Id, ct.Column),
            pb.CreateReferenceParameter("@toEntity", toEntity.Id, ct.Column),
        }))).ToList();
    }

    class ColumnTable
    {
        public ITable Table;
        public IColumn Column;

        public ColumnTable(ITable table, IColumn column)
        {
            Table = table;
            Column = column;
        }
    }

    static ConcurrentDictionary<Table, List<ColumnTable>> columns = new ConcurrentDictionary<Table, List<ColumnTable>>();

    static List<ColumnTable> GetColumnTables(Schema schema, Table refTable)
    {
        return columns.GetOrAdd(refTable, rt =>
        {
            return (from t in schema.GetDatabaseTables()
                    from c in t.Columns.Values
                    where c.ReferenceTable == rt
                    select new ColumnTable(t,c))
                    .ToList();
        });
    }

    public static T GetSetTicks<T>(this T entity) where T : Entity
    {
        entity.Ticks = entity.InDB(e => e.Ticks);
        return entity;
    }

    public static SqlPreCommand DeleteWhereScript(Table table, IColumn column, PrimaryKey id)
    {
        if (table.TablesMList().Any())
            throw new InvalidOperationException($"DeleteWhereScript can not be used for {table.Type.Name} because contains MLists");

        if(id.VariableName.HasText())
            return new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2};".FormatWith(table.Name, column.Name, id.VariableName));

        var param = Connector.Current.ParameterBuilder.CreateReferenceParameter("@id", id, column);
        return new SqlPreCommandSimple("DELETE FROM {0} WHERE {1} = {2}:".FormatWith(table.Name, column.Name, param.ParameterName), new List<DbParameter> { param });
    }


    public static SqlPreCommand DeleteWhereScript<T, R>(Expression<Func<T, R>> field, R value)
        where T : Entity
        where R : IEntity
    {
        var table = Schema.Current.Table<T>();
        var f = Schema.Current.Field(field);

        var column = f is IColumn c ? c : 
            f is FieldImplementedBy fib ? fib.ImplementationColumns.GetOrThrow(value.GetType()) : 
            throw new UnexpectedValueException(f);

        return DeleteWhereScript(table, column, value.Id);
    }
}
