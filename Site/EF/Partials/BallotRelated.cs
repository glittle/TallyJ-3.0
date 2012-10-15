using System;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;

namespace TallyJ.EF
{
  public interface IBallotBase
  {
    System.Guid BallotGuid { get; set; }
    string StatusCode { get; set; }
  }

  public partial class Ballot : IBallotBase
  {

  }
  public partial class vBallotInfo : IBallotBase
  {

  }

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