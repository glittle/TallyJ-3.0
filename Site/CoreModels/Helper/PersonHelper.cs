using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Models;

namespace TallyJ.CoreModels.Helper
{
  public static class PersonHelper
  {
    public const string WordSeparator = "^";

    public static string MakeCombinedInfo(this Person person)
    {
      return new[]
               {
                 person.FirstName.CleanedForSearching(),
                 person.LastName.CleanedForSearching(),
                 person.OtherNames.CleanedForSearching(),
                 person.OtherLastNames.CleanedForSearching(),
                 //person.OtherInfo.CleanedForSearching(),
               }.JoinedAsString(WordSeparator, true)
        .Replace(" ", WordSeparator)
        .ToLower();
    }

    public static void UpdateCombinedSoundCodes(this Person person)
    {
      person.CombinedSoundCodes = new[]
                                    {
                                      person.FirstName.GenerateDoubleMetaphone(),
                                      person.LastName.GenerateDoubleMetaphone(),
                                      person.OtherNames.GenerateDoubleMetaphone(),
                                      person.OtherLastNames.GenerateDoubleMetaphone(),
                                      //person.OtherInfo.GenerateDoubleMetaphone(),
                                    }
                                    .JoinedAsString(WordSeparator, true)
                                    .Replace(" ", WordSeparator)
                                    .ToLower();
    }
  }
}