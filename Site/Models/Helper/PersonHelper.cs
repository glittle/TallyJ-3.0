using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.EF;

namespace TallyJ.Models.Helper
{
  public static class PersonHelper
  {

    public const string WordSeparator = "^";

    public static string MakeCombinedInfo(this Person person)
    {
      return new[]
               {
                 person.FirstName,
                 person.LastName,
                 person.OtherNames,
                 person.OtherLastNames,
                 person.OtherInfo,
               }.JoinedAsString(WordSeparator, true).ToLower();
    }

    public static void UpdateCombinedSoundCodes(this Person person)
    {
      person.CombinedSoundCodes = new[]
                                 {
                                   person.FirstName.GenerateDoubleMetaphone(),
                                   person.LastName.GenerateDoubleMetaphone(),
                                   person.OtherNames.GenerateDoubleMetaphone(),
                                   person.OtherLastNames.GenerateDoubleMetaphone(),
                                   person.OtherInfo.GenerateDoubleMetaphone(),
                                 }.JoinedAsString(WordSeparator, true).ToLower();
    }
  }
}