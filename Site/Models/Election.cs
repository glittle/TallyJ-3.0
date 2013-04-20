using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class Election : Entity
    {
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
    }
}
