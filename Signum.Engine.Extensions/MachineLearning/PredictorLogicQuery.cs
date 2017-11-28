﻿using Signum.Engine.Basics;
using Signum.Engine.DynamicQuery;
using Signum.Entities;
using Signum.Entities.Basics;
using Signum.Entities.DynamicQuery;
using Signum.Entities.MachineLearning;
using Signum.Entities.UserAssets;
using Signum.Entities.UserQueries;
using Signum.Utilities;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Signum.Engine.MachineLearning
{
    public static class PredictorLogicQuery
    {
        public static void RetrieveData(PredictorTrainingContext ctx)
        {
            ctx.ReportProgress($"Executing MainQuery for {ctx.Predictor}");
            QueryRequest mainQueryRequest = GetMainQueryRequest(ctx.Predictor.MainQuery);
            ResultTable mainResult = DynamicQueryManager.Current.ExecuteQuery(mainQueryRequest);

            ctx.MainQuery = new MainQuery
            {
                QueryRequest = mainQueryRequest,
                ResultTable = mainResult,
            };
            
            ctx.SubQueries = new Dictionary<PredictorSubQueryEntity, SubQuery>();
            foreach (var sqe in ctx.Predictor.SubQueries)
            {
                ctx.ReportProgress($"Executing SubQuery {sqe}");
                QueryRequest queryGroupRequest = ToMultiColumnQuery(ctx.Predictor.MainQuery, sqe);
                ResultTable groupResult = DynamicQueryManager.Current.ExecuteQuery(queryGroupRequest);

                var entityGroupKey = groupResult.Columns.FirstEx();
                var remainingKeys = groupResult.Columns.Take(sqe.GroupKeys.Count).Skip(1).ToArray();
                var aggregates = groupResult.Columns.Skip(sqe.GroupKeys.Count).ToArray();

                var groupedValues = groupResult.Rows.AgGroupToDictionary(row => (Lite<Entity>)row[entityGroupKey], gr =>
                    gr.ToDictionaryEx(
                        row => row.GetValues(remainingKeys),
                        row => row.GetValues(aggregates),
                        ObjectArrayComparer.Instance));

                ctx.SubQueries.Add(sqe, new SubQuery
                {
                    SubQueryEntity = sqe,
                    QueryGroupRequest = queryGroupRequest,
                    ResultTable = groupResult,
                    GroupedValues = groupedValues,
                    Aggregates = aggregates,
                });
            }

            ctx.ReportProgress($"Creating Columns");
            var columns = new List<PredictorCodification>();

            for (int i = 0; i < mainResult.Columns.Length; i++)
            {
                columns.AddRange(ExpandColumns(ctx.Predictor.MainQuery.Columns[i], i, mainResult.Columns[i]));
            }
            
            foreach (var sq in ctx.SubQueries.Values)
            {
                var distinctKeys = sq.GroupedValues.SelectMany(a => a.Value.Keys).Distinct(ObjectArrayComparer.Instance).ToList();

                distinctKeys.Sort(ObjectArrayComparer.Instance);

                foreach (var k in distinctKeys)
                {
                    for (int i = 0; i < sq.Aggregates.Length; i++)
                    {
                        var list = ExpandColumns(sq.SubQueryEntity.Aggregates[i], i, sq.Aggregates[i]).ToList();
                        list.ForEach(a =>
                        {
                            a.SubQuery = sq.SubQueryEntity;
                            a.Keys = k;
                        });

                        columns.AddRange(list);
                    }
                }
            }
            
            ctx.SetColums(columns.ToArray());
        }

        private static IEnumerable<PredictorCodification> ExpandColumns(PredictorColumnEmbedded pc, int pcIndex, ResultColumn rc)
        {
            switch (pc.Encoding)
            {
                case PredictorColumnEncoding.None:
                    return new[] { new PredictorCodification(pc, pcIndex) };
                case PredictorColumnEncoding.OneHot:
                    return rc.Values.Cast<object>().NotNull().Distinct().Select(v => new PredictorCodification(pc, pcIndex)
                    {
                        IsValue = v
                    }).ToList();
                case PredictorColumnEncoding.Codified:
                    {
                        var values = rc.Values.Cast<object>().Distinct().ToArray();
                        var valuesToIndex = values.Select((v, i) => KVP.Create(v, i)).ToDictionary();
                        return new[] { new PredictorCodification(pc, pcIndex)
                        {
                            ValuesToIndex = valuesToIndex,
                            CodedValues = values
                        }};
                    }
                case PredictorColumnEncoding.NormalizeZScore:
                    {
                        var values = rc.Values.Cast<object>().NotNull().Select(a => Convert.ToSingle(a)).ToList();
                        return new[] { new PredictorCodification(pc, pcIndex)
                        {
                            Mean = values.Count == 0 ? 0: values.Average(),
                            StdDev = values.Count == 0 ? 1: values.StdDev(),
                        }};
                    }
                default:
                    throw new InvalidOperationException("Unexcpected Encoding");
            }
        }

        static QueryRequest GetMainQueryRequest(PredictorMainQueryEmbedded mainQuery)
        {
            return new QueryRequest
            {
                QueryName = mainQuery.Query.ToQueryName(),

                Filters = mainQuery.Filters.Select(f => ToFilter(f)).ToList(),

                Columns = mainQuery.Columns.Select(c => new Column(c.Token.Token, null)).ToList(),

                Pagination = new Pagination.All(),
                Orders = Enumerable.Empty<Order>().ToList(),
            };
        }

        public static QueryRequest ToMultiColumnQuery(PredictorMainQueryEmbedded mainQuery, PredictorSubQueryEntity sq)
        {
            var firstGroupKey = sq.GroupKeys.FirstEx();

            var mainFilters = mainQuery.Query.Is(sq.Query) ?
                    mainQuery.Filters.Select(f => ToFilter(f)) :
                    mainQuery.Filters.Select(f => ToFilterAppend(f, firstGroupKey.Token.Token));
            var additionalFilters = sq.AdditionalFilters.Select(f => ToFilter(f)).ToList();

            var groupKeys = sq.GroupKeys.Select(c => new Column(c.Token.Token, null)).ToList();
            var aggregates = sq.Aggregates.Select(c => new Column(c.Token.Token, null)).ToList();

            return new QueryRequest
            {
                GroupResults = true,
                QueryName = sq.Query.ToQueryName(),
                Filters = mainFilters.Concat(additionalFilters).ToList(),
                Columns = groupKeys.Concat(aggregates).ToList(),
                Orders = new List<Order>(),
                Pagination = new Pagination.All(),
            };
        }

        internal static Filter ToFilter(QueryFilterEmbedded f)
        {
            return new Filter(f.Token.Token, f.Operation,
                FilterValueConverter.Parse(f.ValueString, f.Token.Token.Type,  f.Operation.IsList(), allowSmart: false));
        }

        static Filter ToFilterAppend(QueryFilterEmbedded f, QueryToken mainQueryKey)
        {
            QueryToken token = mainQueryKey.Append(f.Token.Token);

            return new Filter(token, f.Operation,
                FilterValueConverter.Parse(f.ValueString, token.Type, f.Operation.IsList(), allowSmart: false));
        }

        static QueryToken Append(this QueryToken baseToken, QueryToken suffix)
        {
            var steps = suffix.Follow(a => a.Parent).Reverse();
            var token = baseToken;
            foreach (var step in steps)
            {
                if (step.Key == "Entity" && step is ColumnToken)
                {
                    if (token.Type.CleanType() == step.Type.CleanType())
                        continue;
                    else
                        token = token.SubTokenInternal("[" + TypeLogic.GetCleanName(baseToken.Type.CleanType()) + "]", SubTokensOptions.CanElement);
                }

                token = token.SubTokenInternal(step.Key, SubTokensOptions.CanElement);
            }

            return token;
        }

    }

    public class PredictorCodification
    {
        public int Index;
        
        public PredictorColumnEmbedded PredictorColumn;
        //Index of PredictorColumn in the SimpleColumns/Aggregates
        public int? PredictorColumnIndex;

        //Only for sub queries (values inside of collections)
        public PredictorSubQueryEntity SubQuery;
        public object[] Keys;
        
        //Only for 1-hot encoding in the column (i.e: Neuronal Networks)
        public object IsValue;

        //Serves as Codification (i.e: Bayes)
        public Dictionary<object, int> ValuesToIndex;

        public float? Mean;
        public float? StdDev;

        public object[] CodedValues;

        public PredictorCodification(PredictorColumnEmbedded predictorColumn, int? predictorColumnIndex)
        {
            PredictorColumn = predictorColumn;
            PredictorColumnIndex = predictorColumnIndex;
        }

        public override string ToString()
        {
            return new[]
            {
                PredictorColumn.Usage.ToString(),
                Index.ToString(),
                PredictorColumn.Token.ToString(),
                Keys == null ? null : $"(Key={Keys.ToString(", ")})",
                IsValue == null ? null : $"(IsValue={IsValue})",
                CodedValues == null ? null : $"(Values={CodedValues.Length})",
            }.NotNull().ToString(" ");
        }

        public float Denormalize(float value)
        {
            return Mean.Value + (StdDev.Value * value);
        }
    }

    public class ObjectArrayComparer : IEqualityComparer<object[]>, IComparer<object[]>
    {
        public static readonly ObjectArrayComparer Instance = new ObjectArrayComparer();

        public int Compare(object[] x, object[] y)
        {
            if (x.Length != y.Length)
                return x.Length.CompareTo(y.Length);

            for (int i = 0; i < x.Length; i++)
            {
                var result = CompareValue(x[i], y[i]);
                if (result != 0)
                    return result;
            }
            return 0;
        }

        private int CompareValue(object v1, object v2)
        {
            if (v1 == null && v2 == null)
                return 0;

            if (v1 == null)
                return -1;

            if (v2 == null)
                return 1;

            return ((IComparable)v1).CompareTo(v2);

        }

        public bool Equals(object[] x, object[] y)
        {
            if (x.Length != y.Length)
                return false;

            for (int i = 0; i < x.Length; i++)
            {
                if (!object.Equals(x[i], y[i]))
                    return false;
            }

            return true;
        }

        public int GetHashCode(object[] array)
        {
            int hash = 17;
            foreach (var item in array)
            {
                hash = hash * 23 + ((item != null) ? item.GetHashCode() : 0);
            }
            return hash;
        }
    }
}
