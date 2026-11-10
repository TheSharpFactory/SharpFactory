#region Usings
using System;
using System.Collections.Generic;
using System.Data;
using System.Diagnostics;
using System.Text;
using System.Threading;
using Microsoft.Data.SqlClient;
using SF.Common.Logging;
using SF.Models;
#endregion

namespace SF.Db;

public static class SqlConnector
{
    #region Connection probe
    private sealed class ConnectionProbe
    {
        private readonly SqlConnection _connection;
        private readonly SqlCommand _command;
        // We'll use a Stopwatch here for simplicity. A comparison to a stored DateTime.Now value could also be used
        private readonly Stopwatch _stopwatch = new Stopwatch();

        public bool ConnectSuccess { get; set; }

        /// <summary>
        /// Why the connection failed (the SQL Server message); null when it worked or is still trying.
        /// </summary>
        public string Error { get; set; }

        public ConnectionProbe(SqlConnection connection, SqlCommand command)
        {
            _connection = connection;
            _command = command;
        }

        public void Run()
        {
            try
            {
                _stopwatch.Start();
                _connection.Open();
                var date = _command.ExecuteScalar();
                ConnectSuccess = true;
            }
            catch(Exception ex)
            {
                //a failed connection is an answer, not a crash: keep the reason for the caller and the log
                Error = ex.Message;
                LogManager.WriteErrorLog(ex, "Unable to connect to the database", $"Server: {_connection.DataSource}, database: {_connection.Database}");
            }
            finally
            {
                //both always exist: the probe is created with them
                _command.Cancel();
                _command.Dispose();

                if(_connection.State != ConnectionState.Closed)
                    _connection.Close();
                _connection.Dispose();
            }
        }
    }
    #endregion

    //INFORMATION_SCHEMA VIEWS  https://msdn.microsoft.com/en-us/library/ms186778.aspx
    //Schema Restrictions: https://msdn.microsoft.com/en-us/library/cc716722(v=vs.110).aspx
    //Schema Collections: https://msdn.microsoft.com/en-us/library/ms254969.aspx
    //SQL Server limits. Vary by version https://msdn.microsoft.com/en-us/library/ms143432(v=sql.120).aspx

    public static bool ValidateConnectionInfo(ConnectionInfo conInfo)
    {
        return ValidateConnectionInfo(conInfo, out _);
    }

    /// <summary>
    /// True when the server answers within two seconds. Otherwise <paramref name="error"/> says why (SQL Server's
    /// message, or that it did not answer in time).
    /// </summary>
    public static bool ValidateConnectionInfo(ConnectionInfo conInfo, out string error)
    {
        if(string.IsNullOrWhiteSpace(conInfo.Database))
            conInfo.Database = "master";
        var con = new SqlConnection(GenerateConnectionString(conInfo));
        var cmd = new SqlCommand("SELECT GETDATE();", con);

        // Try to open the connection, if anything goes wrong, make sure we set connectSuccess = false
        var probe = new ConnectionProbe(con, cmd);
        var t = new Thread(probe.Run)
        {
            // Make sure it's marked as a background thread so it'll get cleaned up automatically
            IsBackground = true
        };
        t.Start();
        t.Join(2000);

        error = null;
        if(!probe.ConnectSuccess)
            error = probe.Error ?? $"{conInfo.Server} did not answer within 2 seconds.";

        return probe.ConnectSuccess;
    }

