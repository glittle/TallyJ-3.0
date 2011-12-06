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
  public partial class ResultSummary : IHasElectionGuid {}
  public partial class Message : IHasElectionGuid {}


}
