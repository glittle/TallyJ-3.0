//------------------------------------------------------------------------------
// <auto-generated>
//    This code was generated from a template.
//
//    Manual changes to this file may cause unexpected behavior in your application.
//    Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TallyJ.EF
{
    using System;
    using System.Collections.Generic;
    
    public partial class vVoteInfo
    {
        public int VoteId { get; set; }
        public string VoteStatusCode { get; set; }
        public Nullable<int> SingleNameElectionCount { get; set; }
        public int PositionOnBallot { get; set; }
        public Nullable<System.Guid> VoteInvalidReasonGuid { get; set; }
        public Nullable<int> VoteInvalidReasonId { get; set; }
        public string VoteInvalidReasonDesc { get; set; }
        public string PersonCombinedInfoInVote { get; set; }
        public string PersonCombinedInfo { get; set; }
        public Nullable<System.Guid> PersonGuid { get; set; }
        public Nullable<int> PersonId { get; set; }
        public string PersonFullName { get; set; }
        public Nullable<bool> CanReceiveVotes { get; set; }
        public Nullable<System.Guid> PersonIneligibleReasonGuid { get; set; }
        public Nullable<int> PersonIneligibleReasonId { get; set; }
        public string PersonIneligibleReasonDesc { get; set; }
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
