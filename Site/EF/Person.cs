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
    
    public partial class Person
    {
        public int C_RowId { get; set; }
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
        public string Teller1 { get; set; }
        public string Teller2 { get; set; }
        public string Email { get; set; }
        public string Phone { get; set; }
        public Nullable<bool> HasOnlineBallot { get; set; }
    }
}
