/*SFF*/
/************************************************
This class has been generated dynamically.
Any changes you make here will be overwritten.
************************************************/

#region Usings
using SF.Query;
using System.Data.SqlClient;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Data;
using System.Globalization;
using System.Threading.Tasks;
#endregion

namespace SF.Repository.Common;

public static partial class Database
{
    internal static void Select(EntityQuery entityQuery, Action<SqlDataReader> materializer)
    {
        SelectQuery.Execute(entityQuery.ModelId, entityQuery.EntityId, entityQuery.Top, entityQuery.Where, entityQuery.Sort, entityQuery.Pagination, entityQuery.NavProps, materializer);
    }
    internal static Task<bool> SelectAsync(EntityQuery entityQuery, Action<SqlDataReader> materializer)
    {
        return SelectQuery.ExecuteAsync(entityQuery.ModelId, entityQuery.EntityId, entityQuery.Top, entityQuery.Where, entityQuery.Sort, entityQuery.Pagination, entityQuery.NavProps, materializer);
    }

    /// <summary>
    /// Writes SELECT statements. Each statement is measured, then written once into its final string
    /// (string.Create): the text is the same, character for character, as when it was built from pieces.
    /// </summary>
    private static class SelectQuery
    {
        private static readonly SpanAction<char, StatementText> _writeSelect = WriteSelectInto;
        private static readonly SpanAction<char, StatementText> _writeWhere = WriteWhereInto;
        private static readonly SpanAction<char, StatementText> _writeOrderBy = WriteOrderByInto;
        private static readonly SpanAction<char, NavPropText> _writeNavProp = WriteNavPropInto;
        private static readonly SpanAction<char, string[]> _writeStatements = WriteStatementsInto;

        internal static void Execute(int modelId, int entityId, int topCount, List<IQueryFilter> queryFilters, List<ISortFilter> sortFilters, IPaginationSettings paginationSettings, List<NavPropertyInfo> navProps, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.Entities[entityId];

            var query = Compose(topCount, entityInfo, queryFilters, sortFilters, paginationSettings, navProps);

            var param = Parameters.Compose(entityInfo, queryFilters, paginationSettings);

            SqlExecute.Reader(modelId, entityInfo.DbFQName, query, param, materializer, FinishBehavior.Cancel);
        }
        internal static Task<bool> ExecuteAsync(int modelId, int entityId, int topCount, List<IQueryFilter> queryFilters, List<ISortFilter> sortFilters, IPaginationSettings paginationSettings, List<NavPropertyInfo> navProps, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.Entities[entityId];

            var query = Compose(topCount, entityInfo, queryFilters, sortFilters, paginationSettings, navProps);

            var param = Parameters.Compose(entityInfo, queryFilters, paginationSettings);

            return SqlExecute.ReaderAsync(modelId, entityInfo.DbFQName, query, param, materializer, FinishBehavior.Cancel);
        }

        internal static string Compose(int topCount, EntityInfo entityInfo, List<IQueryFilter> queryFilters, List<ISortFilter> sortFilters, IPaginationSettings paginationSettings, List<NavPropertyInfo> navProps, string paramSuffix = "")
        {
            if(!(navProps?.Count > 0))
            {
                var text = new StatementText(entityInfo, topCount, queryFilters, sortFilters, paginationSettings, paramSuffix, false, null, null);

                //measuring writes the WHERE and the ORDER BY first, so their errors come before the paging one, as always
                var measure = SqlText.Measuring();
                WriteSelect(ref measure, text);
                CheckPaging(sortFilters, paginationSettings);

                return string.Create(measure.Length, text, _writeSelect);
            }

            //each nav prop's statement repeats parts of the main one: those parts are built once, and the main statement from them
            var where = GenerateWhereClause(queryFilters, entityInfo, false, paramSuffix);
            var orderBy = GenerateOrderByClause(sortFilters, entityInfo);
            CheckPaging(sortFilters, paginationSettings);
            var top = topCount > 0 ? string.Concat(_top, "(", topCount.ToString(), ") ") : string.Empty;
            var pagination = paginationSettings != null ? string.Concat(_offset, _offsetCountParam, _rows, _fetch, _fetchCountParam, _rowsOnly) : string.Empty;
            var totalCount = paginationSettings != null ? string.Concat(",", TotalRowCountParam, " = ", _countOver) : string.Empty;
            var selectForNavProp = string.Concat(_select, top, entityInfo.Projection, _from, entityInfo.AsAlias);
            var predicateSuffix = string.Concat(orderBy, pagination);
            var predicateSuffixForParentNavProperty = pagination.Length > 0 ? predicateSuffix : string.Empty;
            var topOrPagingEnabled = top.Length > 0 || pagination.Length > 0;

            var statements = new string[1 + CountNavProps(navProps)];
            statements[0] = string.Concat(_select, top, entityInfo.Projection, totalCount, _from, entityInfo.AsAlias, where.Length > 0 ? _where : string.Empty, where, predicateSuffix);
            var index = 1;
            ComposeNavProps(statements, ref index, entityInfo, entityInfo.Alias, 1, paginationSettings, navProps, selectForNavProp, where, predicateSuffix, predicateSuffixForParentNavProperty, topOrPagingEnabled, false);

            return JoinStatements(statements);
        }

