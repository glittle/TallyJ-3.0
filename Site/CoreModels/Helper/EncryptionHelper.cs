using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Web.Security;
using Microsoft.AspNetCore.DataProtection;
using TallyJ.Code;
using TallyJ.Code.UnityRelated;
using Twilio.Rest.Taskrouter.V1.Workspace.TaskQueue;

namespace TallyJ.CoreModels.Helper
{
  public static class EncryptionHelper
  {
    private static IDataProtector _protectorV1;
    private static IDataProtector _protectorV2;
    public const string EncryptionPrefix = "CfDJ8"; // standard .net prefix

    /*
     * Need to make sure that the encryption keys are transferable between web servers. If a server fails during
     * an election, we must be able to move the keys to a different server to continue the process. The new server
     * must be able to read the votes encrypted by the first server. Otherwise, the new server would be unable
     * to read any of the votes previously submitted.
     * 
     */

    static EncryptionHelper()
    {
      // keeping V1 for backward compatibility only
      _protectorV1 = DataProtectionProvider.Create("TallyJv3").CreateProtector("OnlineVotes.v1");


      var folder = SettingsHelper.Get("EncryptionKeysFolder", "");
      if (folder.HasNoContent())
      {
        throw new ApplicationException("Invalid EncryptionKeysFolder in AppSettings");
      }
      var keyFolder = new DirectoryInfo(folder);
      _protectorV2 = DataProtectionProvider.Create(keyFolder).CreateProtector("OnlineVotes.v2");
    }

    /// <summary>
    /// Returns an encrypted string based on the <param name="text"></param> and <param name="salt"></param>.
    /// </summary>
    /// <param name="text"></param>
    /// <param name="salt"></param>
    /// <returns></returns>
    public static string Encrypt(string text, string salt)
    {
      if (text.HasNoContent()) throw new NotSupportedException("Cannot encrypt null or empty values");
      if (salt.HasNoContent()) throw new NotSupportedException("Must supply salt value");

      var toEncrypt = salt + text;

      return _protectorV2.Protect(toEncrypt);
    }

    /// <summary>
    /// If text may not be encrypted, test here first.
    /// </summary>
    /// <param name="text"></param>
    /// <returns></returns>
    public static bool IsEncrypted(string text)
    {
      if (text.HasNoContent())
      {
        return false;
      }

      return text.StartsWith(EncryptionPrefix);

      // we found the prefix, so it is encrypted... will find out later if we can decrypt it!

      // try
      // {
      //   _protectorV2.Unprotect(text);
      //   return true;
      // }
      // catch (Exception)
      // {
      //   try
      //   {
      //     // fall back to V1
      //     _protectorV1.Unprotect(text);
      //     return true;
      //   }
      //   catch (CryptographicException ex)
      //   {
      //     return false;
      //   }
      // }
    }

    /// <summary>
    /// If decryption fails, errorMessage will have a value.
    /// </summary>
    /// <param name="encryptedText"></param>
    /// <param name="salt"></param>
    /// <param name="errorMessage">A message if the <param name="encryptedText"></param> cannot be decrypted.</param>
    /// <returns></returns>
    public static string Decrypt(string encryptedText, string salt, out string errorMessage)
    {
      if (encryptedText.HasNoContent())
      {
        errorMessage = "Empty string";
        return null;
      }

      if (salt.HasNoContent())
      {
        errorMessage = "Must supply salt value";
        return null;
      }

      if (!IsEncrypted(encryptedText))
      {
        // this content is not actually encrypted... just return it
        errorMessage = "Not encrypted";
        return null;
      }

      string unprotected;
      try
      {
        unprotected = _protectorV2.Unprotect(encryptedText);
      }
      catch (CryptographicException e1)
      {
        try
        {
          // fallback to v1
          unprotected = _protectorV1.Unprotect(encryptedText);
        }
        catch (CryptographicException e2)
        {
          // the first exception is the one we are interested in
          if (e1.Message.Contains("was not found in the key ring"))
          {
            errorMessage = "The server has been changed... You must create your ballot again.";
          }
          else
          {
            errorMessage = e1.Message + $" ({e2.Message})";
          }

          return null;
        }
      }

      if (unprotected.StartsWith(salt))
      {
        errorMessage = null;
        return unprotected.Substring(salt.Length);
      }

      errorMessage = "Invalid salt";
      return null;
    }
  }
}