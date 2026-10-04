using Newtonsoft.Json;

namespace TallyJ.CoreModels.Helper
{
  public class OnlineVoterOtherInfo
  {
    [JsonProperty("numEl")]
    public int NumElections { get; set; }
    [JsonProperty("open")]
    public int Open { get; set; }
    [JsonProperty("usedWA")]
    public bool UsedWhatsApp { get; set; }

    /// <summary>
    /// Wrong guesses against the current verify code. Reset when a new code is issued.
    /// Short name so the JSON stays inside OnlineVoter.OtherInfo (nvarchar 200).
    /// </summary>
    [JsonProperty("fg")]
    public int FailedGuesses { get; set; }
  }
}