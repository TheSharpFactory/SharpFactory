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
using System.Threading.Tasks;
#endregion

namespace SF.Repository.Common;

public static partial class Database
{
    internal static Dictionary<int, object> StoredProc(EntityQuery entityQuery, Action<SqlDataReader> materializer)
    {
        return StoredProcQuery.Execute(entityQuery.ModelId, entityQuery.EntityId, entityQuery.Where, materializer);
    }
    internal static Task<Dictionary<int, object>> StoredProcAsync(EntityQuery entityQuery, Action<SqlDataReader> materializer)
    {
        return StoredProcQuery.ExecuteAsync(entityQuery.ModelId, entityQuery.EntityId, entityQuery.Where, materializer);
    }

    private class StoredProcParams
    {
        public Dictionary<int, SqlParameter> OutParams { get; set; }
        public SqlParameter[] Parameters { get; set; }
    }

    private static class StoredProcQuery
    {
        internal static Dictionary<int, object> Execute(int modelId, int procId, List<IQueryFilter> param, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.StoredProcs[procId];
            var paramResult = Parameters.ComposeForStoredProc(entityInfo, param);
            if(materializer == null)
                SqlExecute.NonQuery(modelId, entityInfo.DbFQName, entityInfo.DbFQName, paramResult.Parameters, FinishBehavior.CloseReader, CommandType.StoredProcedure);
            else
                SqlExecute.Reader(modelId, entityInfo.DbFQName, entityInfo.DbFQName, paramResult.Parameters, materializer, FinishBehavior.CloseReader, CommandType.StoredProcedure);

            //populate out param result
            var result = new Dictionary<int, object>(paramResult.OutParams.Count);
            if(param?.Count > 0 && paramResult.OutParams.Count > 0)
            {
                foreach(var p in param)
                {
                    if(paramResult.OutParams.ContainsKey(p.PropertyId))
                        result.Add(p.PropertyId, paramResult.OutParams[p.PropertyId].Value);
                }
            }

            result.Add(ReturnParamId, paramResult.Parameters[paramResult.Parameters.Length - 1].Value);
            return result;
        }
        internal static int Execute(int modelId, int procId, SqlParameter[] param, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.StoredProcs[procId];
            if(materializer == null)
                SqlExecute.NonQuery(modelId, entityInfo.DbFQName, entityInfo.DbFQName, param, FinishBehavior.CloseReader, CommandType.StoredProcedure);
            else
                SqlExecute.Reader(modelId, entityInfo.DbFQName, entityInfo.DbFQName, param, materializer, FinishBehavior.CloseReader, CommandType.StoredProcedure);

            if(param?.Length > 0)
                return (int)param[param.Length - 1].Value;//return the last parameter which is the return value.

            return 0;
        }

        internal static async Task<Dictionary<int, object>> ExecuteAsync(int modelId, int procId, List<IQueryFilter> param, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.StoredProcs[procId];
            var paramResult = Parameters.ComposeForStoredProc(entityInfo, param);
            if(materializer == null)
                await SqlExecute.NonQueryAsync(modelId, entityInfo.DbFQName, entityInfo.DbFQName, paramResult.Parameters, FinishBehavior.CloseReader, CommandType.StoredProcedure);
            else
                await SqlExecute.ReaderAsync(modelId, entityInfo.DbFQName, entityInfo.DbFQName, paramResult.Parameters, materializer, FinishBehavior.CloseReader, CommandType.StoredProcedure);

            //populate out param result
            var result = new Dictionary<int, object>(paramResult.OutParams.Count);
            if(param?.Count > 0 && paramResult.OutParams.Count > 0)
            {
                foreach(var p in param)
                {
                    if(paramResult.OutParams.ContainsKey(p.PropertyId))
                        result.Add(p.PropertyId, paramResult.OutParams[p.PropertyId].Value);
                }
            }

            result.Add(ReturnParamId, paramResult.Parameters[paramResult.Parameters.Length - 1].Value);
            return result;
        }
        internal static async Task<int> ExecuteAsync(int modelId, int procId, SqlParameter[] param, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.StoredProcs[procId];
            if(materializer == null)
                await SqlExecute.NonQueryAsync(modelId, entityInfo.DbFQName, entityInfo.DbFQName, param, FinishBehavior.CloseReader, CommandType.StoredProcedure);
            else
                await SqlExecute.ReaderAsync(modelId, entityInfo.DbFQName, entityInfo.DbFQName, param, materializer, FinishBehavior.CloseReader, CommandType.StoredProcedure);

            if(param?.Length > 0)
                return (int)param[param.Length - 1].Value;//return the last parameter which is the return value.

            return 0;
        }
    }
}