    public static DataTable GetDatabases(ConnectionInfo conInfo)
    {
        if(string.IsNullOrWhiteSpace(conInfo.Database))
            conInfo.Database = "master";

        var sqlSelect = $"SELECT NAME FROM MASTER.SYS.DATABASES WHERE NAME NOT IN('master','tempdb','model','msdb','ReportServer','ReportServerTempDB') ORDER BY NAME";
        return GetDataTable(conInfo, sqlSelect);
    }
    public static DataTable GetSequences(ConnectionInfo conInfo)
    {
        var sqlSelect = "SELECT SEQUENCE_SCHEMA,SEQUENCE_NAME,DATA_TYPE FROM INFORMATION_SCHEMA.SEQUENCES WHERE SEQUENCE_CATALOG = @catalog";
        return GetDataTable(conInfo, sqlSelect, CatalogParameters(conInfo));
    }
    public static DataTable GetTables(ConnectionInfo conInfo)
    {
        var sqlSelect = $"SELECT TABLE_SCHEMA,TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_CATALOG = @catalog AND TABLE_TYPE = '{SqlTable.TypeTable}'";
        return GetDataTable(conInfo, sqlSelect, CatalogParameters(conInfo));
    }
    public static DataTable GetViews(ConnectionInfo conInfo)
    {
        var sqlSelect = $"SELECT TABLE_SCHEMA,TABLE_NAME FROM INFORMATION_SCHEMA.TABLES WHERE TABLE_CATALOG = @catalog AND TABLE_TYPE = '{SqlTable.TypeView}'";
        return GetDataTable(conInfo, sqlSelect, CatalogParameters(conInfo));
    }
    public static DataTable GetProcs(ConnectionInfo conInfo)
    {
        var sqlSelect = $"SELECT SPECIFIC_SCHEMA,SPECIFIC_NAME FROM INFORMATION_SCHEMA.ROUTINES WHERE SPECIFIC_CATALOG = @catalog AND ROUTINE_TYPE = '{SqlRoutine.TypeStoredProc}'";
        return GetDataTable(conInfo, sqlSelect, CatalogParameters(conInfo));
    }
    public static DataTable GetFuncs(ConnectionInfo conInfo)
    {
        var sqlSelect = $"SELECT SPECIFIC_SCHEMA,SPECIFIC_NAME,DATA_TYPE,CHARACTER_MAXIMUM_LENGTH,CHARACTER_OCTET_LENGTH,NUMERIC_PRECISION,NUMERIC_PRECISION_RADIX,NUMERIC_SCALE,DATETIME_PRECISION FROM INFORMATION_SCHEMA.ROUTINES WHERE SPECIFIC_CATALOG = @catalog AND ROUTINE_TYPE = '{SqlRoutine.TypeFunction}'";
        return GetDataTable(conInfo, sqlSelect, CatalogParameters(conInfo));
    }
    public static DataTable GetColumns(ConnectionInfo conInfo, string schema, string[] tables)
    {
        //COLUMNS https://msdn.microsoft.com/en-us/library/ms188348.aspx
        var sqlSelect = "SELECT * FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG = @catalog AND TABLE_SCHEMA = @schema AND TABLE_NAME IN({names}) ORDER BY TABLE_CATALOG, TABLE_SCHEMA, TABLE_NAME, ORDINAL_POSITION";
        return GetDataTableForNames(conInfo, sqlSelect, schema, tables);
    }
    public static DataTable GetColumnDefaultValue(ConnectionInfo conInfo, string schema, string table, string column)
    {
        //the object name is passed exactly as it was written into the query before: schema.table, unquoted
        var sqlSelect = "SELECT OBJECT_DEFINITION(default_object_id) AS COLUMN_DEFAULT FROM SYS.COLUMNS WHERE NAME = @column AND object_id = object_id(@object)";
        var parameters = new List<SqlParameter>(2)
        {
            Text("@column", column),
            Text("@object", $"{schema}.{table}"),
        };

        return GetDataTable(conInfo, sqlSelect, parameters);
    }
    /// <summary>
    /// Every primary key and unique constraint of the database, with the schema first: one query for all schemas
    /// (the catalog views take most of a second to compile, once per query). StoreUtils takes each schema's rows.
    /// The rows are the ones INFORMATION_SCHEMA.TABLE_CONSTRAINTS and KEY_COLUMN_USAGE gave, in the same order.
    /// </summary>
    public static DataTable GetAllKeys(ConnectionInfo conInfo)
    {
        var sqlSelect = "SELECT S.name AS TABLE_SCHEMA, T.name AS TABLE_NAME, KC.name AS CONSTRAINT_NAME, CAST(IC.key_ordinal AS int) AS ORDINAL_POSITION, C.name AS COLUMN_NAME, CAST(CASE KC.type WHEN 'PK' THEN 'PRIMARY KEY' ELSE 'UNIQUE' END AS varchar(11)) AS CONSTRAINT_TYPE FROM sys.key_constraints AS KC JOIN sys.tables AS T ON T.object_id = KC.parent_object_id JOIN sys.schemas AS S ON S.schema_id = T.schema_id JOIN sys.index_columns AS IC ON IC.object_id = KC.parent_object_id AND IC.index_id = KC.unique_index_id AND IC.key_ordinal > 0 JOIN sys.columns AS C ON C.object_id = IC.object_id AND C.column_id = IC.column_id WHERE DB_NAME() = @catalog ORDER BY TABLE_SCHEMA, TABLE_NAME, CONSTRAINT_NAME, ORDINAL_POSITION";
        return GetDataTable(conInfo, sqlSelect, CatalogParameters(conInfo));
    }

