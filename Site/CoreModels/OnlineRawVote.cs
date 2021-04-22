using System.Linq;

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
      var numWords = split.Length;
      if (numWords >= 2)
      {
        // likely  first last
        // or last, first
        var name1 = split.First().Trim(',');
        var name2 = split.Last().Trim(',');
        if (text.Contains(","))
        {
          Last = name1;
          if (numWords == 2)
          {
            First = name2;
          }
        }
        else
        {
          First = name1;
          if (numWords == 2)
          {
            Last = name2;
          }
        }
      }
    }

    public int Id { get; set; }
    public string First { get; set; }
    public string Last { get; set; }
    public string OtherInfo { get; set; }
  }
}