        internal static string GenerateWhereClause(List<IQueryFilter> queryFilters, EntityInfo entityInfo, bool useColumnName = false, string paramSuffix = "")
        {
            if(!(queryFilters?.Count > 0))
                return string.Empty;

            var text = new StatementText(entityInfo, 0, queryFilters, null, null, paramSuffix, useColumnName, null, null);
            var measure = SqlText.Measuring();
            WriteWhere(ref measure, text.Where, text.EntityInfo, text.UseColumnName, text.ParamSuffix);
            return string.Create(measure.Length, text, _writeWhere);
        }

        private static string GenerateOrderByClause(List<ISortFilter> sortFilters, EntityInfo entityInfo)
        {
            if(!(sortFilters?.Count > 0))
                return string.Empty;

            var text = new StatementText(entityInfo, 0, null, sortFilters, null, null, false, null, null);
            var measure = SqlText.Measuring();
            WriteOrderBy(ref measure, sortFilters, entityInfo);
            return string.Create(measure.Length, text, _writeOrderBy);
        }

        private static void CheckPaging(List<ISortFilter> sortFilters, IPaginationSettings paginationSettings)
        {
            //TODO: Get the TOTAL ROWS when using paging using something other than count over.
            //This link contains good way of doing it.Look for James Moberg's answer:http://stackoverflow.com/questions/12352471/getting-total-row-count-from-offset-fetch-next
            if(paginationSettings != null && !(sortFilters?.Count > 0))
                throw new InvalidOperationException("Pagination is not allowed when no sorting criteria is present.");
        }

        #region The main statement
        private static void WriteSelectInto(Span<char> buffer, StatementText text)
        {
            var w = SqlText.Writing(buffer);
            WriteSelect(ref w, text);
            w.CheckComplete();
        }

        /// <summary>
        /// SELECT [TOP(n) ]projection[,total count] FROM table AS alias[ WHERE ...][ ORDER BY ...][ OFFSET ... ROWS ONLY]
        /// </summary>
        internal static void WriteSelect(ref SqlText w, in StatementText text)
        {
            var entityInfo = text.EntityInfo;
            var paging = text.Pagination != null;

            w.Append(_select);
            if(text.TopCount > 0)
            {
                w.Append(_top);
                w.Append('(');
                w.AppendNumber(text.TopCount);
                w.Append(") ");
            }

            w.Append(entityInfo.Projection);
            if(paging)
            {
                w.Append(',');
                w.Append(TotalRowCountParam);
                w.Append(" = ");
                w.Append(_countOver);
            }

            w.Append(_from);
            w.Append(entityInfo.AsAlias);

            if(text.Where?.Count > 0)
            {
                w.Append(_where);
                WriteWhere(ref w, text.Where, entityInfo, false, text.ParamSuffix);
            }

            WriteOrderBy(ref w, text.Sort, entityInfo);
            if(paging)
            {
                w.Append(_offset);
                w.Append(_offsetCountParam);
                w.Append(_rows);
                w.Append(_fetch);
                w.Append(_fetchCountParam);
                w.Append(_rowsOnly);
            }
        }
        #endregion

        #region WHERE
        private static void WriteWhereInto(Span<char> buffer, StatementText text)
        {
            var w = SqlText.Writing(buffer);
            WriteWhere(ref w, text.Where, text.EntityInfo, text.UseColumnName, text.ParamSuffix);
            w.CheckComplete();
        }

        internal static void WriteWhere(ref SqlText w, List<IQueryFilter> queryFilters, EntityInfo entityInfo, bool useColumnName, string paramSuffix)
        {
            if(!(queryFilters?.Count > 0))
                return;

            var count = queryFilters.Count;
            for(var i = 0; i < count; i++)
            {
                var filter = queryFilters[i];
                if(i > 0)
                    w.Append(GenerateOperator(filter.Operator));

                WriteCondition(ref w, filter, entityInfo.Props[filter.PropertyId], i, useColumnName, paramSuffix);
            }
        }

