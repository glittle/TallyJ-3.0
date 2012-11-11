using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace TallyJ.EF
{
  [MetadataType(typeof (PersonMetadata))]
  public partial class Person
  {

    private class PersonMetadata
    {
      [DebuggerDisplay("Local = {RegistrationTime.ToLocalTime()}, UTC = {RegistrationTime}")]
      public object RegistrationTime { get; set; }
    }
  }
}