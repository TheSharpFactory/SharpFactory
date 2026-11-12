/*SFF*/
/************************************************
This class has been generated dynamically.
Any changes you make here will be overwritten.
************************************************/

#region Usings
using SF.Query;
using System.Data.SqlClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlTypes;
using System.Globalization;
using System.Threading.Tasks;
using System.Xml.Linq;
#endregion

namespace SF.Repository.Common;

public static partial class Database
{
    private enum FinishBehavior : int
    {
        Cancel,
        CloseReader,
        DisposeCommand,
    }
    private static readonly Dictionary<int, string> _conStrings = new Dictionary<int, string>();
    public static bool LogEnabled { get; set; } = false;
    public static Action<string, string> Logger { get; set; } = null;

    internal static readonly int ReturnParamId = -1000000;
    internal static readonly string TotalRowCountParam = "TRC_Internal";
    private static readonly string _select = "SELECT ";
    private static readonly string _distinct = "DISTINCT ";
    private static readonly string _insert = "INSERT INTO ";
    private static readonly string _update = "UPDATE ";
    private static readonly string _delete = "DELETE ";
    private static readonly string _from = " FROM ";
    private static readonly string _set = " SET ";
    private static readonly string _top = " TOP";
    private static readonly string _asc = " ASC";
    private static readonly string _desc = " DESC";
    private static readonly string _offset = " OFFSET ";//info on paging https://www.mssqltips.com/sqlservertip/2362/overview-of-offset-and-fetch-feature-of-sql-server-2012/
    private static readonly string _fetch = " FETCH NEXT ";//info on paging https://technet.microsoft.com/en-us/library/gg699618(v=sql.110).aspx
    private static readonly string _fetchCountParam = "@fetchCount";
    private static readonly string _offsetCountParam = "@offsetCount";
    private static readonly string _countOver = "COUNT(*) OVER()";
    private static readonly string _rowsOnly = " ROWS ONLY";
    private static readonly string _rows = " ROWS";
    private static readonly string _where = " WHERE ";
    private static readonly string _whereExists = " WHERE EXISTS ";
    private static readonly string _like = " LIKE ";
    private static readonly string _len = "LEN";
    private static readonly string _left = "LEFT";
    private static readonly string _right = "RIGHT";
    private static readonly string _equals = " = ";
    private static readonly string _notEquals = " <> ";
    private static readonly string _greater = " > ";
    private static readonly string _less = " < ";
    private static readonly string _greaterEquals = " >= ";
    private static readonly string _lessEquals = " <= ";
    private static readonly string _and = " AND ";
    private static readonly string _or = " OR ";
    private static readonly string _in = " IN ";
    private static readonly string _notIn = " NOT IN ";
    private static readonly string _as = " AS ";
    private static readonly string _null = "NULL";
    private static readonly string _isNull = " IS NULL";
    private static readonly string _isNotNull = " IS NOT NULL";
    //private static readonly string _year = "YEAR";
    //private static readonly string _month = "MONTH";
    //private static readonly string _day = "DAY";
    //private static readonly string _datePart = "DATEPART";
    private static readonly string _orderBy = " ORDER BY ";
    //private static readonly string _join = " JOIN ";
    private static readonly string _on = " ON ";
    //private static readonly string _rowlock = " (ROWLOCK) ";//all supported HINTS: https://msdn.microsoft.com/en-us/library/ms187373.aspx
    //private static readonly string _updlock = " (UPDLOCK) ";
    //private static readonly string _nolock = " (NOLOCK) ";
    private static readonly string _output = " OUTPUT ";
    //private static readonly string _scopeIdentity = "SCOPE_IDENTITY()";
    private static readonly string _innerJoin = " INNER JOIN ";
    private static readonly string _getDateSql = "SELECT SYSDATETIME() AS Expr1";
    private static readonly string _getUtcDateSql = "SELECT SYSUTCDATETIME() AS Expr1";
    private static readonly string _getTimeZoneOffsetSql = "SELECT DATEPART(tz, SYSDATETIMEOFFSET()) AS Expr1";

