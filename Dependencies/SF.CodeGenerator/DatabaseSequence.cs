/*SFF*/
/************************************************
This class has been generated dynamically.
Any changes you make here will be overwritten.
************************************************/

#region Usings
using System.Data;
using System.Threading.Tasks;
#endregion

namespace SF.Repository.Common;

public static partial class Database
{
    internal static TSequenceResult Sequence<TSequenceResult>(EntityQuery entityQuery)
    {
        return SequenceQuery.Execute<TSequenceResult>(entityQuery.ModelId, entityQuery.EntityId);
    }
    internal static async Task<TSequenceResult> SequenceAsync<TSequenceResult>(EntityQuery entityQuery)
    {
        return await SequenceQuery.ExecuteAsync<TSequenceResult>(entityQuery.ModelId, entityQuery.EntityId);
    }

    private static class SequenceQuery
    {
        internal static TScalarResult Execute<TScalarResult>(int modelId, int seqId)
        {
            var entityInfo = RepoLookup.Sequences[seqId];

            var result = SqlExecute.Scalar<TScalarResult>(modelId, entityInfo.DbFQName, $"{_select}{_nextValueFor}{entityInfo.DbFQName}", null, FinishBehavior.DisposeCommand, CommandType.Text);

            return result;
        }
        internal static async Task<TScalarResult> ExecuteAsync<TScalarResult>(int modelId, int seqId)
        {
            var entityInfo = RepoLookup.Sequences[seqId];

            var result = await SqlExecute.ScalarAsync<TScalarResult>(modelId, entityInfo.DbFQName, $"{_select}{_nextValueFor}{entityInfo.DbFQName}", null, FinishBehavior.DisposeCommand, CommandType.Text);

            return result;
        }
    }
}
