using System;

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
}