    private static readonly string _nextValueFor = "NEXT VALUE FOR ";
    /// <summary>
    /// Registers one model's connection string. Call it once per model, when the application starts, before any query
    /// runs: a second call for the same model throws, and the registry is not meant to change while queries read it.
    /// </summary>
    public static void RegisterModelConnectionString(int modelId, string connectionString)
    {
        if(!_conStrings.TryAdd(modelId, connectionString))
            throw new ArgumentException($"A connection string for model with id {modelId} already exists");
    }

    internal static DateTime GetStoreDateTime(int modelId)
    {
        return SqlExecute.Scalar<DateTime>(modelId, "StoreQuery", _getDateSql, null, FinishBehavior.DisposeCommand);
    }
    internal static Task<DateTime> GetStoreDateTimeAsync(int modelId)
    {
        return SqlExecute.ScalarAsync<DateTime>(modelId, "StoreQuery", _getDateSql, null, FinishBehavior.DisposeCommand);
    }

    internal static DateTime GetStoreUtcDateTime(int modelId)
    {
        return SqlExecute.Scalar<DateTime>(modelId, "StoreQuery", _getUtcDateSql, null, FinishBehavior.DisposeCommand);
    }
    internal static Task<DateTime> GetStoreUtcDateTimeAsync(int modelId)
    {
        return SqlExecute.ScalarAsync<DateTime>(modelId, "StoreQuery", _getUtcDateSql, null, FinishBehavior.DisposeCommand);
    }

    internal static int GetServerTimeZoneOffsetInMinutes(int modelId)
    {
        return SqlExecute.Scalar<int>(modelId, "StoreQuery", _getTimeZoneOffsetSql, null, FinishBehavior.DisposeCommand);
    }
    internal static Task<int> GetServerTimeZoneOffsetInMinutesAsync(int modelId)
    {
        return SqlExecute.ScalarAsync<int>(modelId, "StoreQuery", _getTimeZoneOffsetSql, null, FinishBehavior.DisposeCommand);
    }

    internal static bool GetDataBaseHeartBeat(int modelId)
    {
        var heartBeatConnString = _conStrings[modelId].Replace(";multipleactiveresultsets=True", "") + ";Connect Timeout=1";
        var sqlCon = (SqlConnection)null;

        try
        {
            sqlCon = new SqlConnection(heartBeatConnString);
            sqlCon.Open();
            return true;
        }
        catch
        {
            return false;
        }
        finally
        {
            if(sqlCon != null)
            {
                if(sqlCon.State != ConnectionState.Closed)
                    sqlCon.Close();

                sqlCon.Dispose();
                sqlCon = null;
            }
        }
    }

