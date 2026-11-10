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
using System.Threading.Tasks;
#endregion

namespace SF.Repository.Common;

public static partial class Database
{
    internal static void Insert(EntityQuery entityQuery, Action<SqlDataReader> materializer)
    {
        if(entityQuery.Where != null && entityQuery.Where.Count > 0)
            InsertQuery.Execute(entityQuery.ModelId, entityQuery.EntityId, entityQuery.Changes, entityQuery.Where, materializer);
        else
            InsertQuery.Execute(entityQuery.ModelId, entityQuery.EntityId, entityQuery.Changes, materializer);
    }
    internal static async Task<bool> InsertAsync(EntityQuery entityQuery, Action<SqlDataReader> materializer)
    {
        if(entityQuery.Where != null && entityQuery.Where.Count > 0)
            return await InsertQuery.ExecuteAsync(entityQuery.ModelId, entityQuery.EntityId, entityQuery.Changes, entityQuery.Where, materializer);
        else
            return await InsertQuery.ExecuteAsync(entityQuery.ModelId, entityQuery.EntityId, entityQuery.Changes, materializer);
    }

    private static class InsertQuery
    {
        internal static void Execute(int modelId, int entityId, List<IQueryFilter> valuesToSet, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.Entities[entityId];
            var query = Compose(entityInfo, valuesToSet);
            var valToSetParam = Parameters.ComposeForInsert(entityInfo, valuesToSet);
            var paramNames = new HashSet<string>();
            var paramList = new List<SqlParameter>(valToSetParam.Length);

            for(var i = 0; i < valToSetParam.Length; i++)
            {
                if(paramNames.Contains(valToSetParam[i].ParameterName))
                    continue;

                paramNames.Add(valToSetParam[i].ParameterName);
                paramList.Add(valToSetParam[i]);
            }

            SqlExecute.Reader(modelId, entityInfo.DbFQName, query, paramList.ToArray(), materializer, FinishBehavior.Cancel);
        }
        internal static void Execute(int modelId, int entityId, List<IQueryFilter> valuesToSet, List<IQueryFilter> keys, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.Entities[entityId];
            var query = Compose(entityInfo, valuesToSet, keys);
            var valToSetParam = Parameters.ComposeForInsert(entityInfo, valuesToSet);
            var keysToSelectParam = Parameters.Compose(entityInfo, keys, null, "_I");

            var paramList = new List<SqlParameter>(valToSetParam.Length);

            for(var i = 0; i < valToSetParam.Length; i++)
            {
                paramList.Add(valToSetParam[i]);
            }

            for(var i = 0; i < keysToSelectParam.Length; i++)
            {
                paramList.Add(keysToSelectParam[i]);
            }

            SqlExecute.Reader(modelId, entityInfo.DbFQName, query, paramList.ToArray(), materializer, FinishBehavior.Cancel);

        }

        internal static async Task<bool> ExecuteAsync(int modelId, int entityId, List<IQueryFilter> valuesToSet, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.Entities[entityId];
            var query = Compose(entityInfo, valuesToSet);
            var valToSetParam = Parameters.ComposeForInsert(entityInfo, valuesToSet);
            var paramNames = new HashSet<string>();
            var paramList = new List<SqlParameter>(valToSetParam.Length);

            for(var i = 0; i < valToSetParam.Length; i++)
            {
                if(paramNames.Contains(valToSetParam[i].ParameterName))
                    continue;

                paramNames.Add(valToSetParam[i].ParameterName);
                paramList.Add(valToSetParam[i]);
            }

            return await SqlExecute.ReaderAsync(modelId, entityInfo.DbFQName, query, paramList.ToArray(), materializer, FinishBehavior.Cancel);
        }
        internal static async Task<bool> ExecuteAsync(int modelId, int entityId, List<IQueryFilter> valuesToSet, List<IQueryFilter> keys, Action<SqlDataReader> materializer)
        {
            var entityInfo = RepoLookup.Entities[entityId];
            var query = Compose(entityInfo, valuesToSet, keys);
            var valToSetParam = Parameters.ComposeForInsert(entityInfo, valuesToSet);
            var keysToSelectParam = Parameters.Compose(entityInfo, keys, null, "_I");

            var paramList = new List<SqlParameter>(valToSetParam.Length);

            for(var i = 0; i < valToSetParam.Length; i++)
            {
                paramList.Add(valToSetParam[i]);
            }

            for(var i = 0; i < keysToSelectParam.Length; i++)
            {
                paramList.Add(keysToSelectParam[i]);
            }

            return await SqlExecute.ReaderAsync(modelId, entityInfo.DbFQName, query, paramList.ToArray(), materializer, FinishBehavior.Cancel);
        }

