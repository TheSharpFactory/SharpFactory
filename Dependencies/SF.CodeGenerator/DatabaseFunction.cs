/*SFF*/
/************************************************
This class has been generated dynamically.
Any changes you make here will be overwritten.
************************************************/

#region Usings
using System.Data.SqlClient;
using System;
using System.Threading.Tasks;
#endregion

namespace SF.Repository.Common;

public static partial class Database
{
    internal static void Function(int modelId, int entityId, bool isScalar, SqlParameter[] param, Action<SqlDataReader> materializer)
    {
        FunctionQuery.Execute(modelId, entityId, isScalar, param, materializer);
    }
    internal static Task<bool> FunctionAsync(int modelId, int entityId, bool isScalar, SqlParameter[] param, Action<SqlDataReader> materializer)
    {
        return FunctionQuery.ExecuteAsync(modelId, entityId, isScalar, param, materializer);
    }

    private static class FunctionQuery
    {
        internal static void Execute(int modelId, int funcId, bool isScalar, SqlParameter[] param, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.Functions[funcId];
            var query = _select;

            if(isScalar)
                query += $"{entityInfo.DbFQName}({GenerateParams(param)}) AS ResultValue";
            else
                query += $"*{_from}{entityInfo.DbFQName}({GenerateParams(param)})";

            SqlExecute.Reader(modelId, entityInfo.DbFQName, query, param, materializer, FinishBehavior.Cancel);
        }
        internal static Task<bool> ExecuteAsync(int modelId, int funcId, bool isScalar, SqlParameter[] param, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.Functions[funcId];
            var query = _select;

            if(isScalar)
                query += $"{entityInfo.DbFQName}({GenerateParams(param)}) AS ResultValue";
            else
                query += $"*{_from}{entityInfo.DbFQName}({GenerateParams(param)})";

            return SqlExecute.ReaderAsync(modelId, entityInfo.DbFQName, query, param, materializer, FinishBehavior.Cancel);
        }

        private static string GenerateParams(SqlParameter[] param)
        {
            if(!(param?.Length > 0))
                return string.Empty;

            var count = param.Length;
            var values = param[0].ParameterName;//do the first one to avoid removing the trailing comma
            if(count > 1)
            {
                for(var i = 1; i < count; i++)
                    values += "," + param[i].ParameterName;
            }

            return values;
        }
    }
}