    /// <summary>
    /// Every foreign key of the database, with its schema first; each column paired with the referenced key's column
    /// in the same position, as INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS and KEY_COLUMN_USAGE paired them (a foreign
    /// key to a unique index that is not a constraint is left out, as it was there).
    /// </summary>
    public static DataTable GetAllForeignKeys(ConnectionInfo conInfo)
    {
        var sqlSelect = "SELECT CS.name AS 'FK_CONSTRAINT_SCHEMA', FK.name AS 'FK_CONSTRAINT_NAME', CT.name AS 'FK_TABLE_NAME', CC.name AS 'FK_COLUMN_NAME', CAST(FKC.constraint_column_id AS int) AS 'FK_ORDINAL_POSITION', UI.name AS 'UQ_CONSTRAINT_NAME', PS.name AS 'UQ_CONSTRAINT_SCHEMA', PT.name AS 'UQ_TABLE_NAME', UC.name AS 'UQ_COLUMN_NAME' FROM sys.foreign_keys AS FK JOIN sys.schemas AS CS ON CS.schema_id = FK.schema_id JOIN sys.objects AS CT ON CT.object_id = FK.parent_object_id JOIN sys.foreign_key_columns AS FKC ON FKC.constraint_object_id = FK.object_id JOIN sys.columns AS CC ON CC.object_id = FKC.parent_object_id AND CC.column_id = FKC.parent_column_id JOIN sys.indexes AS UI ON UI.object_id = FK.referenced_object_id AND UI.index_id = FK.key_index_id AND (UI.is_primary_key = 1 OR UI.is_unique_constraint = 1) JOIN sys.objects AS PT ON PT.object_id = FK.referenced_object_id JOIN sys.schemas AS PS ON PS.schema_id = PT.schema_id JOIN sys.index_columns AS UIC ON UIC.object_id = UI.object_id AND UIC.index_id = UI.index_id AND UIC.key_ordinal = FKC.constraint_column_id JOIN sys.columns AS UC ON UC.object_id = UIC.object_id AND UC.column_id = UIC.column_id WHERE DB_NAME() = @catalog ORDER BY FK_CONSTRAINT_SCHEMA, FK_CONSTRAINT_NAME, FK_ORDINAL_POSITION";
        return GetDataTable(conInfo, sqlSelect, CatalogParameters(conInfo));
    }
    public static DataTable GetIdentityComputedGeneratedColumns(ConnectionInfo conInfo, string schema, string[] tables)
    {
        //COLUMNS https://msdn.microsoft.com/en-us/library/ms188348.aspx
        //COLUMNPROPERTY https://msdn.microsoft.com/en-us/library/ms174968.aspx
        var sqlSelect = "SELECT TABLE_NAME,COLUMN_NAME,COLUMNPROPERTY(object_id(TABLE_CATALOG+'.'+TABLE_SCHEMA+'.'+TABLE_NAME), COLUMN_NAME, 'IsIdentity') AS 'IS_IDENTITY',COLUMNPROPERTY(object_id(TABLE_CATALOG+'.'+TABLE_SCHEMA+'.'+TABLE_NAME), COLUMN_NAME, 'IsComputed') AS 'IS_COMPUTED',COLUMNPROPERTY(object_id(TABLE_CATALOG+'.'+TABLE_SCHEMA+'.'+TABLE_NAME), COLUMN_NAME, 'GeneratedAlwaysType') AS 'IS_GENERATED' FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_CATALOG = @catalog AND TABLE_SCHEMA = @schema AND TABLE_NAME IN({names}) AND (COLUMNPROPERTY(object_id(TABLE_CATALOG+'.'+TABLE_SCHEMA+'.'+TABLE_NAME), COLUMN_NAME, 'IsIdentity') = 1 OR COLUMNPROPERTY(object_id(TABLE_CATALOG+'.'+TABLE_SCHEMA+'.'+TABLE_NAME), COLUMN_NAME, 'IsComputed') = 1 OR COLUMNPROPERTY(object_id(TABLE_CATALOG+'.'+TABLE_SCHEMA+'.'+TABLE_NAME), COLUMN_NAME, 'GeneratedAlwaysType') > 0) ORDER BY TABLE_NAME";
        return GetDataTableForNames(conInfo, sqlSelect, schema, tables);
    }
    public static DataTable GetRoutineParams(ConnectionInfo conInfo, string schema, string[] routines)
    {
        var sqlSelect = "SELECT SPECIFIC_NAME,PARAMETER_NAME,ORDINAL_POSITION,PARAMETER_MODE,IS_RESULT,AS_LOCATOR,DATA_TYPE,CHARACTER_MAXIMUM_LENGTH,CHARACTER_OCTET_LENGTH,NUMERIC_PRECISION,NUMERIC_PRECISION_RADIX,NUMERIC_SCALE,DATETIME_PRECISION FROM INFORMATION_SCHEMA.PARAMETERS WHERE SPECIFIC_CATALOG = @catalog AND SPECIFIC_SCHEMA = @schema AND IS_RESULT = 'NO' AND SPECIFIC_NAME IN({names})";
        return GetDataTableForNames(conInfo, sqlSelect, schema, routines);
    }
    public static DataTable GetTriggers(ConnectionInfo conInfo)
    {
        var sqlSelect = $"SELECT SC.NAME as TABLE_SCHEMA,SO.NAME AS TABLE_NAME,ST.NAME AS TRIGGER_NAME FROM SYS.TRIGGERS ST JOIN SYS.OBJECTS SO ON ST.parent_id = SO.object_id JOIN sys.schemas SC ON SO.schema_id = SC.schema_id";
        return GetDataTable(conInfo, sqlSelect);
    }

