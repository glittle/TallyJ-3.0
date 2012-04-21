using System;
using System.Data.Objects.DataClasses;

namespace TallyJ.EF
{
  public interface IHasElectionGuid
  {
     Guid ElectionGuid { get; set; }
  }

  public partial class Election : IHasElectionGuid {}
  public partial class Location : IHasElectionGuid {}
  public partial class Person : IHasElectionGuid {}
  //public partial class Computer : IHasElectionGuid {}
  public partial class Teller : IHasElectionGuid {}
  public partial class Result : IHasElectionGuid {}
  public partial class ResultSummary : IHasElectionGuid
  {
    /// <Summary>Total of all collected</Summary>
    public int TotalBallotsCollected
    {
      get
      {
        return InPersonBallots.GetValueOrDefault()
               + DroppedOffBallots.GetValueOrDefault()
               + MailedInBallots.GetValueOrDefault()
               + CalledInBallots.GetValueOrDefault();
      }
    }
  }
  public partial class Message : IHasElectionGuid {}


  
}
