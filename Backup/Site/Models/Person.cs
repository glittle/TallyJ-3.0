using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;

namespace TallyJ.Models
{
    public partial class Person : Entity
    {
        public System.Guid ElectionGuid { get; set; }
        public System.Guid PersonGuid { get; set; }
        public string LastName { get; set; }
        public string FirstName { get; set; }
        public string OtherLastNames { get; set; }
        public string OtherNames { get; set; }
        public string OtherInfo { get; set; }
        public string Area { get; set; }
        public string BahaiId { get; set; }
        public string CombinedInfo { get; set; }
        public string CombinedSoundCodes { get; set; }
        public string CombinedInfoAtStart { get; set; }
        public string AgeGroup { get; set; }
        public Nullable<bool> CanVote { get; set; }
        public Nullable<bool> CanReceiveVotes { get; set; }
        public Nullable<System.Guid> IneligibleReasonGuid { get; set; }
        public Nullable<System.DateTime> RegistrationTime { get; set; }
        public Nullable<System.Guid> VotingLocationGuid { get; set; }
        public string VotingMethod { get; set; }
        public Nullable<int> EnvNum { get; set; }
        public byte[] C_RowVersion { get; set; }
        public string C_FullName { get; set; }
        public Nullable<long> C_RowVersionInt { get; set; }
        public string C_FullNameFL { get; set; }
        public Nullable<System.Guid> TellerAtKeyboard { get; set; }
        public Nullable<System.Guid> TellerAssisting { get; set; }
    }
}
