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
using System.Text;
using System.Threading.Tasks;
#endregion

namespace SF.Repository.Common;

public static partial class Database
{
    internal static void Update(List<EntityQuery> entityQueries, Action<SqlDataReader> materializer)
    {
        UpdateQuery.Execute(entityQueries, materializer);
    }
    internal static Task<bool> UpdateAsync(List<EntityQuery> entityQueries, Action<SqlDataReader> materializer)
    {
        return UpdateQuery.ExecuteAsync(entityQueries, materializer);
    }

    private static class UpdateQuery
    {
        internal static void Execute(List<EntityQuery> batchEntries, Action<SqlDataReader> materializer)
        {
            var queryCount = batchEntries.Count;
            var entityId = batchEntries[0].EntityId;
            var modelId = batchEntries[0].ModelId;
            var entityInfo = RepoLookup.Entities[entityId];
            var b = (StringBuilder)null;
            var finalParams = (List<SqlParameter>)null;

            for(var x = 0; x < queryCount; x++)
            {
                var entityQuery = batchEntries[x];
                var valToSetSuffix = $"_v{x}";
                var whereSuffix = $"_s{x}";
                var valuesToSet = entityQuery.Changes;
                var where = entityQuery.Where;
                var singleQuery = Compose(entityInfo, valuesToSet, where, valToSetSuffix, whereSuffix);

                if(b == null)
                    b = new StringBuilder((singleQuery.Length * queryCount) + 150);

                b.Append(singleQuery)
                  .Append(';')
                  .AppendLine();

                var paramsToSet = Parameters.Compose(entityInfo, valuesToSet, null, valToSetSuffix);
                var paramsForWhere = Parameters.Compose(entityInfo, where, null, whereSuffix);

                if(finalParams == null)
                    finalParams = new List<SqlParameter>((paramsForWhere.Length + paramsToSet.Length) * queryCount);

                for(var i = 0; i < paramsToSet.Length; i++)
                    finalParams.Add(paramsToSet[i]);

                for(var i = 0; i < paramsForWhere.Length; i++)
                    finalParams.Add(paramsForWhere[i]);
            }

            SqlExecute.Reader(modelId, entityInfo.DbFQName, b.ToString(), finalParams.ToArray(), materializer, FinishBehavior.Cancel);
        }
        internal static async Task<bool> ExecuteAsync(List<EntityQuery> batchEntries, Action<SqlDataReader> materializer)
        {
            var queryCount = batchEntries.Count;
            var entityId = batchEntries[0].EntityId;
            var modelId = batchEntries[0].ModelId;
            var entityInfo = RepoLookup.Entities[entityId];
            var b = (StringBuilder)null;
            var finalParams = (List<SqlParameter>)null;

            for(var x = 0; x < queryCount; x++)
            {
                var entityQuery = batchEntries[x];
                var valToSetSuffix = $"_v{x}";
                var whereSuffix = $"_s{x}";
                var valuesToSet = entityQuery.Changes;
                var where = entityQuery.Where;
                var singleQuery = Compose(entityInfo, valuesToSet, where, valToSetSuffix, whereSuffix);

                if(b == null)
                    b = new StringBuilder((singleQuery.Length * queryCount) + 150);

                b.Append(singleQuery)
                  .Append(';')
                  .AppendLine();

                var paramsToSet = Parameters.Compose(entityInfo, valuesToSet, null, valToSetSuffix);
                var paramsForWhere = Parameters.Compose(entityInfo, where, null, whereSuffix);

                if(finalParams == null)
                    finalParams = new List<SqlParameter>((paramsForWhere.Length + paramsToSet.Length) * queryCount);

                for(var i = 0; i < paramsToSet.Length; i++)
                    finalParams.Add(paramsToSet[i]);

                for(var i = 0; i < paramsForWhere.Length; i++)
                    finalParams.Add(paramsForWhere[i]);
            }

            return await SqlExecute.ReaderAsync(modelId, entityInfo.DbFQName, b.ToString(), finalParams.ToArray(), materializer, FinishBehavior.Cancel);
        }

        //internal static void Execute(int modelId, int entityId, List<IQueryFilter> valuesToSet, List<IQueryFilter> where, Action<SqlDataReader> materializer)
        //{
        //  var entityInfo = RepoLookup.Entities[entityId];
        //  var valToSetSuffix = "_v";
        //  var query = Compose(entityInfo, valuesToSet, where, valToSetSuffix);

        //  var paramsToSet = Parameters.Compose(entityInfo, valuesToSet, null, valToSetSuffix);
        //  var paramsForWhere = Parameters.Compose(entityInfo, where, null);
        //  var finalParams = new List<SqlParameter>(paramsForWhere.Length + paramsToSet.Length);

        //  for(var i = 0; i < paramsToSet.Length; i++)
        //    finalParams.Add(paramsToSet[i]);

        //  for(var i = 0; i < paramsForWhere.Length; i++)
        //    finalParams.Add(paramsForWhere[i]);

        //  SqlExecute.Reader(modelId, entityInfo.DbFQName, query, finalParams.ToArray(), materializer, FinishBehavior.Cancel);
        //}
        private static readonly SpanAction<char, StatementText> _writeUpdate = WriteUpdateInto;

        /// <summary>
        /// UPDATE table SET column = @param, ... WHERE ... ; SELECT the rows by the same WHERE
        /// </summary>
        private static string Compose(EntityInfo entityInfo, List<IQueryFilter> valuesToSet, List<IQueryFilter> where, string valToSetSuffix, string whereSuffix)
        { //valuesToSet serve the purpose of defining which fields will be assigned a value.
          //this updates any rows found according to the keys.

            if(!(valuesToSet?.Count > 0))
                throw new InvalidOperationException("No values to set have been found. Update requires at least one value to set.");

            if(!(where?.Count > 0))
                throw new InvalidOperationException("Update operation require at least one where clause.");

            var text = new StatementText(entityInfo, 0, where, null, null, whereSuffix, true, valuesToSet, valToSetSuffix);
            var measure = SqlText.Measuring();
            WriteUpdate(ref measure, text);
            return string.Create(measure.Length, text, _writeUpdate);
        }

        private static void WriteUpdateInto(Span<char> buffer, StatementText text)
        {
            var w = SqlText.Writing(buffer);
            WriteUpdate(ref w, text);
            w.CheckComplete();
        }

        private static void WriteUpdate(ref SqlText w, in StatementText text)
        {
            var entityInfo = text.EntityInfo;
            w.Append(_update);
            w.Append(entityInfo.DbFQName);
            w.Append(_set);

            var values = text.Values;
            var count = values.Count;
            for(var i = 0; i < count; i++)
            {
                if(i > 0)
                    w.Append(',');

                var valueToSet = values[i];
                var p = entityInfo.Props[valueToSet.PropertyId];
                w.Append(p.Column);
                w.Append(_equals);
                if(valueToSet.HasValue)
                {
                    w.Append(p.ParamName);
                    w.Append(text.ValuesSuffix);
                    w.AppendNumber(i);
                }
                else
                {
                    w.Append(_null);
                }
            }

            w.Append(_where);
            SelectQuery.WriteWhere(ref w, text.Where, entityInfo, true, text.ParamSuffix);
            w.Append("; ");
            SelectQuery.WriteSelect(ref w, text);
        }
    }
}
