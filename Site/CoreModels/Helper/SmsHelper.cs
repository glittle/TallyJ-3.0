using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  public class SmsHelper
  {
    public bool SendWhenProcessed(Election e, Person p, OnlineVoter ov, out string error)
    {
      // only send if they asked for it
      if (ov.EmailCodes == null || !ov.EmailCodes.Contains("p") || p.Phone.HasNoContent())
      {
        error = null;
        return false;
      }

      error = "SMS notification not implemented yet";
      return false;
    }

    public bool SendWhenOpened(string phone, string cFullNameFl, string html, out string error)
    {
      error = "SMS notification not implemented yet";
      return false;
    }

    public bool SendVoterTestMessage(string phone, out string error)
    {
      error = "SMS notification not implemented yet";
      return false;
    }
  }
}