using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Mvc;
using Newtonsoft.Json;
using TallyJ.Code;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.EF;
using static TallyJ.CoreModels.ImportBallotsModel;

namespace TallyJ.CoreModels.Helper
{
  public class WhatsAppGreenApiHelper : MessageHelperBase
  {
    private static readonly HttpClient _httpClient = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };

    // Registry of active background send queues, keyed by a token that is returned to the caller.
    // The caller can use that token to abort an in-progress queue via AbortQueue.
    private static readonly ConcurrentDictionary<string, CancellationTokenSource> _activeQueues
      = new ConcurrentDictionary<string, CancellationTokenSource>();

    /// <summary>
    /// Abort a previously-started queue. Returns true if a matching queue was found and signaled.
    /// </summary>
    public static bool AbortQueue(string queueToken)
    {
      if (queueToken.HasNoContent()) return false;

      if (_activeQueues.TryRemove(queueToken, out var cts))
      {
        try { cts.Cancel(); } catch { /* ignore */ }
        return true;
      }
      return false;
    }

    public bool SendVerifyCodeToVoter(string phone, string code, string hubKey, Guid electionGuid, Guid personGuid, out string error)
    {
      var text = GetWhatsAppTemplate("VerifyCodeSms").FilledWithObject(new
      {
        newCode = code
      });

      return SendWhatsAppMessage(UserSession.GetNewDbContext, phone, text, personGuid, out error, electionGuid, "WhatsApp - Login");
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
        .Where(p => IsValidPhoneNumber(p.Phone))
        .Select(p => new
        {
          p.Phone,
          PersonName = p.C_FullNameFL,
          p.FirstName,
          VoterContact = p.Phone,
          p.PersonGuid,
        })
        .ToList();

      var numToSend = phoneNumbersToSendTo.Count;

      LogHelper.Add($"WhatsApp: Queuing sends to {numToSend} {numToSend.Plural("people", "person")}", true);

      // Build the queue
      var queue = new Queue<dynamic>(phoneNumbersToSendTo);

      // Register a cancellation token for this queue so the caller can abort it.
      var queueToken = Guid.NewGuid().ToString("N");
      var cts = new CancellationTokenSource();
      _activeQueues[queueToken] = cts;
      var cancellationToken = cts.Token;

      var electionGuid = election.ElectionGuid;
      var electionName = election.Name;

      // Start background task to process the queue
      Task.Run(async () =>
      {
        var batchStart = DateTime.Now;
        var errors = new List<string>();
        var numSent = 0;
        var rand = new Random();
        var aborted = false;
        string errorMessage;

        // Create a dedicated DbContext for the background task. The normal
        // UserSession.GetNewDbContext path cannot be used here because Unity's
        // IDbContextFactory registration is PerWebRequest and there is no
        // HTTP request on this thread.
        var bgDb = new DbContextFactory().GetNewDbContext;

        try
        {
          while (queue.Count > 0)
          {
            if (cancellationToken.IsCancellationRequested)
            {
              aborted = true;
              break;
            }

            var p = queue.Dequeue();
            var phoneNumber = (string)p.Phone;


            var messageText = text.FilledWithObject(new
            {
              hostSite,
              p.PersonName,
              p.FirstName,
              p.VoterContact,
            });

            var ok = SendWhatsAppMessage(bgDb, phoneNumber, messageText, p.PersonGuid, out errorMessage, electionGuid, "WhatsApp - message sent");

            if (ok)
              numSent++;
            else
              errors.Add(errorMessage);


            if (queue.Count == 0) break;

            // Wait 3-15 seconds before next send (cancellable)
            int delay = rand.Next(3, 16) * 1000;
            try
            {
              await Task.Delay(delay, cancellationToken);
            }
            catch (TaskCanceledException)
            {
              aborted = true;
              break;
            }
          }
        }
        catch (Exception ex)
        {
          errors.Add("Queue error: " + ex.GetBaseException().Message);
        }
        finally
        {
          // Remove from registry if still present (it may have been removed by AbortQueue).
          _activeQueues.TryRemove(queueToken, out _);
          cts.Dispose();
        }

        var timeTaken = DateTime.Now - batchStart;
        var timeTakenDisplay = $"Time: {(int)timeTaken.TotalMinutes}:{timeTaken.Seconds:D2}";
        var remaining = queue.Count;
        var msg2 = aborted
          ? $"WhatsApp: Aborted. Sent to {numSent} {numSent.Plural("people", "person")}. {remaining} not sent. {timeTakenDisplay}"
          : $"WhatsApp: Sent to {numSent} {numSent.Plural("people", "person")}. {timeTakenDisplay}";
        if (errors.Count > 0) msg2 += $" - {errors.Count} failed to send. First error: {errors[0]}";

        // We're on a background thread with no HTTP request, so the normal
        // LogHelper path (which resolves a PerWebRequest DbContext via Unity)
        // cannot write to the database. Log directly using our background context.
        try
        {
          bgDb.C_Log.Add(new C_Log
          {
            ElectionGuid = electionGuid,
            Details = msg2,
            AsOf = DateTime.UtcNow,
            HostAndVersion = $"{Environment.MachineName} / WhatsApp queue"
          });
          bgDb.SaveChanges();
          LogHelper.SendToRemoteLog(msg2, true, electionName);
        }
        catch
        {
          // swallow — the send queue itself already completed
        }
        finally
        {
          try { bgDb.Dispose(); } catch { /* ignore */ }
        }
      });

      return new
      {
        Success = true,
        QueueToken = queueToken,
        Status = $"WhatsApp: Queued to send to {numToSend} {numToSend.Plural("people", "person")} over the next few minutes."
      }.AsJsonResult();
    }

    private bool SendWhatsAppMessage(ITallyJDbContext dbContext, string phoneNumber, string message, Guid personGuid, out string errorMessage, Guid openElectionGuid, string logMsg)
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

      var rand = new Random();
      var chatId = FormatPhoneNumberForGreenApi(phoneNumber);
      var url = $"{apiUrl}/waInstance{idInstance}/sendMessage/{apiToken}";

      var requestBody = new
      {
        chatId,
        message,
        typingTime = rand.Next(1100, 3200) // simulate typing for better delivery (GreenAPI recommends at least 500ms)
      };

      var jsonContent = JsonConvert.SerializeObject(requestBody);
      var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

      try
      {
        var response = _httpClient.PostAsync(url, content).ConfigureAwait(false).GetAwaiter().GetResult();
        var responseContent = response.Content.ReadAsStringAsync().ConfigureAwait(false).GetAwaiter().GetResult();

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

        var utcNow = DateTime.UtcNow;
        dbContext.SmsLog.Add(new SmsLog
        {
          SmsSid = messageId,
          Phone = phoneNumber,
          SentDate = utcNow,
          ElectionGuid = openElectionGuid,
          PersonGuid = personGuid,
          LastDate = utcNow,
          LastStatus = logMsg
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
      if (!long.TryParse(digits, out var numericPhoneNumber))
      {
        return new WhatsAppCheckResult("Invalid phone number: " + phoneNumber);
      }
      var url = $"{apiUrl}/waInstance{idInstance}/checkWhatsapp/{apiToken}";

      var requestBody = new
      {
        phoneNumber = numericPhoneNumber,
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
