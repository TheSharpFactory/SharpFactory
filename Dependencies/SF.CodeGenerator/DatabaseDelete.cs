/*SFF*/
/************************************************
This class has been generated dynamically.
Any changes you make here will be overwritten.
************************************************/

#region Usings
using SF.Query;
using System;
using System.Buffers;
using System.Collections.Generic;
using System.Threading.Tasks;
#endregion

namespace SF.Repository.Common;

public static partial class Database
{
    internal static int Delete(EntityQuery entityQuery)
    {
        return DeleteQuery.Execute(entityQuery.ModelId, entityQuery.EntityId, entityQuery.Where);
    }
    internal static Task<int> DeleteAsync(EntityQuery entityQuery)
    {
        return DeleteQuery.ExecuteAsync(entityQuery.ModelId, entityQuery.EntityId, entityQuery.Where);
    }

    private static class DeleteQuery
    {
        internal static int Execute(int modelId, int entityId, List<IQueryFilter> where)
        {
            var entityInfo = RepoLookup.Entities[entityId];

            var query = Compose(entityInfo, where);

            var paramsForWhere = Parameters.Compose(entityInfo, where, null);

            return SqlExecute.NonQuery(modelId, entityInfo.DbFQName, query, paramsForWhere, FinishBehavior.DisposeCommand);
        }
        internal static Task<int> ExecuteAsync(int modelId, int entityId, List<IQueryFilter> where)
        {
            var entityInfo = RepoLookup.Entities[entityId];

            var query = Compose(entityInfo, where);

            var paramsForWhere = Parameters.Compose(entityInfo, where, null);

            return SqlExecute.NonQueryAsync(modelId, entityInfo.DbFQName, query, paramsForWhere, FinishBehavior.DisposeCommand);
        }

        private static readonly SpanAction<char, StatementText> _writeDelete = WriteDeleteInto;

        /// <summary>
        /// DELETE table WHERE ...
        /// </summary>
        private static string Compose(EntityInfo entityInfo, List<IQueryFilter> where)
        {//delete always returns the number of aftected rows.
            if(!(where?.Count > 0))
                throw new InvalidOperationException("Delete operation requires at least one where clause.");

            var text = new StatementText(entityInfo, 0, where, null, null, null, true, null, null);
            var measure = SqlText.Measuring();
            WriteDelete(ref measure, text);
            return string.Create(measure.Length, text, _writeDelete);
        }

        private static void WriteDeleteInto(Span<char> buffer, StatementText text)
        {
            var w = SqlText.Writing(buffer);
            WriteDelete(ref w, text);
            w.CheckComplete();
        }

        private static void WriteDelete(ref SqlText w, in StatementText text)
        {
            w.Append(_delete);
            w.Append(text.EntityInfo.DbFQName);
            w.Append(_where);
            SelectQuery.WriteWhere(ref w, text.Where, text.EntityInfo, true, text.ParamSuffix);
        }
    }
}
