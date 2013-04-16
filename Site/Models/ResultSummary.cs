using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class ResultSummary : Entity
    {
        public System.Guid ElectionGuid { get; set; }
        public string ResultType { get; set; }
        public Nullable<bool> UseOnReports { get; set; }
        
        /// <summary>
        /// Number who did vote (same as ballots received)
        /// </summary>
        public Nullable<int> NumVoters { get; set; }
        public Nullable<int> NumEligibleToVote { get; set; }
        public Nullable<int> EnvelopesMailedIn { get; set; }
        public Nullable<int> EnvelopesDroppedOff { get; set; }
        public Nullable<int> EnvelopesInPerson { get; set; }
        public Nullable<int> EnvelopesCalledIn { get; set; }

        public Nullable<int> SpoiledBallots { get; set; }
        public Nullable<int> SpoiledManualBallots { get; set; }
        public Nullable<int> SpoiledVotes { get; set; }

        /// <summary>
        /// TotalVotes - may not be needed... is just #ballots x #names
        /// </summary>
        public Nullable<int> TotalVotes { get; set; }
        public Nullable<int> NumBallotsEntered { get; set; }
        public Nullable<int> BallotsNeedingReview { get; set; }
    }
}
