using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class ElectionModel : DataConnectedModel
  {
    public ElectionRules GetRules(string type, string mode)
    {
      var rules = new ElectionRules
                    {
                      Num = 0,
                      Extra = 0,
                      CanVote = "",
                      CanReceive = "",
                      IsSingleNameElection = false
                    };


      switch (type)
      {
        case "LSA":
          rules.CanVote = "A";
          rules.CanVoteLocked = true;

          rules.Extra = 0;
          rules.ExtraLocked = true;

          switch (mode)
          {
            case "N":
              rules.Num = 9;
              rules.NumLocked = true;
              rules.CanReceive = "A";
              break;
            case "T":
              rules.Num = 1;
              rules.NumLocked = false;
              rules.CanReceive = "N";
              break;
            case "B":
              rules.Num = 1;
              rules.NumLocked = false;
              rules.CanReceive = "A";
              break;
          }
          rules.CanReceiveLocked = true;

          break;

        case "NSA":
          rules.CanVote = "N"; // delegates
          rules.CanVoteLocked = true;

          rules.Extra = 0;
          rules.ExtraLocked = true;

          switch (mode)
          {
            case "N":
              rules.Num = 9;
              rules.NumLocked = true;
              rules.CanReceive = "A";
              break;
            case "T":
              rules.Num = 1;
              rules.NumLocked = false;
              rules.CanReceive = "N";
              break;
            case "B":
              rules.Num = 1;
              rules.NumLocked = false;
              rules.CanReceive = "A";
              break;
          }

          rules.CanReceiveLocked = true;

          break;

        case "Con":
          rules.CanVote = "A";
          rules.CanVoteLocked = true;

          switch (mode)
          {
            case "N":
              rules.Num = 5;
              rules.NumLocked = false;

              rules.Extra = 3;
              rules.ExtraLocked = false;

              rules.CanReceive = "A";
              break;

            case "T":
              rules.Num = 1;
              rules.NumLocked = false;

              rules.Extra = 0;
              rules.ExtraLocked = true;

              rules.CanReceive = "N";
              break;

            case "B":
              throw new ApplicationException("Unit Conventions cannot have by-elections");
          }
          rules.CanReceiveLocked = true;
          break;

        case "Reg":
          rules.CanVote = "N"; // LSA members
          rules.CanVoteLocked = false;

          switch (mode)
          {
            case "N":
              rules.Num = 9;
              rules.NumLocked = false;

              rules.Extra = 3;
              rules.ExtraLocked = false;

              rules.CanReceive = "A";
              break;

            case "T":
              rules.Num = 1;
              rules.NumLocked = false;

              rules.Extra = 0;
              rules.ExtraLocked = true;

              rules.CanReceive = "N";
              break;

            case "B":
              // Regional Councils often do not have by-elections, but some countries may allow it?

              rules.Num = 1;
              rules.NumLocked = false;

              rules.Extra = 0;
              rules.ExtraLocked = true;

              rules.CanReceive = "A";
              break;
          }
          rules.CanReceiveLocked = true;
          break;

        case "Oth":
          rules.CanVote = "A";

          rules.CanVoteLocked = false;
          rules.CanReceiveLocked = false;
          rules.NumLocked = false;
          rules.ExtraLocked = false;

          switch (mode)
          {
            case "N":
              rules.Num = 9;
              rules.Extra = 0;
              rules.CanReceive = "A";
              break;

            case "T":
              rules.Num = 1;
              rules.Extra = 0;
              rules.CanReceive = "N";
              break;

            case "B":
              rules.Num = 1;
              rules.Extra = 0;
              rules.CanReceive = "A";
              break;
          }
          break;
      }

      return rules;
    }


    /// <Summary>Saves changes to this electoin</Summary>
    public JsonResult SaveElection(Election electionFromBrowser)
    {
      var election = Db.Elections.SingleOrDefault(e => e.C_RowId == electionFromBrowser.C_RowId);
      if (election != null)
      {
        // List of fields to allow edit from setup page
        var editableFields = new
                               {
                                 election.Name,
                                 election.DateOfElection,
                                 election.Convenor,
                                 election.ElectionType,
                                 election.ElectionMode,
                                 election.NumberToElect,
                                 election.NumberExtra,
                                 election.CanVote,
                                 election.CanReceive,
                                 election.ListForPublic,
                                 election.ElectionPasscode
                               }.GetAllPropertyInfos().Select(pi => pi.Name).ToArray();


        var changed = electionFromBrowser.CopyPropertyValuesTo(election, editableFields);

        var isSingleNameElection = election.NumberToElect.AsInt() == 1;
        if (election.IsSingleNameElection != isSingleNameElection)
        {
          election.IsSingleNameElection = isSingleNameElection;
          changed = true;
        }

        if (changed)
        {
          Db.SaveChanges();
          UserSession.CurrentElection = election;
        }

        return new
                 {
                   Status = "Saved",
                   // TODO 2011-11-20 Glen Little: Return entire election?
                   Election = election
                 }.AsJsonResult();
      }

      return new
               {
                 Status = "Unknown ID"
               }.AsJsonResult();
    }


    public bool JoinIntoElection(Guid wantedElectionGuid)
    {
      var election = Db.Elections.SingleOrDefault(e => e.ElectionGuid == wantedElectionGuid);
      if (election == null)
      {
        return false;
      }

      UserSession.CurrentElection = election;

      new ComputerModel().AddCurrentComputerIntoElection(election.ElectionGuid);

      return true;
    }

    public JsonResult Copy(Guid guidOfElectionToCopy)
    {
      if (UserSession.IsGuestTeller)
      {
        return new
                 {
                   Success = false,
                   Message = "Not authorized"
                 }.AsJsonResult();
      }

      var election = Db.Elections.SingleOrDefault(e => e.ElectionGuid == guidOfElectionToCopy);
      if (election == null)
      {
        return new
                 {
                   Success = false,
                   Message = "Not found"
                 }.AsJsonResult();
      }

      // copy in SQL
      var result = Db.CloneElection(election.ElectionGuid, UserSession.LoginId).SingleOrDefault();
      if (result == null)
      {
        return new
                 {
                   Success = false,
                   Message = "Unable to copy"
                 }.AsJsonResult();
      }
      if (!result.Success.AsBool())
      {
        return new
                 {
                   Success = false,
                   Message = "Sorry: " + result.Message
                 }.AsJsonResult();
      }
      election = Db.Elections.SingleOrDefault(e => e.ElectionGuid == result.NewElectionGuid);
      if (election == null)
      {
        return new
                 {
                   Success = false,
                   Message = "New election not found"
                 }.AsJsonResult();
      }
      UserSession.CurrentElection = election;
      return new
               {
                 Success = true,
                 election.ElectionGuid
               }.AsJsonResult();
    }

    public JsonResult Create()
    {
      if (UserSession.IsGuestTeller)
      {
        return new
                 {
                   Success = false,
                   Message = "Not authorized"
                 }.AsJsonResult();
      }

      // create an election for this ID
      // create a default Location
      // assign all of these to this person and computer

      var election = new Election
                       {
                         Convenor = "[Convenor]",
                         ElectionGuid = Guid.NewGuid(),
                         Name = "[New Election]",
                         ElectionType = "LSA",
                         ElectionMode = "N",
                         NumberToElect = 9,
                         NumberExtra = 0,
                         CanVote = "A",
                         CanReceive = "A"
                       };
      Db.Elections.Add(election);
      Db.SaveChanges();


      var join = new JoinElectionUser
                   {
                     ElectionGuid = election.ElectionGuid,
                     UserId = UserSession.UserGuid
                   };
      Db.JoinElectionUsers.Add(join);


      var mainLocation = new Location
                           {
                             Name = "Main Location",
                             LocationGuid = Guid.NewGuid(),
                             ElectionGuid = election.ElectionGuid,
                             SortOrder = 1
                           };
      Db.Locations.Add(mainLocation);

      var mailedInLocation = new Location
                               {
                                 Name = "Mailed In Ballots",
                                 LocationGuid = Guid.NewGuid(),
                                 ElectionGuid = election.ElectionGuid,
                                 SortOrder = 99
                               };
      Db.Locations.Add(mailedInLocation);
      Db.SaveChanges();

      UserSession.CurrentElection = election;

      var computerModel = new ComputerModel();
      computerModel.AddCurrentComputerIntoElection(election.ElectionGuid);
      computerModel.AddCurrentComputerIntoLocation(mainLocation.C_RowId);

      return new
               {
                 Success = true
               }.AsJsonResult();
    }

    public void SetTallyStatus(string status)
    {
      if (UserSession.IsGuestTeller)
      {
        return;
      }

      var election = UserSession.CurrentElection;
      if (election.TallyStatus != status)
      {
        Db.Elections.Attach(election);

        election.TallyStatus = status;

        Db.SaveChanges();
      }
    }

    public IEnumerable<Election> VisibleElections()
    {
      return Db.Elections
        //.Where(e => e.ElectionPasscode != null && e.ElectionPasscode != "")
        .ToList()
        .Where(e => e.ListForPublic.AsBool() && DateTime.Now - e.ListedForPublicAsOf <= 5.minutes());
    }

    public JsonResult SetTallyStatusJson(string status)
    {
      SetTallyStatus(status);

      return new
               {
                 Saved = true
               }.AsJsonResult();
    }

    public JsonResult UpdateElectionShowAllJson(bool showFullReport)
    {
      var election = UserSession.CurrentElection;
      if (election.ShowFullReport != showFullReport)
      {
        Db.Elections.Attach(election);

        election.ShowFullReport = showFullReport;

        Db.SaveChanges();
      }

      return new { Saved = true }.AsJsonResult();
    }

    public JsonResult UpdateListOnPageJson(bool listOnPage)
    {
      var election = UserSession.CurrentElection;
      if (election.ListForPublic != listOnPage && UserSession.IsKnownTeller)
      {
        Db.Elections.Attach(election);

        election.ListForPublic = listOnPage;
        election.ListedForPublicAsOf = DateTime.Now;

        Db.SaveChanges();
        return new { Saved = true }.AsJsonResult();
      }

      return new { Saved = false }.AsJsonResult();
    }

    public void ProcessPulse()
    {
      var election = UserSession.CurrentElection;

      if (election == null) return;

      if (election.ListForPublic.AsBool() && UserSession.IsKnownTeller)
      {
        Db.Elections.Attach(election);
        election.ListedForPublicAsOf = DateTime.Now;
        Db.SaveChanges();
      }
    }
  }
}