    /// <summary>
    /// Writes SQL text in two passes: the first only measures it, the second writes it into the final string
    /// (string.Create), so a statement costs one string and nothing else: no pieces, no pooled buffers.
    /// </summary>
    private ref struct SqlText
    {
        private readonly Span<char> _buffer;
        private readonly bool _measuring;
        private int _length;

        private SqlText(Span<char> buffer, bool measuring)
        {
            _buffer = buffer;
            _measuring = measuring;
            _length = 0;
        }

        public static SqlText Measuring()
        {
            return new SqlText(default, true);
        }

        public static SqlText Writing(Span<char> buffer)
        {
            return new SqlText(buffer, false);
        }

        public readonly int Length
        {
            get
            {
                return _length;
            }
        }

        public void Append(string value)
        {
            if(value == null)
                return;

            if(!_measuring)
                value.AsSpan().CopyTo(_buffer.Slice(_length));

            _length += value.Length;
        }

        public void Append(char value)
        {
            if(!_measuring)
                _buffer[_length] = value;

            _length++;
        }

        public void Append(char value, int count)
        {
            if(count < 1)
                return;

            if(!_measuring)
                _buffer.Slice(_length, count).Fill(value);

            _length += count;
        }

        /// <summary>
        /// A number as SQL Server reads it (invariant culture: digits and '-'), without making a string of it.
        /// </summary>
        public void AppendNumber(long value)
        {
            if(_measuring)
            {
                //the digits, and the invariant minus sign: what the writing branch writes
                var magnitude = value < 0 ? (ulong)(-(value + 1)) + 1 : (ulong)value;
                _length += CountDigits(magnitude);
                if(value < 0)
                    _length++;

                return;
            }

            value.TryFormat(_buffer.Slice(_length), out var written, ReadOnlySpan<char>.Empty, CultureInfo.InvariantCulture);
            _length += written;
        }

        private static int CountDigits(ulong value)
        {
            var digits = 1;
            while(value >= 10)
            {
                value /= 10;
                digits++;
            }

            return digits;
        }

        /// <summary>
        /// A string value between quotes, its quotes doubled.
        /// </summary>
        public void AppendQuoted(string value)
        {
            var quotes = value.AsSpan().Count('\'');
            if(_measuring)
            {
                _length += value.Length + quotes + 2;
                return;
            }

            Append('\'');
            if(quotes == 0)
            {
                Append(value);
            }
            else
            {
                for(var i = 0; i < value.Length; i++)
                {
                    var c = value[i];
                    Append(c);
                    if(c == '\'')
                        Append('\'');
                }
            }

            Append('\'');
        }

        /// <summary>
        /// After the writing pass: it must have written exactly what the measuring pass counted.
        /// </summary>
        public readonly void CheckComplete()
        {
            if(!_measuring && _length != _buffer.Length)
                throw new InvalidOperationException("The SQL text changed while it was being written.");
        }
    }

    /// <summary>
    /// What a statement is written from, carried into string.Create.
    /// </summary>
    private readonly struct StatementText
    {
        public StatementText(EntityInfo entityInfo, int topCount, List<IQueryFilter> where, List<ISortFilter> sort, IPaginationSettings pagination, string paramSuffix, bool useColumnName, List<IQueryFilter> values, string valuesSuffix)
        {
            EntityInfo = entityInfo;
            TopCount = topCount;
            Where = where;
            Sort = sort;
            Pagination = pagination;
            ParamSuffix = paramSuffix ?? string.Empty;
            UseColumnName = useColumnName;
            Values = values;
            ValuesSuffix = valuesSuffix ?? string.Empty;
        }

        public EntityInfo EntityInfo { get; }
        public int TopCount { get; }
        public List<IQueryFilter> Where { get; }
        public List<ISortFilter> Sort { get; }
        public IPaginationSettings Pagination { get; }
        public string ParamSuffix { get; }
        public bool UseColumnName { get; }
        public List<IQueryFilter> Values { get; }
        public string ValuesSuffix { get; }
    }

