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
    
    public partial class Election
    {
        public int C_RowId { get; set; }
        public System.Guid ElectionGuid { get; set; }
        public string Name { get; set; }
        public string Convenor { get; set; }
        public Nullable<System.DateTime> DateOfElection { get; set; }
        public string ElectionType { get; set; }
        public string ElectionMode { get; set; }
        public Nullable<int> NumberToElect { get; set; }
        public Nullable<int> NumberExtra { get; set; }
        public string CanVote { get; set; }
        public string CanReceive { get; set; }
        public Nullable<int> LastEnvNum { get; set; }
        public string TallyStatus { get; set; }
        public Nullable<bool> ShowFullReport { get; set; }
        public Nullable<System.Guid> LinkedElectionGuid { get; set; }
        public string LinkedElectionKind { get; set; }
        public string OwnerLoginId { get; set; }
        public string ElectionPasscode { get; set; }
        public Nullable<System.DateTime> ListedForPublicAsOf { get; set; }
        public byte[] C_RowVersion { get; set; }
        public Nullable<bool> ListForPublic { get; set; }
        public Nullable<bool> ShowAsTest { get; set; }
        public Nullable<bool> UseCallInButton { get; set; }
        public Nullable<bool> HidePreBallotPages { get; set; }
        public Nullable<bool> MaskVotingMethod { get; set; }
        public Nullable<System.DateTime> OnlineWhenOpen { get; set; }
        public Nullable<System.DateTime> OnlineWhenClose { get; set; }
        public bool OnlineCloseIsEstimate { get; set; }
        public string OnlineSelectionProcess { get; set; }
        public Nullable<System.DateTime> OnlineAnnounced { get; set; }
        public string EmailFromAddress { get; set; }
        public string EmailFromName { get; set; }
        public string EmailText { get; set; }
        public string SmsText { get; set; }
        public string EmailSubject { get; set; }
        public string CustomMethods { get; set; }
        public string VotingMethods { get; set; }
        public string Flags { get; set; }
    }
}
