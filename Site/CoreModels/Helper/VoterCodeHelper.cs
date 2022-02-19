using TallyJ.CoreModels.Hubs;

namespace TallyJ.CoreModels.Helper
{
  public class VoterCodeHelper
  {
    public object GetCode(string type, string method, string target, string hubKey)
    {
      new VoterCodeHub().SetStatus(hubKey, "C.." + type + " " + method + " " + target);

      return new
      {
        Success = false,
        Message = "In dev"
      };

    }
  }
}