    #region Query parameters
    //SQL Server takes at most 2,100 parameters in one query: longer name lists run in batches
    private const int _maxNamesPerQuery = 2000;

    /// <summary>
    /// A text parameter: names travel as values, never as part of the SQL (a name with an apostrophe, such as
    /// O'Brien, or with characters outside the server's code page, works).
    /// </summary>
    private static SqlParameter Text(string name, string value)
    {
        return new SqlParameter(name, SqlDbType.NVarChar, 4000) { Value = value == null ? DBNull.Value : value };
    }

    private static List<SqlParameter> CatalogParameters(ConnectionInfo conInfo)
    {
        return new List<SqlParameter>(1) { Text("@catalog", conInfo.Database) };
    }

    /// <summary>
    /// Runs a query whose "{names}" placeholder is a list of names (tables or routines) of one schema, each name a
    /// parameter. Lists longer than SQL Server's parameter limit run in batches, merged in order.
    /// </summary>
    private static DataTable GetDataTableForNames(ConnectionInfo conInfo, string sqlWithNames, string schema, string[] names)
    {
        if(names == null || names.Length == 0)
            return GetDataTable(conInfo, sqlWithNames.Replace("{names}", "NULL"), SchemaParameters(conInfo, schema));

        var result = (DataTable)null;
        for(var start = 0; start < names.Length; start += _maxNamesPerQuery)
        {
            var count = Math.Min(_maxNamesPerQuery, names.Length - start);
            var parameters = SchemaParameters(conInfo, schema);
            var list = new StringBuilder(count * 6);
            for(var i = 0; i < count; i++)
            {
                if(i > 0)
                    list.Append(',');

                var parameterName = "@n" + i;
                list.Append(parameterName);
                parameters.Add(Text(parameterName, names[start + i]));
            }

            var batch = GetDataTable(conInfo, sqlWithNames.Replace("{names}", list.ToString()), parameters);
            if(result == null)
                result = batch;
            else
                result.Merge(batch);
        }

        return result;
    }

    private static List<SqlParameter> SchemaParameters(ConnectionInfo conInfo, string schema)
    {
        return new List<SqlParameter> { Text("@catalog", conInfo.Database), Text("@schema", schema) };
    }

    /// <summary>
    /// An identifier in square brackets, for the one place a routine is named in SQL text (EXECUTE, SELECT FROM):
    /// a ']' in the name is doubled, as SQL Server's QUOTENAME does.
    /// </summary>
    private static string QuoteName(string name)
    {
        return "[" + name.Replace("]", "]]") + "]";
    }
    #endregion

    private static DataTable GetDataTable(ConnectionInfo conInfo, string sqlSelect)
    {
        return GetDataTable(conInfo, sqlSelect, null);
    }

