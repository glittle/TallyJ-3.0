using TallyJ.Code;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class OnlineVoteHelper
  {
    /// <summary>
    /// Encrypt <param name="newListPoolToEncrypt"></param> and put it into the <param name="onlineVotingInfo"></param>.
    /// </summary>
    /// <param name="onlineVotingInfo"></param>
    /// <param name="newListPoolToEncrypt"></param>
    public void SetListPoolEncrypted(OnlineVotingInfo onlineVotingInfo, string newListPoolToEncrypt = null)
    {
      var encrypted = EncryptionHelper.Encrypt(newListPoolToEncrypt ?? onlineVotingInfo.ListPool, onlineVotingInfo.C_RowId.ToString());
      onlineVotingInfo.ListPool = encrypted;
    }

    public string GetDecryptedListPool(OnlineVotingInfo onlineVotingInfo)
    {
      var rawText = onlineVotingInfo.ListPool;

      if (!EncryptionHelper.IsEncrypted(rawText))
      {
        return rawText;
      }

      var salt = onlineVotingInfo.C_RowId.ToString();
      var listPool = EncryptionHelper.Decrypt(rawText, salt, out string errorMessage);

      if (errorMessage.HasContent())
      {
        // failed
        // log it??
        return null;
      }

      return listPool;
    }

  }
}