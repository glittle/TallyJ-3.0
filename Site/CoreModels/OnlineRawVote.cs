namespace TallyJ.CoreModels
{
  public class OnlineRawVote
  {
    public OnlineRawVote()
    {
      // need this for json deserializing?
    }

    public OnlineRawVote(string text)
    {
      // this constructor used by Cdn ballot importer
      OtherInfo = text;
      First = "";
      Last = "";

      var split = text.Split(' ');
      if (split.Length == 2)
      {
        // likely  first last
        // or last, first
        if (text.Contains(","))
        {
          Last = split[0].Trim(',');
          First = split[1].Trim(',');
        }
        else
        {
          First = split[0].Trim(',');
          Last = split[1].Trim(',');
        }
      }
    }

    public int Id { get; set; }
    public string First { get; set; }
    public string Last { get; set; }
    public string OtherInfo { get; set; }
  }
}