        private static string GenerateOperator(FilterOperators op)
        {
            switch(op)
            {
                case FilterOperators.And:
                    return _and;
                case FilterOperators.Or:
                    return _or;
                default:
                    throw new NotSupportedException($"Operator {op} is not supported");
            }
        }

        private static void WriteCondition(ref SqlText w, IQueryFilter filter, PropertyInfo prop, int paramOrdinal, bool useColumnName, string paramSuffix)
        {//Comparisson operators: https://msdn.microsoft.com/en-us/library/ms188074.aspx
            var colAlias = useColumnName ? prop.Column : prop.Alias;

            w.Append('(', filter.StartExpressions);
            if(filter.HasValue)
            {
                if(!filter.IsArray || prop.SqlType == SqlDbType.Timestamp)
                    WriteConditionNormal(ref w, colAlias, filter, paramOrdinal, prop, paramSuffix);
                else
                    WriteConditionWhenIsArray(ref w, colAlias, filter);
            }
            else
            {
                WriteConditionWhenNull(ref w, colAlias, filter);
            }

            w.Append(')', filter.EndExpressions);
        }

        /// <summary>
        /// The parameter's name: the property's, the suffix, then the filter's position.
        /// </summary>
        private static void WriteParam(ref SqlText w, PropertyInfo prop, string paramSuffix, int paramOrdinal)
        {
            w.Append(prop.ParamName);
            w.Append(paramSuffix);
            w.AppendNumber(paramOrdinal);
        }

        /// <summary>
        /// ({column}{op}{param})
        /// </summary>
        private static void WriteCompare(ref SqlText w, string colAlias, string op, PropertyInfo prop, string paramSuffix, int paramOrdinal)
        {
            w.Append('(');
            w.Append(colAlias);
            w.Append(op);
            WriteParam(ref w, prop, paramSuffix, paramOrdinal);
            w.Append(')');
        }

        /// <summary>
        /// ({part}({column}){op}{param}) for YEAR, MONTH and DAY
        /// </summary>
        private static void WriteDatePart(ref SqlText w, string part, string colAlias, string op, PropertyInfo prop, string paramSuffix, int paramOrdinal)
        {
            w.Append('(');
            w.Append(part);
            w.Append('(');
            w.Append(colAlias);
            w.Append(')');
            w.Append(op);
            WriteParam(ref w, prop, paramSuffix, paramOrdinal);
            w.Append(')');
        }

        /// <summary>
        /// CAST({param} AS DATE)
        /// </summary>
        private static void WriteCastToDate(ref SqlText w, PropertyInfo prop, string paramSuffix, int paramOrdinal)
        {
            w.Append("CAST(");
            WriteParam(ref w, prop, paramSuffix, paramOrdinal);
            w.Append(" AS DATE)");
        }

        /// <summary>
        /// DATEADD(dd, 1, CAST({param} AS DATE))
        /// </summary>
        private static void WriteNextDay(ref SqlText w, PropertyInfo prop, string paramSuffix, int paramOrdinal)
        {
            w.Append("DATEADD(dd, 1, ");
            WriteCastToDate(ref w, prop, paramSuffix, paramOrdinal);
            w.Append(')');
        }

        /// <summary>
        /// ({column} >= CAST({param} AS DATE)) AND ({column} {lastOp} DATEADD(dd, 1, CAST({param} AS DATE)))
        /// </summary>
        private static void WriteDateRange(ref SqlText w, string colAlias, string lastOp, PropertyInfo prop, string paramSuffix, int paramOrdinal)
        {
            w.Append('(');
            w.Append(colAlias);
            w.Append(_greaterEquals);
            WriteCastToDate(ref w, prop, paramSuffix, paramOrdinal);
            w.Append(')');
            w.Append(_and);
            w.Append('(');
            w.Append(colAlias);
            w.Append(lastOp);
            WriteNextDay(ref w, prop, paramSuffix, paramOrdinal);
            w.Append(')');
        }