    private static DataTable GetDataTable(ConnectionInfo conInfo, string sqlSelect, List<SqlParameter> parameters)
    {
        var con = new SqlConnection(GenerateConnectionString(conInfo));
        var cmd = new SqlCommand(sqlSelect, con);
        if(parameters != null)
        {
            foreach(var parameter in parameters)
                cmd.Parameters.Add(parameter);
        }

        var reader = (SqlDataReader)null;
        try
        {
            con.Open();
            var result = new DataTable();
            reader = cmd.ExecuteReader(CommandBehavior.SequentialAccess);
            result.Load(reader);
            return result;
        }
        finally
        {
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

            if(con != null)
            {
                if(con.State != ConnectionState.Closed)
                    con.Close();
                con.Dispose();
            }
        }
    }
    public static DataTable GetRoutineReturnTypeColumns(ConnectionInfo conInfo, string schema, string routineName, int paramCount, bool isTableFunction)
    {
        var paramCsv = string.Empty;
        if(paramCount > 0)
        {
            paramCsv = " NULL";
            var extraCount = paramCount - 1;
            if(extraCount > 0)
                paramCsv += (new StringBuilder(5 * extraCount).Insert(0, ",NULL", extraCount).ToString());
        }

        var sqlQuery = string.Empty;
        if(isTableFunction)
            sqlQuery = $"SET FMTONLY OFF SET FMTONLY ON SELECT * FROM {QuoteName(schema)}.{QuoteName(routineName)}({paramCsv}) SET FMTONLY OFF";
        else
            sqlQuery = $"SET FMTONLY OFF SET FMTONLY ON EXECUTE {QuoteName(schema)}.{QuoteName(routineName)}{paramCsv} SET FMTONLY OFF";

        var con = new SqlConnection(GenerateConnectionString(conInfo));
        var cmd = new SqlCommand(sqlQuery, con);
        var reader = (SqlDataReader)null;
        var result = new DataTable();
        try
        {
            con.Open();
            reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);
            result = reader.GetSchemaTable();
            return result;
        }
        catch(Exception ex)
        {
            //SQL Server cannot describe some routines without running them (temporary tables, dynamic SQL):
            //record it, because the fallback below runs the routine with NULL parameters (BACKLOG.md, SF-023)
            LogManager.WriteLog($"SQL Server could not describe the result of [{schema}].[{routineName}] without running it ({ex.Message}). It is run with NULL parameters to find its columns.", LogEntryType.Warning);
        }
        finally
        {
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

            if(con != null)
            {
                if(con.State != ConnectionState.Closed)
                    con.Close();
                con.Dispose();
            }
        }
        //if it gets here it means it never returned. so we need to try again
        if(isTableFunction)
            sqlQuery = $"SELECT * FROM {QuoteName(schema)}.{QuoteName(routineName)}({paramCsv})";
        else
            sqlQuery = $"EXECUTE {QuoteName(schema)}.{QuoteName(routineName)}{paramCsv}";
        con = new SqlConnection(GenerateConnectionString(conInfo));
        cmd = new SqlCommand(sqlQuery, con);
        reader = null;
        result = new DataTable();
        try
        {
            con.Open();
            reader = cmd.ExecuteReader(CommandBehavior.KeyInfo);
            result = reader.GetSchemaTable();
            return result;
        }
        finally
        {
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

            if(con != null)
            {
                if(con.State != ConnectionState.Closed)
                    con.Close();
                con.Dispose();
            }
        }
    }

    private static string GenerateConnectionString(ConnectionInfo conInfo)
    {
        var connStringBuilder = new SqlConnectionStringBuilder
        {
            ApplicationName = "The Sharp Factory",
            Authentication = SqlAuthenticationMethod.NotSpecified,
            DataSource = conInfo.Server,
            InitialCatalog = conInfo.Database,
            IntegratedSecurity = !conInfo.DisableIntegratedSecurity.Value,
            //do not require encryption (SqlClient 4.0+ requires it by default), so servers without a certificate work
            Encrypt = SqlConnectionEncryptOption.Optional
        };

        if(conInfo.DisableIntegratedSecurity.Value)
        {
            connStringBuilder.UserID = conInfo.User;
            connStringBuilder.Password = conInfo.Password;
        }

        return connStringBuilder.ToString();
    }
}
