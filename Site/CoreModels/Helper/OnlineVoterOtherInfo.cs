using Newtonsoft.Json;

namespace TallyJ.CoreModels.Helper
{
	public class OnlineVoterOtherInfo
	{
		[JsonProperty("numEl")]
    public int NumElections { get; set; }
	}
}