        /// <summary>
        /// ({column} LIKE {param}+N'%' AND (LEFT({column}, LEN({param})) = {param})) OR ({param} = N'')
        /// and the same with RIGHT and the % in front for ends-with.
        /// </summary>
        private static void WriteStartsOrEnds(ref SqlText w, string colAlias, bool starts, string nPrefix, PropertyInfo prop, string paramSuffix, int paramOrdinal)
        {
            w.Append('(');
            w.Append(colAlias);
            w.Append(_like);
            if(starts)
            {
                WriteParam(ref w, prop, paramSuffix, paramOrdinal);
                w.Append('+');
                w.Append(nPrefix);
                w.Append("'%'");
            }
            else
            {
                w.Append(nPrefix);
                w.Append("'%'+");
                WriteParam(ref w, prop, paramSuffix, paramOrdinal);
            }

            w.Append(_and);
            w.Append('(');
            w.Append(starts ? _left : _right);
            w.Append('(');
            w.Append(colAlias);
            w.Append(", ");
            w.Append(_len);
            w.Append('(');
            WriteParam(ref w, prop, paramSuffix, paramOrdinal);
            w.Append("))");
            w.Append(_equals);
            WriteParam(ref w, prop, paramSuffix, paramOrdinal);
            w.Append("))");
            w.Append(_or);
            w.Append('(');
            WriteParam(ref w, prop, paramSuffix, paramOrdinal);
            w.Append(_equals);
            w.Append(nPrefix);
            w.Append("'')");
        }

        /// <summary>
        /// {column} LIKE N'%'+{param}+N'%'
        /// </summary>
        private static void WriteLike(ref SqlText w, string colAlias, string nPrefix, PropertyInfo prop, string paramSuffix, int paramOrdinal)
        {
            w.Append(colAlias);
            w.Append(_like);
            w.Append(nPrefix);
            w.Append("'%'+");
            WriteParam(ref w, prop, paramSuffix, paramOrdinal);
            w.Append('+');
            w.Append(nPrefix);
            w.Append("'%'");
        }

