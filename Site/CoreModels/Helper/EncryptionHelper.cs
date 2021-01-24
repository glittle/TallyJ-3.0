using System;
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
    private static IDataProtector _protector;
    public const string EncryptionPrefix = "CfDJ8";

    static EncryptionHelper()
    {
      // _protector = UnityInstance.Resolve<IDataProtectionProvider>().CreateProtector("OnlineVotes.v1");
      _protector = DataProtectionProvider.Create("TallyJv3").CreateProtector("OnlineVotes.v1");
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

      return _protector.Protect(toEncrypt);
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

      if (!text.StartsWith(EncryptionPrefix))
      {
        return false;
      }

      try
      {
        _protector.Unprotect(text);
        return true;
      }
      catch (CryptographicException)
      {
        return false;
      }
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
        unprotected = _protector.Unprotect(encryptedText);
      }
      catch (CryptographicException e)
      {
        errorMessage = e.Message;
        return null;
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