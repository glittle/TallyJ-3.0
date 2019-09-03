using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Web.Hosting;
using TallyJ.Code;

namespace TallyJ.CoreModels
{
  public class EmailHelper
  {
    public bool SendTest(string email, out string error)
    {
      var message = new MailMessage();

      message.To.Add(email);
      message.From = new MailAddress("system@tallyj.com", "TallyJ System");

      var html = File.ReadAllText(System.AppDomain.CurrentDomain.BaseDirectory + "/App_Data/TestEmail.html");
      message.Body = html;
      message.IsBodyHtml = true;

      message.Subject = "Test 1";

      return SendEmail(message, out error);
    }

    public bool SendWhenOpen(string email, out string error)
    {
      error = null;
      return false;
    }

    public bool SendWhenProcessed(string email, out string error)
    {
      error = null;
      return false;
    }

    /// <summary>
    /// 
    /// </summary>
    /// <param name="message"></param>
    /// <param name="errorMessage"></param>
    /// <remarks>
    /// Settings from AppSettings in Web.config or similar:
    /// "SmtpHost", "localhost"
    /// "SmtpPickupDirectory", ""
    /// "SmtpUsername", ""
    /// "SmtpPassword", ""
    /// "SmtpSecure", false
    /// "SmtpPort", 25
    /// "SmtpTimeoutMs", 5 * 1000
    /// </remarks>
    /// <returns></returns>
    public bool SendEmail(MailMessage message, out string errorMessage)
    {
      var host = SettingsHelper.Get("SmtpHost", "localhost");
      var pickupDirectory = SettingsHelper.Get("SmtpPickupDirectory", "");
      var senderHostName = message.Sender?.Host ?? "TallyJ";

      message.Headers.Add("Message-Id", "<{0}@{1}>".FilledWith(Guid.NewGuid(), senderHostName));

      try
      {
        var smtpUsername = SettingsHelper.Get("SmtpUsername", "");
        var credentials = smtpUsername.HasContent()
          ? new NetworkCredential(smtpUsername, SettingsHelper.Get("SmtpPassword", ""))
          : CredentialCache.DefaultNetworkCredentials;

        using (var smtpClient = new SmtpClient
        {
          Host = host,
          EnableSsl = !pickupDirectory.HasContent() && SettingsHelper.Get("SmtpSecure", false),
          Port = SettingsHelper.Get("SmtpPort", 25),
          PickupDirectoryLocation = pickupDirectory,
          DeliveryMethod = pickupDirectory.HasContent() ? SmtpDeliveryMethod.SpecifiedPickupDirectory : SmtpDeliveryMethod.Network,
          Credentials = credentials,
          Timeout = SettingsHelper.Get("SmtpTimeoutMs", 5 * 1000) // milliseconds
        })
        {
          smtpClient.Send(message);
          // .SendAsync does not help? the entire page is held until the operation finishes or times out
          errorMessage = "";

          return true;
        }
      }
      catch (SmtpFailedRecipientsException ex)
      {
        var who = ex.InnerExceptions.Select(e => e.FailedRecipient).JoinedAsString(", ");
        var why = ex.InnerExceptions.Select(e => e.GetAllMsgs("; "));
        errorMessage = $"Email Failed for {who}: {why}";
        
        new LogHelper().Add(errorMessage, true);
      }
      catch (SmtpFailedRecipientException ex)
      {
        errorMessage = $"Email Failed for {ex.FailedRecipient}: {ex.GetAllMsgs("; ")}";
        new LogHelper().Add(errorMessage, true);
      }
      catch (Exception ex)
      {
        errorMessage = ex.GetAllMsgs("; ");
        new LogHelper().Add(errorMessage, true);
      }

      return false;
    }
  }
}