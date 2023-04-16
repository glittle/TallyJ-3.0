using System.Linq;
using TallyJ.Code;

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

      // do a rough guess at first and last name
      First = "";
      Last = "";

      // likely   first last
      //     or   last, first

      if (text.Contains(","))
      {
        var split = text.Split(new[] { ',' }, 2);
        Last = split[0].Trim();
        First = split[1].Trim();
      }
      else
      {
        var split = text.Split(' ');
        var numWords = split.Length;

        // if > 2 words, cannot guess which are for first name or last name.  Default to last word --> Last
        Last = split.Last();
        First = split.Reverse().Skip(1).Reverse().JoinedAsString(" ");
      }
    }

    public int Id { get; set; }
    public string First { get; set; }
    public string Last { get; set; }
    public string OtherInfo { get; set; }
  }
}