    private static class Parameters
    {
        internal static SqlParameter[] Compose(EntityInfo entityInfo, List<IQueryFilter> queryFilters, IPaginationSettings paginationSettings, string paramSuffix = "")
        {
            var filterParamCount = 0;
            var pagingParamCount = 0;

            if(queryFilters?.Count > 0)
                filterParamCount = queryFilters.Count;

            if(paginationSettings != null)
                pagingParamCount = 2;

            var totalParamCount = filterParamCount + pagingParamCount;
            if(totalParamCount < 1)
                return null;

            //not every filter is a parameter (an IN list of numbers or strings is written into the SQL): count them first, for an array of the exact size
            var parameterCount = pagingParamCount;
            for(var i = 0; i < filterParamCount; i++)
            {
                if(IsParameter(queryFilters[i], entityInfo))
                    parameterCount++;
            }

            var result = new SqlParameter[parameterCount];
            var index = 0;

            for(var i = 0; i < filterParamCount; i++)
            {
                var filter = queryFilters[i];
                if(!IsParameter(filter, entityInfo))
                    continue;

                var prop = entityInfo.Props[filter.PropertyId];

                var size = GetParamSize(prop, filter);

                var paramName = string.Concat(prop.ParamName, paramSuffix, i.ToString());

                var param = size.HasValue ? new SqlParameter(paramName, GetSqlDbTypeForParam(filter.Condition, prop), size.Value) : new SqlParameter(paramName, GetSqlDbTypeForParam(filter.Condition, prop));

                if(prop.SqlType == SqlDbType.Udt)
                    param.UdtTypeName = prop.UdtTypeName;

                param.IsNullable = filter.IsNullable;

                //at this point this filter has a value
                if(prop.SqlType == SqlDbType.Xml)
                {
                    using(var xReader = ((XDocument)filter.Value).Root.CreateReader())
                    {
                        param.Value = new SqlXml(xReader);
                        xReader.Close();
                    }
                }
                else
                {
                    param.Value = filter.Value;
                }

                result[index++] = param;
            }

            if(pagingParamCount > 0)
            {
                var offsetParam = new SqlParameter(_offsetCountParam, SqlDbType.Int, 4);
                var fetchParam = new SqlParameter(_fetchCountParam, SqlDbType.Int, 4);
                offsetParam.IsNullable = false;
                fetchParam.IsNullable = false;
                offsetParam.Value = paginationSettings.PageNumberForRep * paginationSettings.PageSize;
                fetchParam.Value = paginationSettings.PageSize;
                result[index++] = offsetParam;
                result[index++] = fetchParam;
            }

            return result;
        }

        /// <summary>
        /// True when the filter is sent as a parameter: it has a value, and it is not an IN list written into the SQL
        /// (binary values are always parameters).
        /// </summary>
        private static bool IsParameter(IQueryFilter filter, EntityInfo entityInfo)
        {
            if(!filter.HasValue)
                return false;

            if(!filter.IsArray)
                return true;

            var sqlType = entityInfo.Props[filter.PropertyId].SqlType;
            return sqlType == SqlDbType.Binary || sqlType == SqlDbType.VarBinary || sqlType == SqlDbType.Image || sqlType == SqlDbType.Timestamp;
        }

        internal static SqlParameter[] ComposeForInsert(EntityInfo entityInfo, List<IQueryFilter> queryFilters)
        {
            var filterParamCount = 0;

            if(queryFilters?.Count > 0)
                filterParamCount = queryFilters.Count;

            if(filterParamCount < 1)
                return null;

            var result = new List<SqlParameter>(filterParamCount);

            for(var i = 0; i < filterParamCount; i++)
            {
                var filter = queryFilters[i];
                if(!filter.HasValue && !filter.IsNullable)
                    throw new ArgumentNullException($"Property {filter.PropertyName} is not nullable.");

                var prop = entityInfo.Props[filter.PropertyId];

                if(filter.IsArray)
                {
                    var sqlType = prop.SqlType;
                    if(sqlType != SqlDbType.Binary && sqlType != SqlDbType.VarBinary && sqlType != SqlDbType.Image && sqlType != SqlDbType.Timestamp)
                        continue;
                }

                var size = GetParamSize(prop, filter);

                var param = size.HasValue ? new SqlParameter(prop.ParamName, prop.SqlType, size.Value) : new SqlParameter(prop.ParamName, prop.SqlType);

                if(prop.SqlType == SqlDbType.Udt)
                    param.UdtTypeName = prop.UdtTypeName;

                param.IsNullable = filter.IsNullable;

                if(filter.Value == null)
                {
                    param.Value = DBNull.Value;
                }
                else
                {
                    if(prop.SqlType == SqlDbType.Xml)
                    {
                        using(var xReader = ((XDocument)filter.Value).Root.CreateReader())
                        {
                            param.Value = new SqlXml(xReader);
                            xReader.Close();
                        }
                    }
                    else
                    {
                        param.Value = filter.Value;
                    }
                }

                result.Add(param);
            }

            return result.ToArray();
        }

