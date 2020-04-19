namespace TallyJ.CoreModels
{
  public static class VoteStatusCode
  {
    public const string Ok = "Ok";
    public const string Changed = "Changed"; // if the person info does not match
    public const string Spoiled = "Spoiled";
    public const string OnlineRaw = "Raw";
  }
}