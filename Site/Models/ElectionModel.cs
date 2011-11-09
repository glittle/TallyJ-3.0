using System;
using System.Linq;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.EF;

namespace TallyJ.Models
{
  public class ElectionRules
  {
    public bool CanVoteLocked { get; set; }
    public bool CanReceiveLocked { get; set; }
    public bool NumLocked { get; set; }
    public bool ExtraLocked { get; set; }

    /// <summary>
    /// Can Vote/Receive - All or Named people
    /// </summary>
    public string CanVote { get; set; }

    public string CanReceive { get; set; }
    public int Num { get; set; }
    public int Extra { get; set; }
  };

  public class ElectionModel : BaseViewModel
  {
    readonly string[] _editableFields = new[]
                                        {
                                          "Name",
                                          "DateOfElection",
                                          "Convenor",
                                          "ElectionType",
                                          "ElectionMode",
                                          "NumberToElect",
                                          "NumberExtra",
                                          "CanVote",
                                          "CanReceive",
                                        };

    public ElectionRules GetRules(string type, string mode)
    {
      var rules = new ElectionRules
                    {
                      Num = 0,
                      Extra = 0,
                      CanVote = "",
                      CanReceive = ""
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
              throw new ApplicationException("Unit Conventions cannot have bi-elections");
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
              // Regional Councils often do not have bi-elections, but some countries may allow it?

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


    public JsonResult SaveElection(Election election)
    {

      var onFile = Db.Elections.Where(e => e.C_RowId == election.C_RowId).SingleOrDefault();
      if (onFile != null)
      {
        // apply changes
        if (election.CopyPropertyValuesTo(onFile, _editableFields))
        {
          Db.SaveChanges();
        }

        return new
        {
          Status = "Saved",
          Election = onFile
        }.AsJsonResult();
      }

      return new
      {
        Status = "Unkown ID"
      }.AsJsonResult();
    }

  }
}