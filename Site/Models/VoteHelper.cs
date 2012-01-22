using System;

namespace TallyJ.Models
{
  public class VoteHelper
  {
    public static class IneligibleReason
    {
      /// <Summary>Hardcoded value from the database</Summary>
      public static readonly Guid BlankVote = new Guid("DA27534D-D7E8-E011-A095-002269C41D11");
    }

    public static class VoteStatusCode
    {
      public const string Ok = "Ok";
      public const string Changed = "Changed"; // if the person info does not match
    }
  }
}