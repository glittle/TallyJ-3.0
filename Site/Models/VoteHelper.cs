using System;

namespace TallyJ.Models
{
  public class VoteHelper
  {
    public static class VoteStatusCode
    {
      public const string Ok = "Ok";
      public const string Changed = "Changed"; // if the person info does not match
      public const string Spoiled = "Spoiled";
    }
  }
}