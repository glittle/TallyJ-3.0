using System;
using System.Collections.Generic;
using System.Data.Entity.SqlServer;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text.RegularExpressions;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.Helper
{
  public class NotificationHelper 
  {

    public bool SendVoterTestMessage(out string error)
    {
      var voterIdType = UserSession.VoterIdType;

      if (voterIdType == VoterIdTypeEnum.Email)
      {
        var emailHelper = new EmailHelper();
        return emailHelper.SendVoterTestMessage(UserSession.VoterId, out error);
      } 
      
      if (voterIdType == VoterIdTypeEnum.Phone)
      {
        var smsHelper = new TwilioHelper();
        return smsHelper.SendVoterSmsTestMessage(UserSession.VoterId, out error);
      }

      error = "Invalid voter type: " + voterIdType; 
      return false;
    }

    public bool SendWhenBallotSubmitted(Person person, Election election, out string notificationType, out string error)
    {
      var voterIdType = UserSession.VoterIdType;

      if (voterIdType == VoterIdTypeEnum.Email)
      {
        var emailHelper = new EmailHelper();
        notificationType = "email";
        return emailHelper.SendWhenBallotSubmitted(person, election, out error);
      } 
      
      if (voterIdType == VoterIdTypeEnum.Phone)
      {
        var smsHelper = new TwilioHelper();
        notificationType = "text message";
        return smsHelper.SendWhenBallotSubmitted(person, election, out error);
      }

      error = "Invalid voter type: " + voterIdType;
      notificationType = null;
      return false;
    }

    //
    // public string SendWhenOpened(Election currentElection = null, bool automated = false)
    // {
    //   var db = UserSession.GetNewDbContext;
    //   var now = DateTime.Now;
    //   var hostSite = SettingsHelper.Get("HostSite", "");
    //   var electionGuid = currentElection?.ElectionGuid;
    //   var allElections = currentElection == null;
    //
    //   var electionList = db.Election
    //     // elections that are open but have not been announced
    //     .Where(e => allElections || e.ElectionGuid == electionGuid)
    //     .Where(e => e.OnlineWhenOpen < now
    //                 && e.OnlineAnnounced == null
    //                 && e.OnlineWhenClose > now)
    //     .GroupJoin(
    //       // get list of voters who want to be notified by email when their election opens
    //       db.OnlineVotingInfo
    //         .Where(ovi => !ovi.NotifiedAboutOpening.Value)
    //         .Join(db.Person, ovi => ovi.PersonGuid, p => p.PersonGuid, (ovi, p) => new { ovi, p })
    //         .Join(db.OnlineVoter.Where(ov => ov.EmailCodes.Contains("o")), j => j.p.Email, ov => ov.VoterId, (j, ov) => new { j.ovi, emailOv = ov, j.p })
    //         .Join(db.OnlineVoter.Where(ov => ov.EmailCodes.Contains("o")), j => j.p.Phone, ov => ov.VoterId, (j, ov) => new { j.ovi, phoneOv = ov, j.emailOv, j.p })
    //       , e => e.ElectionGuid, jv => jv.ovi.ElectionGuid, (election, oviList) => new { election, oviList })
    //     .Select(g => new
    //     {
    //       g.election,
    //       peopleEmail = g.oviList.Where(jOvi => jOvi.emailOv != null).Select(jOvi => new
    //       {
    //         jOvi.p.Email,
    //         jOvi.p.C_FullNameFL,
    //         jOvi.ovi
    //       }),
    //       peopleSms = g.oviList.Where(jOvi => jOvi.phoneOv != null).Select(jOvi => new
    //       {
    //         jOvi.p.Phone,
    //         jOvi.p.C_FullNameFL,
    //         jOvi.ovi
    //       })
    //
    //     })
    //     .ToList();
    //
    //   var allMsgs = new List<string>();
    //   var smsHelper = new SmsHelper();
    //
    //   electionList.ForEach(electionInfo =>
    //   {
    //     var election = electionInfo.election;
    //
    //     var electionName = election.Name;
    //     var electionType = ElectionTypeEnum.TextFor(election.ElectionType);
    //     //        var isEstimate = electionInfo.election.OnlineCloseIsEstimate;
    //     var whenClosed = election.OnlineWhenClose.GetValueOrDefault();
    //     var whenClosedUtc = whenClosed.ToUniversalTime();
    //     var remainingTime = whenClosed - now;
    //     var howLong = "";
    //     if (remainingTime.Days > 1)
    //     {
    //       howLong = remainingTime.Days + " days";
    //     }
    //     else
    //     {
    //       howLong = remainingTime.Hours + " hours";
    //     }
    //
    //     var numSent = 0;
    //     var errors = new List<string>();
    //
    //     electionInfo.peopleEmail.ToList().ForEach(voter =>
    //     {
    //       var html = GetEmailTemplate("ElectionOpen").FilledWithObject(new
    //       {
    //         hostSite,
    //         logo = hostSite + "/Images/LogoSideM.png",
    //         voter.Email,
    //         personName = voter.C_FullNameFL,
    //         electionName,
    //         electionType,
    //         whenClosedDay = whenClosedUtc.ToString("d MMM"),
    //         whenClosedTime = whenClosedUtc.ToString("h:mm tt"),
    //         howLong,
    //       });
    //
    //       var message = new MailMessage();
    //       message.To.Add(new MailAddress(voter.Email, voter.C_FullNameFL));
    //
    //       if (election.EmailFromAddress.HasContent())
    //       {
    //         message.From = new MailAddress(election.EmailFromAddressWithDefault, election.EmailFromNameWithDefault);
    //       }
    //
    //       var ok = SendEmail(message, html, out var emailError);
    //
    //       if (ok)
    //       {
    //         numSent++;
    //       }
    //       else
    //       {
    //         errors.Add(emailError);
    //       }
    //
    //       // regardless if the email went or not, record it
    //       voter.ovi.NotifiedAboutOpening = true;
    //     });
    //
    //     electionInfo.peopleSms.ToList().ForEach(voter =>
    //     {
    //       var html = GetEmailTemplate("SmsElectionOpen").FilledWithObject(new
    //       {
    //         hostSite,
    //         voter.Phone,
    //         personName = voter.C_FullNameFL,
    //         electionName,
    //         electionType,
    //         whenClosedDay = whenClosedUtc.ToString("d MMM"),
    //         whenClosedTime = whenClosedUtc.ToString("h:mm tt"),
    //         howLong,
    //       });
    //
    //       var ok = smsHelper.SendWhenOpened(voter.Phone, voter.C_FullNameFL, html, out var smsError);
    //
    //       if (ok)
    //       {
    //         numSent++;
    //       }
    //       else
    //       {
    //         errors.Add(smsError);
    //       }
    //
    //       // regardless if the email went or not, record it
    //       voter.ovi.NotifiedAboutOpening = true;
    //     });
    //
    //     var msg = $"Email: Election #{election.C_RowId} {(automated ? "automatically " : "")}announced to {numSent} {(numSent == 1 ? "person" : "people")}";
    //
    //     if (errors.Count > 0)
    //     {
    //       msg += $" {errors.Count} failed to send.";
    //     }
    //
    //
    //     election.OnlineAnnounced = now;
    //
    //     if (currentElection != null)
    //     {
    //       currentElection.OnlineAnnounced = now;
    //     }
    //
    //     db.SaveChanges();
    //
    //     allMsgs.Add(msg);
    //
    //     // after adding the message to allMsgs, add more details for the log
    //     if (errors.Count > 0)
    //     {
    //       msg += $" First error: {errors[0]}";
    //     }
    //
    //     LogHelper.Add(msg, true);
    //
    //   });
    //
    //   var numElections = electionList.Count;
    //
    //   // return the messages for the Azure logs (or any random anonymous caller)
    //
    //   if (numElections == 0)
    //   {
    //     return "Nothing at " + now;
    //   }
    //
    //   return allMsgs.JoinedAsString("\r\n");
    // }

  }
}