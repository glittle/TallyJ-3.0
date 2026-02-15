using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  public class WhatsAppHelper : MessageHelperBase
  {
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    public bool SendVerifyCodeToVoter(string phone, string code, string hubKey, Guid electionGuid, Guid personGuid, out string error)
    {
      var text = GetWhatsAppTemplate("VerifyCodeSms").FilledWithObject(new
      {
        newCode = code
      });

      return SendWhatsAppMessage(phone, text, personGuid, out error, electionGuid);
    }

    public JsonResult SendHeadTellerMessage(string idList)
    {
      var db = UserSession.GetNewDbContext;
      var hostSite = SettingsHelper.Get("HostSite", "");
      var election = UserSession.CurrentElection;
      var text = election.SmsText;

      if (text.HasNoContent())
      {
        return new
        {
          Success = false,
          Status = "WhatsApp text not set"
        }.AsJsonResult();
      }

      var personIds = idList.Replace("[", "").Replace("]", "").Split(',').Select(s => s.AsInt()).ToList();

      var phoneNumbersToSendTo = db.Person
        .Where(p => p.ElectionGuid == election.ElectionGuid && p.Phone != null && p.Phone.Trim().Length > 0)
        .Where(p => p.CanVote.Value)
        .Where(p => personIds.Contains(p.C_RowId))
        .ToList()
        .Where(p => p.HasWhatsApp.Value)
        .Select(p => new
        {
          p.Phone,
          PersonName = p.C_FullNameFL,
          p.FirstName,
          VoterContact = p.Phone,
          p.PersonGuid,
        })
        .ToList();

      var numSent = 0;
      var errors = new List<string>();
      var numToSend = phoneNumbersToSendTo.Count;

      LogHelper.Add($"WhatsApp: Sending to {numToSend} {numToSend.Plural("people", "person")} (see above)", true);
      var startTime = DateTime.Now;

      foreach (var p in phoneNumbersToSendTo)
      {
        var phoneNumber = p.Phone;

        if (!IsValidPhoneNumber(phoneNumber))
        {
          errors.Add("Invalid phone number: " + phoneNumber);
          continue;
        }

        var messageText = text.FilledWithObject(new
        {
          hostSite,
          p.PersonName,
          p.FirstName,
          p.VoterContact,
        });

        var ok = SendWhatsAppMessage(phoneNumber, messageText, p.PersonGuid, out var errorMessage, election.ElectionGuid);

        if (ok)
          numSent++;
        else
          errors.Add(errorMessage);
      }

      var seconds = (DateTime.Now - startTime).TotalSeconds.AsInt();

      var msg2 = $"WhatsApp: Sent to {numSent} {numSent.Plural("people", "person")} in {seconds} second{seconds.Plural()}";
      if (errors.Count > 0) msg2 += $" - {errors.Count} failed to send. First error: {errors[0]}";
      LogHelper.Add(msg2, true);

      return new
      {
        Success = numSent > 0,
        Status = msg2
      }.AsJsonResult();
    }

    private bool SendWhatsAppMessage(string phoneNumber, string message, Guid personGuid, out string errorMessage, Guid openElectionGuid)
    {
      var idInstance = SettingsHelper.Get("greenapi-IdInstance", "");
      var apiToken = SettingsHelper.Get("greenapi-ApiTokenInstance", "");
      var apiUrl = SettingsHelper.Get("greenapi-ApiUrl", "https://api.green-api.com");

      if (idInstance.HasNoContent() || apiToken.HasNoContent())
      {
        errorMessage = "Server not configured for WhatsApp (GreenAPI).";
        return false;
      }

      if (!IsValidPhoneNumber(phoneNumber))
      {
        errorMessage = "Invalid phone number: " + phoneNumber;
        return false;
      }

      var chatId = FormatPhoneNumberForGreenApi(phoneNumber);
      var url = $"{apiUrl}/waInstance{idInstance}/sendMessage/{apiToken}";

      var requestBody = new
      {
        chatId,
        message
      };

      var jsonContent = JsonConvert.SerializeObject(requestBody);
      var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

      try
      {
        var response = _httpClient.PostAsync(url, content).Result;
        var responseContent = response.Content.ReadAsStringAsync().Result;

        if (!response.IsSuccessStatusCode)
        {
          var errorResponse = TryParseErrorResponse(responseContent);
          errorMessage = $"GreenAPI Error: {errorResponse}";
          return false;
        }

        var responseObj = JsonConvert.DeserializeObject<GreenApiSendMessageResponse>(responseContent);
        var messageId = responseObj?.idMessage;

        if (messageId.HasNoContent())
        {
          errorMessage = "GreenAPI Error: No message ID returned";
          return false;
        }

        var dbContext = UserSession.GetNewDbContext;
        var utcNow = DateTime.UtcNow;
        dbContext.SmsLog.Add(new SmsLog
        {
          SmsSid = messageId,
          Phone = phoneNumber,
          SentDate = utcNow,
          ElectionGuid = openElectionGuid,
          PersonGuid = personGuid,
          LastDate = utcNow,
          LastStatus = "WhatsApp sent"
        });
        dbContext.SaveChanges();

        errorMessage = null;
        return true;
      }
      catch (HttpRequestException ex)
      {
        errorMessage = $"GreenAPI connection error: {ex.Message}";
        return false;
      }
      catch (Exception ex)
      {
        errorMessage = $"GreenAPI error: {ex.GetBaseException().Message}";
        return false;
      }
    }

    private string FormatPhoneNumberForGreenApi(string phoneNumber)
    {
      var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
      return $"{digits}@c.us";
    }

    private static bool IsValidPhoneNumber(string phoneNumber)
    {
      if (phoneNumber.HasNoContent()) return false;

      var digits = phoneNumber.Where(char.IsDigit).ToArray();
      return digits.Length >= 4 && digits.Length <= 15;
    }

    private string TryParseErrorResponse(string responseContent)
    {
      try
      {
        var errorObj = JsonConvert.DeserializeObject<GreenApiErrorResponse>(responseContent);
        if (errorObj?.error.HasContent() == true)
        {
          return $"{errorObj.error} (Code: {errorObj.code})";
        }
      }
      catch
      {
        // Ignore parsing errors
      }

      return responseContent;
    }

    private string GetWhatsAppTemplate(string templateName)
    {
      var path = $"{AppDomain.CurrentDomain.BaseDirectory}/MessageTemplates/Sms/{templateName}.txt";

      if (!System.IO.File.Exists(path))
      {
        throw new InvalidOperationException($"Missing WhatsApp template: {templateName}");
      }

      return System.IO.File.ReadAllText(path);
    }

    public async Task<WhatsAppCheckResult> CheckWhatsAppAsync(string phoneNumber)
    {
      var idInstance = SettingsHelper.Get("greenapi-IdInstance", "");
      var apiToken = SettingsHelper.Get("greenapi-ApiTokenInstance", "");
      var apiUrl = SettingsHelper.Get("greenapi-ApiUrl", "https://api.green-api.com");

      if (idInstance.HasNoContent() || apiToken.HasNoContent())
      {
        return new WhatsAppCheckResult("Server not configured for WhatsApp (GreenAPI).");
      }

      if (!IsValidPhoneNumber(phoneNumber))
      {
        return new WhatsAppCheckResult("Invalid phone number: " + phoneNumber);
      }

      var digits = new string(phoneNumber.Where(char.IsDigit).ToArray());
      var url = $"{apiUrl}/waInstance{idInstance}/checkWhatsapp/{apiToken}";

      var requestBody = new
      {
        phoneNumber = long.Parse(digits)
      };

      var jsonContent = JsonConvert.SerializeObject(requestBody);
      var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

      try
      {
        var response = await _httpClient.PostAsync(url, content);
        var responseContent = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
        {
          var errorResponse = TryParseErrorResponse(responseContent);
          return new WhatsAppCheckResult($"GreenAPI CheckWhatsApp Error: {errorResponse}");
        }

        var responseObj = JsonConvert.DeserializeObject<GreenApiCheckWhatsAppResponse>(responseContent);
        var existsWhatsapp = responseObj?.existsWhatsapp ?? false;

        return new WhatsAppCheckResult(existsWhatsapp);
      }
      catch (HttpRequestException ex)
      {
        return new WhatsAppCheckResult($"GreenAPI connection error: {ex.Message}");
      }
      catch (Exception ex)
      {
        return new WhatsAppCheckResult($"GreenAPI error: {ex.GetBaseException().Message}");
      }
    }

    public async Task<CheckMultipleResults> CheckMultipleWhatsAppAsync(List<Person> people)
    {
      var errors = new List<string>();
      var results = new Dictionary<int, bool>();

      // only check those not previously checked. 
      // if someone adds WhatsApp to their phone later, we won't know...
      foreach (var person in people.Where(p => !p.HasWhatsApp.HasValue && p.Phone.HasContent()))
      {
        var phoneNumber = person.Phone;
        var result = await CheckWhatsAppAsync(phoneNumber);

        if (result.errorMessage != null)
        {
          errors.Add($"{phoneNumber}: {result.errorMessage}");
          // don't record yes or no
        }
        else
        {
          person.HasWhatsApp = result.HasWhatsApp;
          results.Add(person.C_RowId, result.HasWhatsApp);
        }
      }

      return new CheckMultipleResults { errorMessages = errors, personIdToHasWhatsApp = results };
    }

    private class GreenApiSendMessageResponse
    {
      public string idMessage { get; set; }
    }

    private class GreenApiCheckWhatsAppResponse
    {
      public bool existsWhatsapp { get; set; }
    }

    private class GreenApiErrorResponse
    {
      public string error { get; set; }
      public int code { get; set; }
    }
  }
  public class WhatsAppCheckResult
  {
    public WhatsAppCheckResult(string errorMessage)
    {
      this.errorMessage = errorMessage;
    }
    public WhatsAppCheckResult(bool hasWhatsApp)
    {
      HasWhatsApp = hasWhatsApp;
    }

    public bool HasWhatsApp { get; set; }
    public string errorMessage { get; set; }
  }

  public class CheckMultipleResults
  {
    public List<String> errorMessages { get; set; }
    public Dictionary<int, bool> personIdToHasWhatsApp { get; set; }
  }
}
