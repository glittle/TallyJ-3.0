using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class vVoteInfo
    {
        [Key]
        public int VoteId { get; set; }
        public string VoteStatusCode { get; set; }
        public Nullable<int> SingleNameElectionCount { get; set; }
        public Nullable<bool> IsSingleNameElection { get; set; }
        public int PositionOnBallot { get; set; }
        public Nullable<System.Guid> VoteIneligibleReasonGuid { get; set; }
        public string PersonCombinedInfoInVote { get; set; }
        public string PersonCombinedInfo { get; set; }
        public Nullable<System.Guid> PersonGuid { get; set; }
        public Nullable<int> PersonId { get; set; }
        public string PersonFullName { get; set; }
        public string PersonFullNameFL { get; set; }
        public Nullable<bool> CanReceiveVotes { get; set; }
        public Nullable<System.Guid> PersonIneligibleReasonGuid { get; set; }
        public Nullable<int> ResultId { get; set; }
        public System.Guid BallotGuid { get; set; }
        public int BallotId { get; set; }
        public string BallotStatusCode { get; set; }
        public string C_BallotCode { get; set; }
        public int LocationId { get; set; }
        public string LocationTallyStatus { get; set; }
        public System.Guid ElectionGuid { get; set; }
    }
}
