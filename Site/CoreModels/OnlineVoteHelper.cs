using TallyJ.Code;
using TallyJ.Code.Session;
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

    public string GetDecryptedListPool(OnlineVotingInfo onlineVotingInfo, out string errorMessage)
    {
      var rawText = onlineVotingInfo.ListPool;

      if (!EncryptionHelper.IsEncrypted(rawText))
      {
        errorMessage = null;
        return rawText;
      }

      var salt = onlineVotingInfo.C_RowId.ToString();
      var listPool = EncryptionHelper.Decrypt(rawText, salt, out errorMessage);

      if (errorMessage.HasContent())
      {
        // DecryptionError: The provided payload could not be decrypted. Refer to the inner exception for more information.
        // (The key {8bcd2320-f17b-4831-a4b3-f24d74f50e5c} was not found in the key ring.)        

        if (errorMessage.Contains("not found in the key ring"))
        {
          new LogHelper().Add("DecryptionError: " + errorMessage, true, UserSession.VoterId);

          errorMessage = "The server has been changed. You must create your ballot again.";
          return null;
        }

        new LogHelper().Add("DecryptionError: " + errorMessage, true, UserSession.VoterId);
        return null;
      }

      return listPool;
    }

  }
}