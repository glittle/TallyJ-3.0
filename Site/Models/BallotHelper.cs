namespace TallyJ.Models
{
  public class BallotHelper
  {
    #region Nested type: BallotStatusCode

    public static class BallotStatusCode
    {
      public const string Ok = "Ok";
      public const string InEdit = "InEdit";
      public const string Review = "Review";
      public const string TooFew = "TooFew";
      public const string TooMany = "TooMany";
      public const string HasDup = "HasDup";
    }

    #endregion

    #region Nested type: VoteStatusCode

    public static class VoteStatusCode
    {
      public const string Ok = "Ok";
    }

    #endregion
  }
}