        internal static StoredProcParams ComposeForStoredProc(EntityInfo entityInfo, List<IQueryFilter> queryFilters)
        {
            var filterParamCount = 0;

            if(queryFilters?.Count > 0)
                filterParamCount = queryFilters.Count;

            var resultParams = new StoredProcParams { OutParams = new Dictionary<int, SqlParameter>(filterParamCount) };
            var result = new List<SqlParameter>(filterParamCount);

            if(filterParamCount > 0)
            {
                for(var i = 0; i < filterParamCount; i++)
                {
                    var filter = queryFilters[i];
                    if(!filter.HasValue && !filter.IsNullable)
                        throw new ArgumentNullException($"Property {filter.PropertyName} is not nullable.");

                    var prop = entityInfo.Props[filter.PropertyId];

                    if(filter.IsArray)
                    {
                        var sqlType = prop.SqlType;
                        if(sqlType != SqlDbType.Binary && sqlType != SqlDbType.VarBinary && sqlType != SqlDbType.Image && sqlType != SqlDbType.Timestamp)
                            continue;
                    }

                    var size = GetParamSize(prop, filter);

                    var param = size.HasValue ? new SqlParameter(prop.ParamName, prop.SqlType, size.Value) : new SqlParameter(prop.ParamName, prop.SqlType);

                    if(prop.SqlType == SqlDbType.Udt)
                        param.UdtTypeName = prop.UdtTypeName;

                    param.Direction = prop.Direction;
                    param.IsNullable = filter.IsNullable;

                    if(filter.Value == null)
                    {
                        param.Value = DBNull.Value;
                    }
                    else
                    {
                        if(prop.SqlType == SqlDbType.Xml)
                        {
                            using(var xReader = ((XDocument)filter.Value).Root.CreateReader())
                            {
                                param.Value = new SqlXml(xReader);
                                xReader.Close();
                            }
                        }
                        else
                        {
                            param.Value = filter.Value;
                        }
                    }

                    result.Add(param);

                    if(param.Direction == ParameterDirection.InputOutput || param.Direction == ParameterDirection.Output)
                        resultParams.OutParams.Add(filter.PropertyId, param);
                }
            }
            //add the return param                  
            result.Add(new SqlParameter("Return_Value", SqlDbType.Int) { Direction = ParameterDirection.ReturnValue });
            resultParams.Parameters = result.ToArray();

            return resultParams;
        }

