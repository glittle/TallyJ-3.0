using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Models;

namespace TallyJ.CoreModels.Helper
{
  public static class PersonHelper
  {
    public const string WordSeparator = " ";

    public static string MakeCombinedInfo(this Person person)
    {
      return new[]
      {
        person.FirstName,
        person.LastName,
        person.OtherNames,
        person.OtherLastNames,
        // additional - for searching
        person.Area,
        person.OtherInfo,
        person.BahaiId
      }
        .JoinedAsString(WordSeparator, true)
        .ReplacePunctuation(WordSeparator[0])
        .WithoutDiacritics(true);
    }

    public static void UpdateCombinedSoundCodes(this Person person)
    {
      person.CombinedSoundCodes = new[]
      {
        person.FirstName,
        person.LastName,
        person.OtherNames,
        person.OtherLastNames,
        // additional - for searching
        person.Area,
        person.OtherInfo
      }
        .GenerateDoubleMetaphone(" ");
    }
  }
}