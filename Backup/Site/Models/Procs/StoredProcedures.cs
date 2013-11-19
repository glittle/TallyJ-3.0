using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Linq;
using TallyJ.Code;

namespace TallyJ.Models
{
    public partial class TallyJ2dContext
    {
        public virtual IEnumerable<CloneElection_Result> CloneElection(Guid? sourceElection, string byLoginId)
        {
            var sourceElectionParameter = sourceElection.HasValue
                                              ? new SqlParameter("SourceElection", sourceElection)
                                              : new SqlParameter("SourceElection", SqlDbType.UniqueIdentifier);

            var byLoginIdParameter = byLoginId != null
                                         ? new SqlParameter("ByLoginId", byLoginId)
                                         : new SqlParameter("ByLoginId", SqlDbType.VarChar);

            return Database.SqlQuery<CloneElection_Result>("tj.CloneElection"
                                                           ,
                                                           sourceElectionParameter,
                                                           byLoginIdParameter);
        }

        public virtual long CurrentRowVersion()
        {
//            return Database.ExecuteSqlCommand("tj.CurrentRowVersion");

            var x = Database.SqlQuery<long>("select cast(@@DbTs as bigint)");
            return x.First();
//            return ((IObjectContextAdapter) this).ObjectContext.ExecuteFunction<long?>("CurrentRowVersion");
        }

        public virtual int EraseElectionContents(Guid? electionGuidToClear, bool? eraseTallyContent, string byLoginId)
        {
            var electionGuidToClearParameter = electionGuidToClear.HasValue
                                                   ? new SqlParameter("ElectionGuidToClear", electionGuidToClear)
                                                   : new SqlParameter("ElectionGuidToClear", SqlDbType.UniqueIdentifier);

            var eraseTallyContentParameter = eraseTallyContent.HasValue
                                                 ? new SqlParameter("EraseTallyContent", eraseTallyContent)
                                                 : new SqlParameter("EraseTallyContent", SqlDbType.Bit);

            var byLoginIdParameter = byLoginId != null
                                         ? new SqlParameter("ByLoginId", byLoginId)
                                         : new SqlParameter("ByLoginId", SqlDbType.VarChar);

            return Database.ExecuteSqlCommand("EraseElectionContents",
                                              electionGuidToClearParameter,
                                              eraseTallyContentParameter,
                                              byLoginIdParameter);
        }

        public virtual IEnumerable<SqlSearch_Result> SqlSearch(Guid election, string term1, string term2, string sound1,
                                                               string sound2, int maxToReturn,
                                                               out bool moreFound)
        {
            var electionParameter = new SqlParameter("Election", election);

            var term1Parameter = new SqlParameter("Term1", term1.ForSqlParameter());

            var term2Parameter = new SqlParameter("Term2", term2.ForSqlParameter());

            var sound1Parameter = new SqlParameter("Sound1", sound1.ForSqlParameter());

            var sound2Parameter = new SqlParameter("Sound2", sound2.ForSqlParameter());

            var maxToReturnParameter = new SqlParameter("MaxToReturn", maxToReturn);

            var moreExactMatchesFound = new SqlParameter("MoreExactMatchesFound", SqlDbType.Bit)
                {
                    Direction = ParameterDirection.Output
                };

            var results =
                Database.SqlQuery<SqlSearch_Result>(
                    "exec tj.SqlSearch @Election, @Term1, @Term2, @Sound1, @Sound2, @MaxToReturn, @MoreExactMatchesFound out",
                    electionParameter,
                    term1Parameter,
                    term2Parameter,
                    sound1Parameter,
                    sound2Parameter,
                    maxToReturnParameter,
                    moreExactMatchesFound).ToList();

            moreFound = (bool) moreExactMatchesFound.Value;

            return results;
        }

        public virtual int UpdateVoteStatus(int? voteRowId, string statusCode)
        {
            var voteRowIdParameter = voteRowId.HasValue
                                         ? new SqlParameter("VoteRowId", voteRowId)
                                         : new SqlParameter("VoteRowId", SqlDbType.Int);

            var statusCodeParameter = statusCode != null
                                          ? new SqlParameter("StatusCode", statusCode)
                                          : new SqlParameter("StatusCode", SqlDbType.VarChar);

            return Database.ExecuteSqlCommand("UpdateVoteStatus", voteRowIdParameter,
                                              statusCodeParameter);
        }
    }
}