        private static readonly SpanAction<char, StatementText> _writeInsertOutput = WriteInsertOutputInto;
        private static readonly SpanAction<char, StatementText> _writeInsertThenSelect = WriteInsertThenSelectInto;

        /// <summary>
        /// INSERT INTO table(columns) OUTPUT inserted columns VALUES(params)
        /// </summary>
        private static string Compose(EntityInfo entityInfo, List<IQueryFilter> valuesToSet)
        {//query filters serve the purpose of defining which fields will be assigned a value.
            var text = new StatementText(entityInfo, 0, null, null, null, null, false, valuesToSet, null);
            var measure = SqlText.Measuring();
            WriteInsertOutput(ref measure, text);
            return string.Create(measure.Length, text, _writeInsertOutput);
        }

        /// <summary>
        /// INSERT INTO table(columns) VALUES(params) ; SELECT the row by its keys
        /// </summary>
        private static string Compose(EntityInfo entityInfo, List<IQueryFilter> valuesToSet, List<IQueryFilter> keys)
        {
            var text = new StatementText(entityInfo, 0, keys, null, null, "_I", false, valuesToSet, null);
            var measure = SqlText.Measuring();
            WriteInsertThenSelect(ref measure, text);
            return string.Create(measure.Length, text, _writeInsertThenSelect);
        }

        private static void WriteInsertOutputInto(Span<char> buffer, StatementText text)
        {
            var w = SqlText.Writing(buffer);
            WriteInsertOutput(ref w, text);
            w.CheckComplete();
        }

        private static void WriteInsertThenSelectInto(Span<char> buffer, StatementText text)
        {
            var w = SqlText.Writing(buffer);
            WriteInsertThenSelect(ref w, text);
            w.CheckComplete();
        }

        private static void WriteInsertOutput(ref SqlText w, in StatementText text)
        {
            var entityInfo = text.EntityInfo;
            w.Append(_insert);
            w.Append(entityInfo.DbFQName);
            if(string.IsNullOrWhiteSpace(entityInfo.InsertProjection))
            {
                w.Append(_output);
                w.Append(entityInfo.InsertedProjection);
                return;
            }

            w.Append('(');
            w.Append(entityInfo.InsertProjection);
            w.Append(')');
            w.Append(_output);
            w.Append(entityInfo.InsertedProjection);
            WriteValues(ref w, text.Values, entityInfo);
        }

        private static void WriteInsertThenSelect(ref SqlText w, in StatementText text)
        {
            var entityInfo = text.EntityInfo;
            w.Append(_insert);
            w.Append(entityInfo.DbFQName);
            if(!string.IsNullOrWhiteSpace(entityInfo.InsertProjection))
            {
                w.Append('(');
                w.Append(entityInfo.InsertProjection);
                w.Append(')');
                WriteValues(ref w, text.Values, entityInfo);
            }

            w.Append("; ");
            SelectQuery.WriteSelect(ref w, text);
        }

        /// <summary>
        /// " VALUES(@a,@b) " when there are values to set; nothing otherwise.
        /// </summary>
        private static void WriteValues(ref SqlText w, List<IQueryFilter> valuesToSet, EntityInfo entityInfo)
        {
            if(!(valuesToSet?.Count > 0))
                return;

            w.Append(" VALUES(");
            var count = valuesToSet.Count;
            for(var i = 0; i < count; i++)
            {
                if(i > 0)
                    w.Append(',');

                w.Append(entityInfo.Props[valuesToSet[i].PropertyId].ParamName);
            }

            w.Append(") ");
        }
    }
}