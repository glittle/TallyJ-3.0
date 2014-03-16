using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Helpers;
using TallyJ.CoreModels.Helper;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
  public class ImportV1Election : ImportV1Base
  {
    private readonly Election _election;
    private readonly Location _location;
    private readonly Action<Ballot> _storeBallot;
    private readonly Action<ResultSummary> _storeResultSummary;
    private readonly Action<Vote> _storeVote;
    private int _ballotsLoaded;

    public ImportV1Election(TallyJ2dEntities db, ImportFile file, XmlDocument xml,
      Election election,
      Location location,
      Action<Ballot> storeBallot,
      Action<Vote> storeVote,
      List<Person> people, Action<Person> addPerson,
      Action<ResultSummary> storeResultSummary,
      ILogHelper logHelper
      )
      : base(db, file, xml, people, addPerson, logHelper)
    {
      _election = election;
      _location = location;
      _storeBallot = storeBallot;
      _storeVote = storeVote;
      _storeResultSummary = storeResultSummary;
    }

    public override void Process()
    {
      /*
             * Approach... 
             *  - Community attributes
             *  - ResultSummary (manual)
             *  - Ballots
             *    - Votes
             *      - Persons  (add if not already loaded)
             * 
             * Load all data, but don't use .SaveChanges until the end (will EF do them in the correct order?)
             */

      ImportCommunityInfo();
      ImportManualSummary();
      ImportBallots();

      _file.ProcessingStatus = "Imported";

      _db.SaveChanges();

      ImportSummaryMessage = "Imported {0} ballot{1}.".FilledWith(_ballotsLoaded, _ballotsLoaded.Plural());

      new CacherHelper().DropAllCachesForThisElection();

      _logHelper.Add("Imported v1 election file #" + _file.C_RowId + ": " + ImportSummaryMessage);
    }

    private void ImportCommunityInfo()
    {
      _election.ShowAsTest = true; // default to show as a test election
      _election.TallyStatus = ElectionTallyStatusEnum.Reviewing; // default to reviewing


      var commXml = _xmlDoc.DocumentElement;
      if (commXml == null)
      {
        throw new ApplicationException("No root element");
      }

      var name = commXml.GetAttribute("Name");
      if (name.HasContent())
      {
        _election.Name = "(Imported) " + name;
      }

      var infoXml = commXml.SelectSingleNode("Info") as XmlElement;
      if (infoXml == null)
      {
        throw new ApplicationException("No Info element");
      }

      //* ElectionType="UnitConvention" 
      /*
                  <xsd:enumeration value="Assembly"/>
                  <xsd:enumeration value="UnitConvention"/>
                  <xsd:enumeration value="AssemblyBiElection"/>
                  <xsd:enumeration value="AssemblyTieBreak"/>
                  <xsd:enumeration value="UnitConventionTieBreak"/>
             */
      var electionType = infoXml.GetAttribute("ElectionType");
      switch (electionType)
      {
        case "UnitConvention":
          _election.ElectionType = ElectionTypeEnum.Con;
          _election.ElectionMode = ElectionModeEnum.Normal;
          break;

        case "AssemblyBiElection":
          _election.ElectionType = ElectionTypeEnum.Lsa;
          _election.ElectionMode = ElectionModeEnum.ByElection;
          break;

        case "AssemblyTieBreak":
          _election.ElectionType = ElectionTypeEnum.Lsa;
          _election.ElectionMode = ElectionModeEnum.Tie;
          break;

        case "UnitConventionTieBreak":
          _election.ElectionType = ElectionTypeEnum.Con;
          _election.ElectionMode = ElectionModeEnum.Tie;
          break;

        case "Assembly":
        default:
          _election.ElectionType = ElectionTypeEnum.Lsa;
          _election.ElectionMode = ElectionModeEnum.Normal;
          break;
      }

      //* DateOfElection="Jan 30, 2011" 
      var date = infoXml.GetAttribute("DateOfElection");
      if (date.HasContent())
      {
        // try to convert... ignore if not possible
        DateTime dateTime;
        if (DateTime.TryParse(date, out dateTime))
        {
          _election.DateOfElection = dateTime;
        }
      }

      //* ElectoralUnit
      //-->not imported

      //* Location="Calgary, Alberta" 
      var location = infoXml.GetAttribute("Location");
      if (location.HasContent())
      {
        _election.Convenor = location;
      }

      //* ChiefTeller="Arman Imadi" 
      //-->not imported

      //* OtherTellers
      //-->not imported

      //* NumberToElect="5" 
      var numberToElect = infoXml.GetAttribute("NumberToElect").AsInt(-1);
      if (numberToElect != -1)
      {
        _election.NumberToElect = numberToElect;
      }

      //* NumberOfAlternatesToReport="3" 
      var numberExtra = infoXml.GetAttribute("NumberOfAlternatesToReport").AsInt(-1);
      if (numberExtra != -1)
      {
        _election.NumberExtra = numberExtra;
      }

      //* UseManualCounts="true" 
      //-->not imported

      //* AllowAddNewInBallot="false"
      //-->not imported

      //* ApprovedForReporting="true" 
      //-->not imported

      //* CommunityFileName="C:/Users/Glen/Bahai/TallyJ/v1/Data/Calgary Jan 2010.xml" 
      //-->not imported

      //* CommunityFileTime="Mon Apr 11 22:14:00 MDT 2011" 
      //-->not imported

      //* CodeForThisComputer="A" 
      //-->not imported

      //* TellersAtThisComputer
      //-->not imported
    }

    private void ImportManualSummary()
    {
      var manualCountsXml = _xmlDoc.SelectSingleNode("//ManualResults") as XmlElement;
      if (manualCountsXml == null) return;

      // * OtherTellers="(Add other tellers here)" 
      //-->not imported

      // * AdultsInCommunity="538" 

      // * MailedInBallots="10" 

      // * DroppedOffBallots="43" 

      // * VotedInPerson="84"

      var results = new ResultSummary
      {
        ElectionGuid = _election.ElectionGuid,
        ResultType = ResultType.Manual,
        NumEligibleToVote = manualCountsXml.GetAttribute("AdultsInCommunity").AsInt(),
        MailedInBallots = manualCountsXml.GetAttribute("MailedInBallots").AsInt(),
        CalledInBallots = 0,
        DroppedOffBallots = manualCountsXml.GetAttribute("DroppedOffBallots").AsInt(),
        InPersonBallots = manualCountsXml.GetAttribute("VotedInPerson").AsInt()
      };


      _storeResultSummary(results);
    }

    private void ImportBallots()
    {
      var ballotsXml = _xmlDoc.SelectNodes("//Ballot");

      if (ballotsXml == null || ballotsXml.Count == 0)
      {
        return;
      }

      for (int i = 0, max = ballotsXml.Count; i < max; i++)
      {
        var ballotXml = ballotsXml[i] as XmlElement;

        var ballotId = ballotXml.GetAttribute("Id");
        var ballotCodeInfo = ballotId.Split(new[] {'.'});

        AssertAtRuntime.That(ballotCodeInfo.Length == 2, "Invalid ballot Id: " + ballotId);

        var ballot = new Ballot
        {
          LocationGuid = _location.LocationGuid,
          BallotGuid = Guid.NewGuid(),
          ComputerCode = ballotCodeInfo[0],
          BallotNumAtComputer = ballotCodeInfo[1].AsInt()
        };

        // ignore automatic Ballot status, let this program determine actual status

        //var status = ballotXml.GetAttribute("BallotStatus");
        var overrideStatus = ballotXml.GetAttribute("OverrideStatus");
        switch (overrideStatus)
        {
          case "TooMany":
            ballot.StatusCode = BallotStatusEnum.TooMany;
            break;

          case "ReviewNeeded":
            ballot.StatusCode = BallotStatusEnum.Review;
            break;

          case "Normal":
          default:
            ballot.StatusCode = BallotStatusEnum.Ok;
            break;
        }

        _ballotsLoaded++;
        _storeBallot(ballot);

        ImportVotes(ballot, ballotXml.SelectNodes("Vote"));
      }
    }

    private void ImportVotes(Ballot ballot, XmlNodeList votesXml)
    {
      var voteNum = 0;
      foreach (XmlElement voteXml in votesXml)
      {
        var voteStatus = voteXml.GetAttribute("VoteStatus");
        switch (voteStatus)
        {
          case "Spoiled": // 1.80
          case "Ineligible": // 1.71
          case "UnReadable": // 1.71

            var spoiledGroup = voteXml.GetAttribute("SpoiledGroup").DefaultTo(voteStatus);
            var spoiledDetail = voteXml.GetAttribute("SpoiledDetail");

            var ineligibleReasonGuid = MapIneligible(spoiledGroup, spoiledDetail);

            var vote = new Vote
            {
              BallotGuid = ballot.BallotGuid,
              PositionOnBallot = ++voteNum,
              InvalidReasonGuid = ineligibleReasonGuid,
              StatusCode = VoteHelper.VoteStatusCode.Ok
            };

            _storeVote(vote);

            break;

          case "Ok":
          case "AddToList":
          case "New":
          default:
            ImportVotePerson(ballot, voteXml, ++voteNum);
            break;
        }
      }

      if (ballot.StatusCode == BallotStatusEnum.TooMany)
      {
        if (voteNum <= _election.NumberToElect)
        {
          // TallyJ v1 allowed user to choose "TooMany" and not enter too many votes...
          // TallyJ v2 want to see "too many" votes, so on import, add more to ensure status will be calculated as "TooMany"
          for (var i = _election.NumberToElect.AsInt() - voteNum; i >= 0; i--)
          {
            var vote = new Vote
            {
              BallotGuid = ballot.BallotGuid,
              PositionOnBallot = ++voteNum,
              InvalidReasonGuid = IneligibleReasonEnum.Ineligible_Other,
              StatusCode = VoteHelper.VoteStatusCode.Ok
            };

            _storeVote(vote);
          }
        }
      }
    }

    /// <Summary>Vote is not spoiled, so determine who this is...</Summary>
    private void ImportVotePerson(Ballot ballot, XmlElement voteXml, int voteNum)
    {
      var personXml = voteXml.SelectSingleNode("Person") as XmlElement;
      if (personXml == null)
      {
        // can't happen if XML is okay
        return;
      }

      Vote vote;

      var lastName = personXml.GetAttribute("LName");
      var firstName = personXml.GetAttribute("FName");
      var akaName = personXml.GetAttribute("AKAName");

      // check for matches
      Person person;
      var matchedPeople = _people.Where(p => p.LastName.DefaultTo("") == lastName
                                             && p.FirstName.DefaultTo("") == firstName
                                             && p.OtherNames.DefaultTo("") == akaName).ToList();
      var numMatched = matchedPeople.Count;
      switch (numMatched)
      {
        case 1:
          person = matchedPeople[0];
          break;

        case 0:
          // vote is valid, but person not found!
          // is okay if we are loading without a community file

          if (lastName.HasNoContent() && firstName.HasNoContent())
          {
            //            vote = new Vote
            //            {
            //              BallotGuid = ballot.BallotGuid,
            //              PositionOnBallot = voteNum,
            //              InvalidReasonGuid = IneligibleReasonEnum.Unreadable_Vote_is_blank,
            //              StatusCode = VoteHelper.VoteStatusCode.Ok
            //            };
            //
            //            _storeVote(vote);
            return;
          }
          else
          {
            person = new Person
            {
              PersonGuid = Guid.NewGuid(),
              ElectionGuid = _election.ElectionGuid,
              LastName = lastName,
              FirstName = firstName
            };

            AddPerson(person);
            _people.Add(person);

            if (akaName.HasContent())
            {
              person.OtherNames = akaName;
            }

            var bahaiId = personXml.GetAttribute("BahaiId");
            if (bahaiId.HasContent())
            {
              person.BahaiId = bahaiId;
            }

            var ineligible = personXml.GetAttribute("IneligibleToReceiveVotes").AsBoolean();
            person.CanReceiveVotes = ineligible;

            var ineligibleReason = personXml.GetAttribute("ReasonToNotReceive").AsBoolean();
            //TODO...
          }

          break;

        default:
          throw new ApplicationException("Name in Vote matches {0} {1}".FilledWith(numMatched,
            numMatched.Plural("person", "people")));
      }

      // got the person
      vote = new Vote
      {
        BallotGuid = ballot.BallotGuid,
        PositionOnBallot = voteNum,
        PersonCombinedInfo = person.CombinedInfo,
        PersonGuid = person.PersonGuid,
        StatusCode = VoteHelper.VoteStatusCode.Ok
      };

      _storeVote(vote);
    }
  }
}