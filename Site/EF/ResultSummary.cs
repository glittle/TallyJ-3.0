//------------------------------------------------------------------------------
// <auto-generated>
//     This code was generated from a template.
//
//     Manual changes to this file may cause unexpected behavior in your application.
//     Manual changes to this file will be overwritten if the code is regenerated.
// </auto-generated>
//------------------------------------------------------------------------------

namespace TallyJ.EF
{
    using System;
    using System.Collections.Generic;
    
    public partial class ResultSummary
    {
        public int C_RowId { get; set; }
        public System.Guid ElectionGuid { get; set; }
        public string ResultType { get; set; }
        public Nullable<bool> UseOnReports { get; set; }
        public Nullable<int> NumVoters { get; set; }
        public Nullable<int> NumEligibleToVote { get; set; }
        public Nullable<int> MailedInBallots { get; set; }
        public Nullable<int> DroppedOffBallots { get; set; }
        public Nullable<int> InPersonBallots { get; set; }
        public Nullable<int> SpoiledBallots { get; set; }
        public Nullable<int> SpoiledVotes { get; set; }
        public Nullable<int> TotalVotes { get; set; }
        public Nullable<int> BallotsReceived { get; set; }
        public Nullable<int> BallotsNeedingReview { get; set; }
        public Nullable<int> CalledInBallots { get; set; }
        public Nullable<int> OnlineBallots { get; set; }
        public Nullable<int> SpoiledManualBallots { get; set; }
        public Nullable<int> Custom1Ballots { get; set; }
        public Nullable<int> Custom2Ballots { get; set; }
        public Nullable<int> Custom3Ballots { get; set; }
        public Nullable<int> ImportedBallots { get; set; }
    }
}
