using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using Microsoft.VisualStudio.TestTools.UnitTesting.Logging;
using TallyJ.CoreModels.Helper;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class EncryptionTests
  {
    [TestMethod]
    public void Test_Overall()
    {
      var text = "This should be encrypted";
      var salt = "123";

      var encrypted = EncryptionHelper.Encrypt(text, salt);

      encrypted.ShouldNotEqual(text);
      encrypted.Substring(0, 5).ShouldEqual(EncryptionHelper.EncryptionPrefix);

      Logger.LogMessage("Encrypted: " + encrypted);

      var decrypted = EncryptionHelper.Decrypt(encrypted, salt, out var errorMessage);

      decrypted.ShouldEqual(text);

      errorMessage.ShouldEqual(null);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void Test_null()
    {
      string text = null;
      var salt = "123";

      var encrypted = EncryptionHelper.Encrypt(text, salt);

      encrypted.ShouldEqual(null);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void Test_null_salt()
    {
      var text = "abc";
      string salt = null;
      var encrypted = EncryptionHelper.Encrypt(text, salt);
    }

    [TestMethod]
    [ExpectedException(typeof(NotSupportedException))]
    public void Test_empty_salt()
    {
      var text = "abc";
      var salt = "";
      var encrypted = EncryptionHelper.Encrypt(text, salt);
    }

    [TestMethod]
    public void Decrypt_null_text()
    {
      string encrypted = null;
      string salt = null;

      var decrypted = EncryptionHelper.Decrypt(encrypted, salt, out var errorMessage);

      errorMessage.ShouldEqual("Empty string");
      decrypted.ShouldEqual(null);
    }

    [TestMethod]
    public void Decrypt_null_salt()
    {
      var encrypted = "sflsafjsf";
      string salt = null;

      var decrypted = EncryptionHelper.Decrypt(encrypted, salt, out var errorMessage);

      errorMessage.ShouldEqual("Must supply salt value");
      decrypted.ShouldEqual(null);
    }

    [TestMethod]
    public void Decrypt_randomText()
    {
      string salt = "123";

      var decrypted = EncryptionHelper.Decrypt("random fake encrypted text", salt, out var errorMessage);
      // The provided payload cannot be decrypted because it was not protected with this protection provider. --> not valid

      Logger.LogMessage("RandomText error: " + errorMessage);
      errorMessage.ShouldNotEqual("");
      decrypted.ShouldEqual(null);

      var decrypted2 = EncryptionHelper.Decrypt("a", salt, out errorMessage);
      // An error occurred during a cryptographic operation. --> string too short?
      Logger.LogMessage("RandomText error: " + errorMessage);
      errorMessage.ShouldNotEqual("");
      decrypted2.ShouldEqual(null);

      // https://docs.microsoft.com/en-us/aspnet/core/security/data-protection/compatibility/replacing-machinekey?view=aspnetcore-5.0
      // CfDJ8 is a standard encryption prefix
      var decrypted3 = EncryptionHelper.Decrypt(EncryptionHelper.EncryptionPrefix + "random fake encrypted text", salt, out errorMessage);
      Logger.LogMessage("RandomText error: " + errorMessage);
      // The provided payload cannot be decrypted because it was protected with a newer version of the protection provider.
      errorMessage.ShouldNotEqual("");
      decrypted3.ShouldEqual(null);


    }

    [TestMethod]
    public void Decrypt_wrong_salt()
    {
      var text = "This is the test string";
      var salt = "123";

      var encrypted = EncryptionHelper.Encrypt(text, salt);
      encrypted.ShouldNotEqual(text);

      var badSalt = "1234";
      var decrypted = EncryptionHelper.Decrypt(encrypted, badSalt, out var errorMessage);

      errorMessage.ShouldEqual("Invalid salt");
      decrypted.ShouldEqual(null);
    }

    [TestMethod]
    public void IsEncrypted()
    {
      EncryptionHelper.IsEncrypted("Not encrypted").ShouldEqual(false);
      EncryptionHelper.IsEncrypted("").ShouldEqual(false);
      EncryptionHelper.IsEncrypted(null).ShouldEqual(false);

      EncryptionHelper.IsEncrypted(EncryptionHelper.EncryptionPrefix + "Not encrypted").ShouldEqual(false);


      var encrypted = EncryptionHelper.Encrypt("Hello", "salt");
      EncryptionHelper.IsEncrypted(encrypted).ShouldEqual(true);
    }

    [TestMethod]
    public void v1_to_v2_Decrypt()
    {
      // captured output from first text (above) when temporarily hardcoded to use v1
      var v1result =
        "CfDJ8CDzsoxHFAROsU-9aq_i2lqJcZ7xNV32pzWQpLGqRuTQ2RoioZ2DXbrHyretWC6YoM2gRiIo-QCyWiKwzhSmk4jqYhhKfgirnnabUta-PEJPV7ZExHPTIEXXs4O75mr9oWGkuWg7WU2WzdMOfbFEImM";
      var salt = "123";

      var v2result = EncryptionHelper.Decrypt(v1result, salt, out var eroMessage);
      v2result.ShouldEqual("This should be encrypted");
    }
  }
}