        private static int? GetParamSize(PropertyInfo prop, IQueryFilter filter)
        {//info about the Size property of the Parameter object : https://msdn.microsoft.com/en-us/library/system.data.sqlclient.sqlparameter.size(v=vs.110).aspx
            var size = -1;//TODO: Test sending empty strings to the db and also null. Also test sending different length strings for fixed size chars, nchar and binary
            switch(prop.SqlType)
            {
                case SqlDbType.VarChar:
                case SqlDbType.NVarChar:
                    if(prop.Size.HasValue)
                    {
                        if(prop.Size.Value > -1)
                            return prop.Size;
                    }

                    size = prop.SqlType == SqlDbType.VarChar ? 8000 : 4000;

                    if(filter.Value != null && filter.Value.ToString().Length > size)
                        return -1;
                    return size;
                case SqlDbType.NText:
                case SqlDbType.Text:
                    if(filter.Value != null)
                        return filter.Value.ToString().Length;
                    return size;
                case SqlDbType.Binary:
                case SqlDbType.VarBinary:
                    if(prop.Size.HasValue)
                    {
                        if(prop.Size.Value > -1)
                            return prop.Size;
                    }

                    size = 8000;

                    if(filter.Value is byte[] val2 && val2.Length > size)
                        return -1;
                    return size;
                default:
                    return null;
            }
        }
        private static SqlDbType GetSqlDbTypeForParam(FilterConditions condition, PropertyInfo prop)
        {//in some cases the SqlDbType is different than the columns
            switch(condition)
            {
                case FilterConditions.Equals:
                case FilterConditions.NotEquals:
                case FilterConditions.Contains:
                case FilterConditions.DoesNotContain:
                case FilterConditions.StartsWith:
                case FilterConditions.DoesNotStartWith:
                case FilterConditions.EndsWith:
                case FilterConditions.DoesNotEndWith:
                case FilterConditions.GreaterThan:
                case FilterConditions.LessThan:
                case FilterConditions.GreaterThanOrEquals:
                case FilterConditions.LessThanOrEquals:
                case FilterConditions.DateEquals:
                case FilterConditions.DateNotEquals:
                case FilterConditions.DateGreaterThan:
                case FilterConditions.DateLessThan:
                case FilterConditions.DateGreaterThanOrEquals:
                case FilterConditions.DateLessThanOrEquals:
                case FilterConditions.DateContains:
                case FilterConditions.DateDoesNotContain:
                    return prop.SqlType;

                case FilterConditions.YearEquals:
                case FilterConditions.YearNotEquals:
                case FilterConditions.YearGreaterThan:
                case FilterConditions.YearLessThan:
                case FilterConditions.YearGreaterThanOrEquals:
                case FilterConditions.YearLessThanOrEquals:
                case FilterConditions.MonthEquals:
                case FilterConditions.MonthNotEquals:
                case FilterConditions.MonthGreaterThan:
                case FilterConditions.MonthLessThan:
                case FilterConditions.MonthGreaterThanOrEquals:
                case FilterConditions.MonthLessThanOrEquals:
                case FilterConditions.DayEquals:
                case FilterConditions.DayNotEquals:
                case FilterConditions.DayGreaterThan:
                case FilterConditions.DayLessThan:
                case FilterConditions.DayGreaterThanOrEquals:
                case FilterConditions.DayLessThanOrEquals:
                    return SqlDbType.Int;

                case FilterConditions.YearContains:
                case FilterConditions.YearDoesNotContain:
                case FilterConditions.MonthContains:
                case FilterConditions.MonthDoesNotContain:
                case FilterConditions.DayContains:
                case FilterConditions.DayDoesNotContain:
                default:
                    throw new NotSupportedException("Condition is not implemented.");
            }
        }
    }
    private static class Utils
    {
        internal static int[] PreCalcReaderIndexes(int startIndex, int count)
        {
            var result = new int[count];

            for(var i = 0; i < count; i++)
                result[i] = startIndex++;

            return result;
        }
    }
    private static class SqlExecute
    {
        internal static void Reader(int modelId, string dbFQName, string query, SqlParameter[] param, Action<SqlDataReader> materializer, FinishBehavior queryType, CommandType cmdType = CommandType.Text)
        {
            SqlDataReader reader = null;
            SqlConnection con = null;
            SqlCommand cmd = null;
            try
            {
                if(LogEnabled)
                    Logger(dbFQName, query);

                con = new SqlConnection(_conStrings[modelId]);
                cmd = new SqlCommand(query, con) { CommandType = cmdType };
                if(param != null)
                    cmd.Parameters.AddRange(param);
                con.Open();
                reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
                materializer.Invoke(reader);
            }
            finally
            {
                DisposeSqlObjects(con, cmd, reader, queryType);
            }
        }
        internal static async Task<bool> ReaderAsync(int modelId, string dbFQName, string query, SqlParameter[] param, Action<SqlDataReader> materializer, FinishBehavior queryType, CommandType cmdType = CommandType.Text)
        {
            SqlDataReader reader = null;
            SqlConnection con = null;
            SqlCommand cmd = null;
            try
            {
                if(LogEnabled)
                    Logger(dbFQName, query);

                con = new SqlConnection(_conStrings[modelId]);
                cmd = new SqlCommand(query, con) { CommandType = cmdType };
                if(param != null)
                    cmd.Parameters.AddRange(param);

                await con.OpenAsync();
                reader = await cmd.ExecuteReaderAsync(CommandBehavior.SequentialAccess);
                materializer.Invoke(reader);
                return true;
            }
            finally
            {
                DisposeSqlObjects(con, cmd, reader, queryType);
            }
        }

