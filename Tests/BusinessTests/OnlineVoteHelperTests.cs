using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;
using Tests.Support;

namespace Tests.BusinessTests
{
  [TestClass]
  public class OnlineVoteHelperTests
  {
    [TestMethod]
    public void SetListPoolEncrypted_null_pool_does_not_throw()
    {
      var helper = new OnlineVoteHelper();
      var info = new OnlineVotingInfo { C_RowId = 1, ListPool = null };

      helper.SetListPoolEncrypted(info);

      info.ListPool.ShouldEqual(null);
    }

    [TestMethod]
    public void SetListPoolEncrypted_empty_pool_does_not_throw()
    {
      var helper = new OnlineVoteHelper();
      var info = new OnlineVotingInfo { C_RowId = 1, ListPool = "" };

      helper.SetListPoolEncrypted(info);

      info.ListPool.ShouldEqual("");
    }

    [TestMethod]
    public void SetListPoolEncrypted_plaintext_pool_is_encrypted()
    {
      var helper = new OnlineVoteHelper();
      var plaintext = "[{\"Id\":1}]";
      var info = new OnlineVotingInfo { C_RowId = 42, ListPool = plaintext };

      helper.SetListPoolEncrypted(info);

      EncryptionHelper.IsEncrypted(info.ListPool).ShouldEqual(true);
      info.ListPool.ShouldNotEqual(plaintext);

      var decrypted = helper.GetDecryptedListPool(info, out var errorMessage);
      decrypted.ShouldEqual(plaintext);
      errorMessage.ShouldEqual(null);
    }
  }
}