        private static void WriteConditionNormal(ref SqlText w, string colAlias, IQueryFilter filter, int paramOrdinal, PropertyInfo prop, string paramSuffix)
        {
            var nPrefix = "";
            if(prop.SqlType == SqlDbType.NVarChar || prop.SqlType == SqlDbType.NText || prop.SqlType == SqlDbType.NChar)
                nPrefix = "N";

            switch(filter.Condition)
            {
                case FilterConditions.Equals:
                    WriteCompare(ref w, colAlias, _equals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.NotEquals:
                    WriteCompare(ref w, colAlias, _notEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.Contains://TODO: Escape characters in LIKE clause??. http://stackoverflow.com/questions/20496098/sql-generated-by-entityframework-startswith-contains-plan-altering-escape
                    w.Append('(');
                    WriteLike(ref w, colAlias, nPrefix, prop, paramSuffix, paramOrdinal);
                    w.Append(')');
                    return;

                case FilterConditions.DoesNotContain:
                    w.Append("(NOT(");
                    WriteLike(ref w, colAlias, nPrefix, prop, paramSuffix, paramOrdinal);
                    w.Append("))");
                    return;

                case FilterConditions.StartsWith:
                    w.Append('(');
                    WriteStartsOrEnds(ref w, colAlias, true, nPrefix, prop, paramSuffix, paramOrdinal);
                    w.Append(')');
                    return;

                case FilterConditions.DoesNotStartWith:
                    w.Append("(NOT(");
                    WriteStartsOrEnds(ref w, colAlias, true, nPrefix, prop, paramSuffix, paramOrdinal);
                    w.Append("))");
                    return;

                case FilterConditions.EndsWith:
                    w.Append('(');
                    WriteStartsOrEnds(ref w, colAlias, false, nPrefix, prop, paramSuffix, paramOrdinal);
                    w.Append(')');
                    return;

                case FilterConditions.DoesNotEndWith:
                    w.Append("(NOT(");
                    WriteStartsOrEnds(ref w, colAlias, false, nPrefix, prop, paramSuffix, paramOrdinal);
                    w.Append(")) ");
                    return;

                case FilterConditions.GreaterThan:
                    WriteCompare(ref w, colAlias, _greater, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.LessThan:
                    WriteCompare(ref w, colAlias, _less, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.GreaterThanOrEquals:
                    WriteCompare(ref w, colAlias, _greaterEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.LessThanOrEquals:
                    WriteCompare(ref w, colAlias, _lessEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.DateEquals://this is the comparisson for DATE part only. it is a  little convoluted but it respects indexes. EF DOES NOT SUPPORT THIS! NOT EVEN COMPARING DATE part!:http://stackoverflow.com/questions/1843395/compare-two-datetime-only-by-date-not-time-in-sql-server-2008
                    w.Append('(');
                    WriteDateRange(ref w, colAlias, _less, prop, paramSuffix, paramOrdinal);
                    w.Append(')');
                    return;

                case FilterConditions.DateNotEquals:
                    w.Append("(NOT(");
                    WriteDateRange(ref w, colAlias, _less, prop, paramSuffix, paramOrdinal);
                    w.Append("))");
                    return;

                case FilterConditions.DateGreaterThan:
                    w.Append('(');
                    WriteDateRange(ref w, colAlias, _greater, prop, paramSuffix, paramOrdinal);
                    w.Append(')');
                    return;

                case FilterConditions.DateLessThan:
                    w.Append('(');
                    w.Append(colAlias);
                    w.Append(_less);
                    WriteCastToDate(ref w, prop, paramSuffix, paramOrdinal);
                    w.Append(')');
                    return;

                case FilterConditions.DateGreaterThanOrEquals:
                    w.Append('(');
                    w.Append(colAlias);
                    w.Append(_greaterEquals);
                    WriteCastToDate(ref w, prop, paramSuffix, paramOrdinal);
                    w.Append(')');
                    return;

                case FilterConditions.DateLessThanOrEquals:
                    w.Append('(');
                    w.Append(colAlias);
                    w.Append(_less);
                    WriteNextDay(ref w, prop, paramSuffix, paramOrdinal);
                    w.Append(')');
                    return;

                case FilterConditions.DateContains://TODO: Implement DateContains and DateDoesNotContain or remove them from the filters.
                case FilterConditions.DateDoesNotContain:
                    throw new NotSupportedException("This is pending implementation");

                case FilterConditions.YearEquals://Performace issue with this. SqlServer 2016 still has not fixed the indexes for this. WORKAROUND: http://www.littlekendra.com/2016/03/01/sql-servers-year-function-and-index-performance/
                    WriteDatePart(ref w, "YEAR", colAlias, _equals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.YearNotEquals:
                    WriteDatePart(ref w, "YEAR", colAlias, _notEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.YearGreaterThan:
                    WriteDatePart(ref w, "YEAR", colAlias, _greater, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.YearLessThan:
                    WriteDatePart(ref w, "YEAR", colAlias, _less, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.YearGreaterThanOrEquals:
                    WriteDatePart(ref w, "YEAR", colAlias, _greaterEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.YearLessThanOrEquals:
                    WriteDatePart(ref w, "YEAR", colAlias, _lessEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.YearContains://TODO: Year contains. This probably cannot be parameterized
                case FilterConditions.YearDoesNotContain://TODO: Year does not contain. This probably cannot be parameterized
                    throw new NotSupportedException("This is pending implementation");

                case FilterConditions.MonthEquals:
                    WriteDatePart(ref w, "MONTH", colAlias, _equals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.MonthNotEquals:
                    WriteDatePart(ref w, "MONTH", colAlias, _notEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.MonthGreaterThan:
                    WriteDatePart(ref w, "MONTH", colAlias, _greater, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.MonthLessThan:
                    WriteDatePart(ref w, "MONTH", colAlias, _less, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.MonthGreaterThanOrEquals:
                    WriteDatePart(ref w, "MONTH", colAlias, _greaterEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.MonthLessThanOrEquals:
                    WriteDatePart(ref w, "MONTH", colAlias, _lessEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.MonthContains://TODO: Month contains. This probably cannot be parameterized
                case FilterConditions.MonthDoesNotContain://TODO: MONTH does not contain. This probably cannot be parameterized
                    throw new NotSupportedException("This is pending implementation");

                case FilterConditions.DayEquals:
                    WriteDatePart(ref w, "DAY", colAlias, _equals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.DayNotEquals:
                    WriteDatePart(ref w, "DAY", colAlias, _notEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.DayGreaterThan:
                    WriteDatePart(ref w, "DAY", colAlias, _greater, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.DayLessThan:
                    WriteDatePart(ref w, "DAY", colAlias, _less, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.DayGreaterThanOrEquals:
                    WriteDatePart(ref w, "DAY", colAlias, _greaterEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.DayLessThanOrEquals:
                    WriteDatePart(ref w, "DAY", colAlias, _lessEquals, prop, paramSuffix, paramOrdinal);
                    return;

                case FilterConditions.DayContains://TODO: Day contains. This probably cannot be parameterized
                case FilterConditions.DayDoesNotContain://TODO: Day does not contain. This probably cannot be parameterized
                default:
                    throw new NotSupportedException($"FilterCondition {filter.Condition} is not supported.");
            }
        }

        private static void WriteConditionWhenNull(ref SqlText w, string colAlias, IQueryFilter filter)
        {
            switch(filter.Condition)
            {
                case FilterConditions.Equals:
                    w.Append('(');
                    w.Append(colAlias);
                    w.Append(_isNull);
                    w.Append(')');
                    return;

                case FilterConditions.NotEquals:
                    w.Append('(');
                    w.Append(colAlias);
                    w.Append(_isNotNull);
                    w.Append(')');
                    return;

                default:
                    throw new NotSupportedException($"When the value is null the only supported conditions are Equals and NotEquals.");
            }
        }

        private static void WriteConditionWhenIsArray(ref SqlText w, string colAlias, IQueryFilter filter)
        {
            var typeCode = Type.GetTypeCode(filter.Type);

            switch(filter.Condition)
            {
                case FilterConditions.Contains:
                    w.Append('(');
                    w.Append(colAlias);
                    w.Append(_in);
                    w.Append('(');
                    WriteList(ref w, typeCode, filter.Value);
                    w.Append("))");
                    return;

                case FilterConditions.DoesNotContain:
                    w.Append('(');
                    w.Append(colAlias);
                    w.Append(_notIn);
                    w.Append('(');
                    WriteList(ref w, typeCode, filter.Value);
                    w.Append("))");
                    return;

                default:
                    throw new NotSupportedException($"When the value is an array the only supported conditions are Contains and DoesNotContain.");
            }
        }

        /// <summary>
        /// The values of an IN list, separated by commas: numbers with the invariant culture, strings quoted.
        /// </summary>
        private static void WriteList(ref SqlText w, TypeCode typeCode, object val)
        {
            switch(typeCode)
            {
                case TypeCode.Int32:
                    WriteInts(ref w, val as int[]);
                    return;
                case TypeCode.Int64:
                    if(val is long[] longs)
                    {
                        CheckNotEmpty(longs.Length);
                        for(var i = 0; i < longs.Length; i++)
                        {
                            if(i > 0)
                                w.Append(',');
                            w.AppendNumber(longs[i]);
                        }
                    }
                    else
                    {
                        WriteInts(ref w, val as int[]);
                    }
                    return;
                case TypeCode.Int16:
                    var shorts = val as short[];
                    CheckNotEmpty(shorts.Length);
                    for(var i = 0; i < shorts.Length; i++)
                    {
                        if(i > 0)
                            w.Append(',');
                        w.AppendNumber(shorts[i]);
                    }
                    return;
                case TypeCode.Byte:
                    var bytes = val as byte[];
                    CheckNotEmpty(bytes.Length);
                    for(var i = 0; i < bytes.Length; i++)
                    {
                        if(i > 0)
                            w.Append(',');
                        w.AppendNumber(bytes[i]);
                    }
                    return;
                case TypeCode.String:
                    var strings = val as string[];
                    CheckNotEmpty(strings.Length);
                    for(var i = 0; i < strings.Length; i++)
                    {
                        if(i > 0)
                            w.Append(',');
                        w.AppendQuoted(strings[i]);
                    }
                    return;

                case TypeCode.Single:
                case TypeCode.Double:
                case TypeCode.Decimal:
                case TypeCode.DateTime:
                default:
                    throw new NotSupportedException($"Type {typeCode} is not supported as an array.");
            }
        }

        private static void WriteInts(ref SqlText w, int[] ints)
        {
            CheckNotEmpty(ints.Length);
            for(var i = 0; i < ints.Length; i++)
            {
                if(i > 0)
                    w.Append(',');
                w.AppendNumber(ints[i]);
            }
        }

        private static void CheckNotEmpty(int length)
        {
            if(length < 1)
                throw new ArgumentException("Empty arrays are not valid. Please pass a valid array.");
        }
        #endregion

        #region ORDER BY
        private static void WriteOrderByInto(Span<char> buffer, StatementText text)
        {
            var w = SqlText.Writing(buffer);
            WriteOrderBy(ref w, text.Sort, text.EntityInfo);
            w.CheckComplete();
        }

        private static void WriteOrderBy(ref SqlText w, List<ISortFilter> sortFilters, EntityInfo entityInfo)
        {
            if(!(sortFilters?.Count > 0))
                return;

            var count = sortFilters.Count;
            for(var i = 0; i < count; i++)
            {
                var filter = sortFilters[i];
                w.Append(i == 0 ? _orderBy : ", ");
                w.Append(entityInfo.Props[filter.PropertyId].Alias);
                w.Append(GenerateSortDirection(filter.SortDirection));
            }
        }

        private static string GenerateSortDirection(SortDirection dir)
        {
            switch(dir)
            {
                case SortDirection.Ascending:
                    return _asc;
                case SortDirection.Descending:
                    return _desc;
                default:
                    throw new NotSupportedException($"SortDirection {dir} is not supported.");
            }
        }
        #endregion

        #region Nav props
        /// <summary>
        /// What one nav prop's statement is written from.
        /// </summary>
        private readonly struct NavPropText
        {
            public NavPropText(EntityInfo entityInfo, string ownerAlias, NavPropertyInfo navProp, string alias, bool paging, string select, string where, string predicateSuffix, bool topOrPagingEnabled, bool ownerIsParent)
            {
                EntityInfo = entityInfo;
                OwnerAlias = ownerAlias;
                NavProp = navProp;
                Alias = alias;
                Paging = paging;
                Select = select;
                Where = where;
                PredicateSuffix = predicateSuffix;
                TopOrPagingEnabled = topOrPagingEnabled;
                OwnerIsParent = ownerIsParent;
            }

            public EntityInfo EntityInfo { get; }
            /// <summary>
            /// The owner's alias in the statement this one is built on: its own alias, unless that table was already in the statement.
            /// </summary>
            public string OwnerAlias { get; }
            public NavPropertyInfo NavProp { get; }
            /// <summary>
            /// The nav prop table's alias: its own, unless that table is already in the statement this one is built on.
            /// </summary>
            public string Alias { get; }
            public bool Paging { get; }
            public string Select { get; }
            public string Where { get; }
            public string PredicateSuffix { get; }
            public bool TopOrPagingEnabled { get; }
            public bool OwnerIsParent { get; }
        }

        private static int CountNavProps(List<NavPropertyInfo> navProps)
        {
            if(!(navProps?.Count > 0))
                return 0;

            var count = 0;
            foreach(var navProp in navProps)
                count += 1 + CountNavProps(navProp.NavProps);

            return count;
        }

        private static void ComposeNavProps(string[] statements, ref int index, EntityInfo entityInfo, string ownerAlias, int depth, IPaginationSettings paginationSettings, List<NavPropertyInfo> navProps, string selectForNavProp, string where, string predicateSuffix, string predicateSuffixForParentNavProperty, bool topOrPagingEnabled, bool ownerIsParent)
        {
            if(!(navProps?.Count > 0))
                return;

            foreach(var navProp in navProps)
            {
                var suffix = navProp.IsParent ? predicateSuffixForParentNavProperty : predicateSuffix;
                var subEntity = RepoLookup.Entities[navProp.EntityId];
                var alias = subEntity.Alias;
                //a table already in the statement (a self-referencing foreign key, or a chain back to a table) gets an alias of its own: [D] becomes [D_1], [D_2] one level down
                if(selectForNavProp.Contains(subEntity.AsAlias, StringComparison.Ordinal))
                    alias = string.Concat(alias.Substring(0, alias.Length - 1), "_", depth.ToString(CultureInfo.InvariantCulture), "]");

                var text = new NavPropText(entityInfo, ownerAlias, navProp, alias, paginationSettings != null, selectForNavProp, where, suffix, topOrPagingEnabled, ownerIsParent);
                var measure = SqlText.Measuring();
                WriteNavProp(ref measure, text);
                var navPropSelect = string.Create(measure.Length, text, _writeNavProp);

                statements[index++] = navPropSelect;
                ComposeNavProps(statements, ref index, subEntity, alias, depth + 1, paginationSettings, navProp.NavProps, navPropSelect, where, predicateSuffix, predicateSuffixForParentNavProperty, topOrPagingEnabled, navProp.IsParent);
            }
        }

        private static void WriteNavPropInto(Span<char> buffer, NavPropText text)
        {
            var w = SqlText.Writing(buffer);
            WriteNavProp(ref w, text);
            w.CheckComplete();
        }

        private static void WriteNavProp(ref SqlText w, in NavPropText text)
        {
            var entityInfo = text.EntityInfo;
            var otherEntityInfo = RepoLookup.Entities[text.NavProp.EntityId];

            w.Append(_select);
            if(text.Alias == otherEntityInfo.Alias)
            {
                w.Append(otherEntityInfo.Projection);
                w.Append(_from);
                w.Append(otherEntityInfo.AsAlias);
            }
            else
            {
                WriteProjection(ref w, otherEntityInfo, text.Alias);
                w.Append(_from);
                w.Append(otherEntityInfo.DbFQName);
                w.Append(_as);
                w.Append(text.Alias);
            }

            if(text.NavProp.IsParent && !text.Paging)
            {
                //SELECT ... WHERE EXISTS(select WHERE (predicate) AND (where) suffix)
                w.Append(_whereExists);
                w.Append('(');
                w.Append(text.Select);
                w.Append(text.OwnerIsParent ? _and : _where);
                w.Append('(');
                WriteNavPredicate(ref w, entityInfo, otherEntityInfo, text);
                w.Append(')');
                if(!string.IsNullOrWhiteSpace(text.Where) && !text.OwnerIsParent)
                {
                    w.Append(_and);
                    w.Append('(');
                    w.Append(text.Where);
                    w.Append(')');
                }

                w.Append(text.PredicateSuffix);
                w.Append(')');
                return;
            }

            if(text.NavProp.IsParent)
            {
                //this is a way to get nav props correct when using pagination
                //    INNER JOIN(
                //    SELECT DISTINCT [x].*
                //    FROM(
                //      //the main select goes here
                //    ) AS[x]
                //) AS[D3] ON nav prop predicate goes here
                w.Append(_innerJoin);
                w.Append('(');
                w.Append(_select);
                w.Append(_distinct);
                w.Append("[x].*");
                w.Append(_from);
                w.Append('(');
                w.Append(text.Select);
                if(!string.IsNullOrWhiteSpace(text.Where))
                {
                    w.Append(_where);
                    w.Append('(');
                    w.Append(text.Where);
                    w.Append(')');
                }

                w.Append(text.PredicateSuffix);
                w.Append(") AS [x]) AS ");
                w.Append(text.OwnerAlias);
                w.Append(" ON ");
                WriteNavPredicate(ref w, entityInfo, otherEntityInfo, text);
                return;
            }

            //a child: INNER JOIN (the main select) AS alias ON predicate
            w.Append(_innerJoin);
            w.Append('(');
            w.Append(text.Select);
            if(!text.OwnerIsParent && text.Where.Length > 0)
            {
                w.Append(_where);
                w.Append(text.Where);
            }

            if(text.TopOrPagingEnabled)
                w.Append(text.PredicateSuffix);

            w.Append(')');
            w.Append(_as);
            w.Append(text.OwnerAlias);
            w.Append(_on);
            WriteNavPredicate(ref w, entityInfo, otherEntityInfo, text);
        }

        /// <summary>
        /// alias.column = other alias.column[ AND ...] for each pair of the foreign key
        /// </summary>
        private static void WriteNavPredicate(ref SqlText w, EntityInfo entityInfo, EntityInfo otherEntityInfo, in NavPropText text)
        {
            var navProp = text.NavProp;
            var predicateCount = navProp.Predicate.Count;
            for(var i = 0; i < predicateCount; i++)
            {
                if(i > 0)
                    w.Append(_and);

                WriteColumn(ref w, entityInfo, entityInfo.Props[navProp.Predicate[i].PropId], text.OwnerAlias);
                w.Append(_equals);
                WriteColumn(ref w, otherEntityInfo, otherEntityInfo.Props[navProp.Predicate[i].OtherPropId], text.Alias);
            }
        }

        /// <summary>
        /// alias.column: the column's own alias text, unless the table has another alias in this statement
        /// </summary>
        private static void WriteColumn(ref SqlText w, EntityInfo entityInfo, PropertyInfo prop, string alias)
        {
            if(alias == entityInfo.Alias)
            {
                w.Append(prop.Alias);
                return;
            }

            w.Append(alias);
            w.Append('.');
            w.Append(prop.Column);
        }

        /// <summary>
        /// The table's projection with another alias: the columns are in the same order as its own projection.
        /// </summary>
        private static void WriteProjection(ref SqlText w, EntityInfo entityInfo, string alias)
        {
            var first = true;
            foreach(var prop in entityInfo.Props.Values)
            {
                if(!first)
                    w.Append(',');

                first = false;
                w.Append(alias);
                w.Append('.');
                w.Append(prop.Column);
            }
        }

        /// <summary>
        /// The main statement and the nav props' statements, each after a semicolon and a new line.
        /// </summary>
        private static string JoinStatements(string[] statements)
        {
            var separator = 1 + Environment.NewLine.Length;
            var length = 0;
            for(var i = 0; i < statements.Length; i++)
                length += statements[i].Length + (i > 0 ? separator : 0);

            return string.Create(length, statements, _writeStatements);
        }

        private static void WriteStatementsInto(Span<char> buffer, string[] statements)
        {
            var w = SqlText.Writing(buffer);
            for(var i = 0; i < statements.Length; i++)
            {
                if(i > 0)
                {
                    w.Append(';');
                    w.Append(Environment.NewLine);
                }

                w.Append(statements[i]);
            }

            w.CheckComplete();
        }
        #endregion
    }
}