        internal static int NonQuery(int modelId, string table, string query, SqlParameter[] param, FinishBehavior queryType, CommandType cmdType = CommandType.Text)
        {
            SqlConnection con = null;
            SqlCommand cmd = null;
            try
            {
                if(LogEnabled)
                    Logger(table, query);

                con = new SqlConnection(_conStrings[modelId]);
                cmd = new SqlCommand(query, con) { CommandType = cmdType };
                if(param != null)
                    cmd.Parameters.AddRange(param);
                con.Open();
                return cmd.ExecuteNonQuery();
            }
            finally
            {
                DisposeSqlObjects(con, cmd, null, queryType);
            }
        }
        internal static async Task<int> NonQueryAsync(int modelId, string table, string query, SqlParameter[] param, FinishBehavior queryType, CommandType cmdType = CommandType.Text)
        {
            SqlConnection con = null;
            SqlCommand cmd = null;
            try
            {
                if(LogEnabled)
                    Logger(table, query);

                con = new SqlConnection(_conStrings[modelId]);
                cmd = new SqlCommand(query, con) { CommandType = cmdType };
                if(param != null)
                    cmd.Parameters.AddRange(param);

                await con.OpenAsync();
                return await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                DisposeSqlObjects(con, cmd, null, queryType);
            }
        }

        internal static TScalarResult Scalar<TScalarResult>(int modelId, string table, string query, SqlParameter[] param, FinishBehavior queryType, CommandType cmdType = CommandType.Text)
        {
            SqlConnection con = null;
            SqlCommand cmd = null;
            try
            {
                if(LogEnabled)
                    Logger(table, query);

                con = new SqlConnection(_conStrings[modelId]);
                cmd = new SqlCommand(query, con) { CommandType = cmdType };

                if(param != null)
                    cmd.Parameters.AddRange(param);
                con.Open();

                var result = cmd.ExecuteScalar();

                if(result == null)
                    return default;

                return (TScalarResult)result;
            }
            finally
            {
                DisposeSqlObjects(con, cmd, null, queryType);
            }
        }
        internal static async Task<TScalarResult> ScalarAsync<TScalarResult>(int modelId, string table, string query, SqlParameter[] param, FinishBehavior queryType, CommandType cmdType = CommandType.Text)
        {
            SqlConnection con = null;
            SqlCommand cmd = null;
            try
            {
                if(LogEnabled)
                    Logger(table, query);

                con = new SqlConnection(_conStrings[modelId]);
                cmd = new SqlCommand(query, con) { CommandType = cmdType };

                if(param != null)
                    cmd.Parameters.AddRange(param);

                await con.OpenAsync();

                var result = await cmd.ExecuteScalarAsync();

                if(result == null)
                    return default;

                return (TScalarResult)result;
            }
            finally
            {
                DisposeSqlObjects(con, cmd, null, queryType);
            }
        }

        private static void DisposeSqlObjects(SqlConnection con, SqlCommand cmd, SqlDataReader reader, FinishBehavior queryType)
        {
            if(cmd != null)
            {
                cmd.Cancel();
                cmd.Dispose();
            }

            if(con != null)
            {
                if(con.State != ConnectionState.Closed)
                    con.Close();
                con.Dispose();
            }

            switch(queryType)
            {
                case FinishBehavior.Cancel:
                    if(cmd != null)
                    {
                        cmd.Cancel();
                        cmd.Dispose();
                    }

                    if(reader != null)
                    {
                        if(!reader.IsClosed)
                            reader.Close();
                        reader.Dispose();
                    }

                    break;
                case FinishBehavior.CloseReader:
                    if(reader != null)
                    {
                        if(!reader.IsClosed)
                            reader.Close();
                        reader.Dispose();
                    }

                    if(cmd != null)
                        cmd.Dispose();
                    break;
                case FinishBehavior.DisposeCommand:
                    if(cmd != null)
                    {
                        cmd.Dispose();
                    }

                    break;
                default:
                    throw new ArgumentException($"QueryType {queryType} is not supported.");
            }

            if(con != null)
            {
                if(con.State != ConnectionState.Closed)
                    con.Close();
                con.Dispose();
            }
        }
    }
}