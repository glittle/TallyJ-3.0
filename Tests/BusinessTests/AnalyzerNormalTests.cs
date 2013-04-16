using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using TallyJ.Code.Enumerations;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Helper;
using TallyJ.Models;
using Tests.Support;

namespace Tests.BusinessTests
{
    [TestClass]
    public class AnalyzerNormalTests
    {
        private AnalyzerFakes _fakes;
        private List<Person> _persons;

        private List<Person> SamplePeople
        {
            get { return _persons; }
        }

        [TestInitialize]
        public void Init()
        {
            _fakes = new AnalyzerFakes();

            _persons = new List<Person>
        {
          new Person {VotingMethod = VotingMethodEnum.InPerson},
          new Person {},
          new Person {},
          new Person {},
          new Person {},
          new Person {IneligibleReasonGuid = IneligibleReasonEnum.Unidentifiable_Unknown_person},
        };
            _persons.ForEach(delegate(Person p)
              {
                  p.CanVote = true;
                  p.PersonGuid = Guid.NewGuid();
              });
        }


        [TestMethod]
        public void Ballot_TwoPeople()
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
              {
                  ElectionGuid = electionGuid,
                  NumberToElect = 2,
                  NumberExtra = 0,
                  CanReceive = ElectionModel.CanVoteOrReceive.All
              };

            var personGuid = Guid.NewGuid();

            var ballots = new List<Ballot>
        {
          new Ballot {BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok}
        };
            var votes = new List<vVoteInfo>
        {
          new vVoteInfo {PersonGuid = personGuid},
          new vVoteInfo {PersonGuid = Guid.NewGuid()},
        };
            foreach (var vVoteInfo in votes)
            {
                //vVoteInfo.PersonGuid = personGuid; // all for one person in this test
                vVoteInfo.ElectionGuid = electionGuid;
                vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
                vVoteInfo.BallotGuid = ballots.Select(b => b.BallotGuid).First();
                vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
                vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
            }

            var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

            model.AnalyzeEverything();

            var results = model.Results;

            results.Count.ShouldEqual(2);

            var result1 = results[0];
            result1.VoteCount.ShouldEqual(1);
            result1.Rank.ShouldEqual(1);
            result1.Section.ShouldEqual(ResultHelper.Section.Top);

            var result2 = results[1];
            result2.VoteCount.ShouldEqual(1);
            result2.Rank.ShouldEqual(2);
            result2.Section.ShouldEqual(ResultHelper.Section.Top);

            var resultSummaryFinal = model.ResultSummaryFinal;
            resultSummaryFinal.BallotsNeedingReview.ShouldEqual(0);
            resultSummaryFinal.NumBallotsEntered.ShouldEqual(1);

            resultSummaryFinal.EnvelopesDroppedOff.ShouldEqual(0);
            resultSummaryFinal.EnvelopesInPerson.ShouldEqual(1);
            resultSummaryFinal.EnvelopesMailedIn.ShouldEqual(0);
            resultSummaryFinal.EnvelopesCalledIn.ShouldEqual(0);
            resultSummaryFinal.NumEligibleToVote.ShouldEqual(5);
            resultSummaryFinal.NumVoters.ShouldEqual(1);
            resultSummaryFinal.ResultType.ShouldEqual(ResultType.Final);
        }

        [TestMethod]
        public void Ballot_TwoPeople_NameChanged()
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
              {
                  ElectionGuid = electionGuid,
                  NumberToElect = 2,
                  NumberExtra = 0,
                  CanReceive = ElectionModel.CanVoteOrReceive.All
              };

            var personGuid = Guid.NewGuid();

            var ballots = new List<Ballot>
        {
          new Ballot {BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok}
        };
            var vVoteInfos = new List<vVoteInfo>
        {
          new vVoteInfo {PersonGuid = personGuid},
          new vVoteInfo {PersonGuid = Guid.NewGuid()},
        };
            var rowId = 1;
            foreach (var vVoteInfo in vVoteInfos)
            {
                vVoteInfo.VoteId = rowId++;
                //vVoteInfo.PersonGuid = personGuid; // all for one person in this test
                vVoteInfo.ElectionGuid = electionGuid;
                vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
                vVoteInfo.BallotGuid = ballots.Select(b => b.BallotGuid).First();
                vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
                vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
            }

            vVoteInfos[0].PersonCombinedInfo = "yy";

            var model = new ElectionAnalyzerNormal(_fakes, election, vVoteInfos, ballots, SamplePeople);

            model.AnalyzeEverything();

            var results = model.Results;

            results.Count.ShouldEqual(0);

            var resultSummaryFinal = model.ResultSummaryFinal;
            resultSummaryFinal.BallotsNeedingReview.ShouldEqual(1);
            resultSummaryFinal.NumBallotsEntered.ShouldEqual(1);

            resultSummaryFinal.EnvelopesDroppedOff.ShouldEqual(0);
            resultSummaryFinal.EnvelopesInPerson.ShouldEqual(1);
            resultSummaryFinal.EnvelopesMailedIn.ShouldEqual(0);
            resultSummaryFinal.EnvelopesCalledIn.ShouldEqual(0);
            resultSummaryFinal.NumEligibleToVote.ShouldEqual(5);
            resultSummaryFinal.NumVoters.ShouldEqual(1);
            resultSummaryFinal.ResultType.ShouldEqual(ResultType.Final);
        }

        [TestMethod]
        public void Election_3_people()
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
              {
                  ElectionGuid = electionGuid,
                  NumberToElect = 1,
                  NumberExtra = 0
              };
            var location = new Location
              {
                  LocationGuid = Guid.NewGuid(),
                  ElectionGuid = electionGuid
              };

            var ballot1Guid = Guid.NewGuid();
            var ballot2Guid = Guid.NewGuid();
            var ballot3Guid = Guid.NewGuid();
            var ballots = new List<Ballot>
        {
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot1Guid, StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot2Guid, StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot3Guid, StatusCode = BallotStatusEnum.Ok}
        };

            var person1Guid = Guid.NewGuid();
            var votes = new List<vVoteInfo>
        {
          new vVoteInfo {SingleNameElectionCount = 33, PersonGuid = person1Guid, BallotGuid = ballot1Guid},
          new vVoteInfo {SingleNameElectionCount = 5, PersonGuid = person1Guid, BallotGuid = ballot2Guid},
          new vVoteInfo {SingleNameElectionCount = 2, PersonGuid = Guid.NewGuid(), BallotGuid = ballot3Guid},
        };
            foreach (var vVoteInfo in votes)
            {
                vVoteInfo.ElectionGuid = electionGuid;
                vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
                vVoteInfo.BallotStatusCode = ballots.Single(b => b.BallotGuid == vVoteInfo.BallotGuid).StatusCode;
                vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
            }

            var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

            model.AnalyzeEverything();

            var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

            results.Count.ShouldEqual(2);

            var result1 = results[0];
            result1.VoteCount.ShouldEqual(2);
            result1.Rank.ShouldEqual(1);
            result1.Section.ShouldEqual(ResultHelper.Section.Top);
            result1.IsTied.ShouldEqual(false);

            var result2 = results[1];
            result2.VoteCount.ShouldEqual(1);
            result2.Rank.ShouldEqual(2);
            result2.Section.ShouldEqual(ResultHelper.Section.Other);
            result2.IsTied.ShouldEqual(false);
        }

        [TestMethod]
        public void Election_3_people_With_Manual_Results()
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
              {
                  ElectionGuid = electionGuid,
                  NumberToElect = 1,
                  NumberExtra = 0
              };
            var location = new Location
              {
                  LocationGuid = Guid.NewGuid(),
                  ElectionGuid = electionGuid
              };

            var ballot1Guid = Guid.NewGuid();
            var ballot2Guid = Guid.NewGuid();
            var ballot3Guid = Guid.NewGuid();
            var ballots = new List<Ballot>
        {
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot1Guid, StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot2Guid, StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot3Guid, StatusCode = BallotStatusEnum.Ok}
        };

            var person1Guid = Guid.NewGuid();
            var votes = new List<vVoteInfo>
        {
          new vVoteInfo {SingleNameElectionCount = 33, PersonGuid = person1Guid, BallotGuid = ballot1Guid},
          new vVoteInfo {SingleNameElectionCount = 5, PersonGuid = person1Guid, BallotGuid = ballot2Guid},
          new vVoteInfo {SingleNameElectionCount = 2, PersonGuid = Guid.NewGuid(), BallotGuid = ballot3Guid},
        };
            foreach (var vVoteInfo in votes)
            {
                vVoteInfo.ElectionGuid = electionGuid;
                vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
                vVoteInfo.BallotStatusCode = ballots.Single(b => b.BallotGuid == vVoteInfo.BallotGuid).StatusCode;
                vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
            }
            _fakes.ResultSummaryManual = new ResultSummary
                {
                    ResultType = ResultType.Manual,
                    NumBallotsEntered = 4
                };

            var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

            model.AnalyzeEverything();


            var resultSummaryFinal = model.ResultSummaryFinal;
            resultSummaryFinal.BallotsNeedingReview.ShouldEqual(0);
            resultSummaryFinal.NumBallotsEntered.ShouldEqual(4);

            resultSummaryFinal.EnvelopesDroppedOff.ShouldEqual(0);
            resultSummaryFinal.EnvelopesInPerson.ShouldEqual(1);
            resultSummaryFinal.EnvelopesMailedIn.ShouldEqual(0);
            resultSummaryFinal.EnvelopesCalledIn.ShouldEqual(0);
            resultSummaryFinal.NumEligibleToVote.ShouldEqual(5);
            resultSummaryFinal.NumVoters.ShouldEqual(1);
            resultSummaryFinal.ResultType.ShouldEqual(ResultType.Final);


            var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

            results.Count.ShouldEqual(2);

            var result1 = results[0];
            result1.VoteCount.ShouldEqual(2);
            result1.Rank.ShouldEqual(1);
            result1.Section.ShouldEqual(ResultHelper.Section.Top);
            result1.IsTied.ShouldEqual(false);

            var result2 = results[1];
            result2.VoteCount.ShouldEqual(1);
            result2.Rank.ShouldEqual(2);
            result2.Section.ShouldEqual(ResultHelper.Section.Other);
            result2.IsTied.ShouldEqual(false);
        }

        [TestMethod]
        public void Election_3_people_with_Tie()
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
              {
                  ElectionGuid = electionGuid,
                  NumberToElect = 1,
                  NumberExtra = 0
              };
            var location = new Location
              {
                  LocationGuid = Guid.NewGuid(),
                  ElectionGuid = electionGuid
              };

            var ballot1Guid = Guid.NewGuid();
            var ballot2Guid = Guid.NewGuid();
            var ballot3Guid = Guid.NewGuid();
            var ballots = new List<Ballot>
        {
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot1Guid, StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot2Guid, StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot3Guid, StatusCode = BallotStatusEnum.Ok}
        };


            var votes = new List<vVoteInfo>
        {
          new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid = ballot1Guid},
          new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid = ballot2Guid},
          new vVoteInfo {SingleNameElectionCount = 2, PersonGuid = Guid.NewGuid(), BallotGuid = ballot3Guid},
        };
            foreach (var vVoteInfo in votes)
            {
                vVoteInfo.ElectionGuid = electionGuid;
                vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
                vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
                vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
            }

            var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

            model.AnalyzeEverything();

            var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
            var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

            resultTies.Count.ShouldEqual(1);
            resultTies[0].NumToElect.ShouldEqual(1);
            resultTies[0].NumInTie.ShouldEqual(3);
            resultTies[0].TieBreakRequired.ShouldEqual(true);

            results.Count.ShouldEqual(3);

            var result1 = results[0];
            result1.VoteCount.ShouldEqual(1);
            result1.Rank.ShouldEqual(1);
            result1.Section.ShouldEqual(ResultHelper.Section.Top);
            result1.IsTied.ShouldEqual(true);
            result1.TieBreakGroup.ShouldEqual(1);
            result1.TieBreakRequired.ShouldEqual(true);

            var result2 = results[1];
            result2.VoteCount.ShouldEqual(1);
            result2.Rank.ShouldEqual(2);
            result2.Section.ShouldEqual(ResultHelper.Section.Other);
            result2.IsTied.ShouldEqual(true);
            result2.TieBreakGroup.ShouldEqual(1);
            result2.ForceShowInOther.ShouldEqual(true);
            result2.TieBreakRequired.ShouldEqual(true);

            var result3 = results[2];
            result3.VoteCount.ShouldEqual(1);
            result3.Rank.ShouldEqual(3);
            result3.Section.ShouldEqual(ResultHelper.Section.Other);
            result3.IsTied.ShouldEqual(true);
            result3.TieBreakGroup.ShouldEqual(1);
            result3.ForceShowInOther.ShouldEqual(true);
            result3.TieBreakRequired.ShouldEqual(true);
        }

        [TestMethod]
        public void Election_3_people_with_Tie_Not_Required()
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
              {
                  ElectionGuid = electionGuid,
                  NumberToElect = 3,
                  NumberExtra = 0
              };
            var location = new Location
              {
                  LocationGuid = Guid.NewGuid(),
                  ElectionGuid = electionGuid
              };

            var ballot1Guid = Guid.NewGuid();
            var ballot2Guid = Guid.NewGuid();
            var ballot3Guid = Guid.NewGuid();
            var ballots = new List<Ballot>
        {
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot1Guid, StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot2Guid, StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot3Guid, StatusCode = BallotStatusEnum.Ok}
        };

            var person1Guid = Guid.NewGuid();
            var person2Guid = Guid.NewGuid();
            var person3Guid = Guid.NewGuid();
            var votes = new List<vVoteInfo>
        {
          new vVoteInfo {PersonGuid = person1Guid, BallotGuid = ballot1Guid},
          new vVoteInfo {PersonGuid = person1Guid, BallotGuid = ballot2Guid},
          new vVoteInfo {PersonGuid = person1Guid, BallotGuid = ballot3Guid},
          new vVoteInfo {PersonGuid = person2Guid, BallotGuid = ballot1Guid},
          new vVoteInfo {PersonGuid = person2Guid, BallotGuid = ballot2Guid},
          new vVoteInfo {PersonGuid = person2Guid, BallotGuid = ballot3Guid},
          new vVoteInfo {PersonGuid = person3Guid, BallotGuid = ballot1Guid},
          new vVoteInfo {PersonGuid = person3Guid, BallotGuid = ballot2Guid},
          new vVoteInfo {PersonGuid = person3Guid, BallotGuid = ballot3Guid},
        };
            foreach (var vVoteInfo in votes)
            {
                vVoteInfo.ElectionGuid = electionGuid;
                vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
                vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
                vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
            }

            var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

            model.AnalyzeEverything();

            var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
            var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

            resultTies.Count.ShouldEqual(1);
            resultTies[0].NumToElect.ShouldEqual(0);
            resultTies[0].NumInTie.ShouldEqual(3);
            resultTies[0].TieBreakRequired.ShouldEqual(false);

            results.Count.ShouldEqual(3);

            var result1 = results[0];
            result1.VoteCount.ShouldEqual(3);
            result1.Rank.ShouldEqual(1);
            result1.Section.ShouldEqual(ResultHelper.Section.Top);
            result1.IsTied.ShouldEqual(true);
            result1.TieBreakGroup.ShouldEqual(1);
            result1.TieBreakRequired.ShouldEqual(false);
            result1.ForceShowInOther.ShouldEqual(false);

            var result2 = results[1];
            result2.VoteCount.ShouldEqual(3);
            result2.Rank.ShouldEqual(2);
            result2.Section.ShouldEqual(ResultHelper.Section.Top);
            result2.IsTied.ShouldEqual(true);
            result2.TieBreakGroup.ShouldEqual(1);
            result2.ForceShowInOther.ShouldEqual(false);
            result2.TieBreakRequired.ShouldEqual(false);

            var result3 = results[2];
            result3.VoteCount.ShouldEqual(3);
            result3.Rank.ShouldEqual(3);
            result3.Section.ShouldEqual(ResultHelper.Section.Top);
            result3.IsTied.ShouldEqual(true);
            result3.TieBreakGroup.ShouldEqual(1);
            result3.ForceShowInOther.ShouldEqual(false);
            result3.TieBreakRequired.ShouldEqual(false);
        }

        [TestMethod]
        public void Election_3_people_with_3_way_Tie()
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
              {
                  ElectionGuid = electionGuid,
                  NumberToElect = 1,
                  NumberExtra = 0
              };
            var location = new Location
              {
                  LocationGuid = Guid.NewGuid(),
                  ElectionGuid = electionGuid
              };

            var ballot1Guid = Guid.NewGuid();
            var ballot2Guid = Guid.NewGuid();
            var ballot3Guid = Guid.NewGuid();
            var ballots = new List<Ballot>
        {
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot1Guid, StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot2Guid, StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = ballot3Guid, StatusCode = BallotStatusEnum.Ok},
        };
            var votes = new List<vVoteInfo>
        {
          new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid = ballot1Guid},
          new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid = ballot2Guid},
          new vVoteInfo {SingleNameElectionCount = 10, PersonGuid = Guid.NewGuid(), BallotGuid = ballot3Guid},
        };
            foreach (var vVoteInfo in votes)
            {
                vVoteInfo.ElectionGuid = electionGuid;
                vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
                vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
                vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
            }

            var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

            model.AnalyzeEverything();

            var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();

            var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

            resultTies.Count.ShouldEqual(1);
            resultTies[0].NumToElect.ShouldEqual(1);
            resultTies[0].NumInTie.ShouldEqual(3);
            resultTies[0].TieBreakRequired.ShouldEqual(true);

            results.Count.ShouldEqual(3);

            var result1 = results[0];
            result1.VoteCount.ShouldEqual(1);
            result1.Rank.ShouldEqual(1);
            result1.Section.ShouldEqual(ResultHelper.Section.Top);
            result1.IsTied.ShouldEqual(true);
            result1.TieBreakGroup.ShouldEqual(1);
            result1.TieBreakRequired = true;

            var result2 = results[1];
            result2.VoteCount.ShouldEqual(1);
            result2.Rank.ShouldEqual(2);
            result2.Section.ShouldEqual(ResultHelper.Section.Other);
            result2.IsTied.ShouldEqual(true);
            result2.TieBreakGroup.ShouldEqual(1);
            result2.TieBreakRequired = true;
            result2.ForceShowInOther = true;

            var result3 = results[2];
            result3.VoteCount.ShouldEqual(1);
            result3.Rank.ShouldEqual(3);
            result3.Section.ShouldEqual(ResultHelper.Section.Other);
            result3.IsTied.ShouldEqual(true);
            result3.TieBreakGroup.ShouldEqual(1);
            result3.TieBreakRequired = true;
            result3.ForceShowInOther = true;
        }

        [TestMethod]
        public void ElectionWithTwoSetsOfTies()
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
              {
                  ElectionGuid = electionGuid,
                  NumberToElect = 2,
                  NumberExtra = 2
              };
            var location = new Location
              {
                  LocationGuid = Guid.NewGuid(),
                  ElectionGuid = electionGuid
              };

            var ballots = new List<Ballot>
        {
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
        };

            // results wanted:
            //  person 0 = 3 votes
            //  person 1 = 2
            // ---
            //  person 2 = 2
            //  person 3 = 1
            // --
            //  person 4 = 1
            //  person 5 = 1
            var votes = new List<vVoteInfo>
        {
          new vVoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[0].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[0].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[1].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[1].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[2].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[2].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[3].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[3].PersonGuid, BallotGuid = ballots[3].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[4].PersonGuid, BallotGuid = ballots[4].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[5].PersonGuid, BallotGuid = ballots[4].BallotGuid},
        };
            foreach (var vVoteInfo in votes)
            {
                vVoteInfo.ElectionGuid = electionGuid;
                vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
                vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
                vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
            }

            var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

            model.AnalyzeEverything();

            var summary = model.ResultSummaryFinal;
            summary.NumBallotsEntered.ShouldEqual(5);
            summary.SpoiledBallots.ShouldEqual(0);
            summary.SpoiledVotes.ShouldEqual(0);
            summary.BallotsNeedingReview.ShouldEqual(0);

            var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
            var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

            resultTies.Count.ShouldEqual(2);
            resultTies[0].NumToElect.ShouldEqual(1);
            resultTies[0].NumInTie.ShouldEqual(2);
            resultTies[0].TieBreakRequired.ShouldEqual(true);

            resultTies[1].NumToElect.ShouldEqual(1);
            resultTies[1].NumInTie.ShouldEqual(3);
            resultTies[1].TieBreakRequired.ShouldEqual(true);

            results.Count.ShouldEqual(6);

            results[0].IsTied.ShouldEqual(false);
            results[0].CloseToPrev.ShouldEqual(false);
            results[0].CloseToNext.ShouldEqual(true);
            results[0].Section.ShouldEqual(ResultHelper.Section.Top);
            results[0].TieBreakRequired = null;
            results[0].ForceShowInOther = null;

            results[1].IsTied.ShouldEqual(true);
            results[1].TieBreakGroup.ShouldEqual(1);
            results[1].CloseToPrev.ShouldEqual(true);
            results[1].CloseToNext.ShouldEqual(true);
            results[1].Section.ShouldEqual(ResultHelper.Section.Top);
            results[1].TieBreakRequired = true;
            results[1].ForceShowInOther = false;

            results[2].IsTied.ShouldEqual(true);
            results[2].TieBreakGroup.ShouldEqual(1);
            results[2].CloseToPrev.ShouldEqual(true);
            results[2].CloseToNext.ShouldEqual(true);
            results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
            results[2].TieBreakRequired = true;
            results[2].ForceShowInOther = false;

            results[3].IsTied.ShouldEqual(true);
            results[3].TieBreakGroup.ShouldEqual(2);
            results[3].CloseToPrev.ShouldEqual(true);
            results[3].CloseToNext.ShouldEqual(true);
            results[3].Section.ShouldEqual(ResultHelper.Section.Extra);
            results[3].TieBreakRequired = true;
            results[3].ForceShowInOther = false;

            results[4].IsTied.ShouldEqual(true);
            results[4].TieBreakGroup.ShouldEqual(2);
            results[4].CloseToPrev.ShouldEqual(true);
            results[4].CloseToNext.ShouldEqual(true);
            results[4].Section.ShouldEqual(ResultHelper.Section.Other);
            results[4].ForceShowInOther.ShouldEqual(true);
            results[4].TieBreakRequired = true;
            results[4].ForceShowInOther = true;

            results[5].IsTied.ShouldEqual(true);
            results[5].TieBreakGroup.ShouldEqual(2);
            results[5].CloseToPrev.ShouldEqual(true);
            results[5].CloseToNext.ShouldEqual(false);
            results[5].Section.ShouldEqual(ResultHelper.Section.Other);
            results[5].ForceShowInOther.ShouldEqual(true);
            results[5].TieBreakRequired = true;
            results[5].ForceShowInOther = true;
        }

        [TestMethod]
        public void ElectionTieSpanningTopExtraOther()
        {
            var electionGuid = Guid.NewGuid();
            var election = new Election
              {
                  ElectionGuid = electionGuid,
                  NumberToElect = 2,
                  NumberExtra = 2
              };
            var location = new Location
              {
                  LocationGuid = Guid.NewGuid(),
                  ElectionGuid = electionGuid
              };

            var ballots = new List<Ballot>
        {
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
          new Ballot
            {LocationGuid = location.LocationGuid, BallotGuid = Guid.NewGuid(), StatusCode = BallotStatusEnum.Ok},
        };

            // results wanted:
            //  person 0 = 2 votes
            //  person 1 = 1
            // ---
            //  person 2 = 1
            //  person 3 = 1
            // --
            //  person 4 = 1
            var votes = new List<vVoteInfo>
        {
          new vVoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[0].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[1].PersonGuid, BallotGuid = ballots[0].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[0].PersonGuid, BallotGuid = ballots[1].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[2].PersonGuid, BallotGuid = ballots[1].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[3].PersonGuid, BallotGuid = ballots[2].BallotGuid},
          new vVoteInfo {PersonGuid = SamplePeople[4].PersonGuid, BallotGuid = ballots[2].BallotGuid},
        };
            foreach (var vVoteInfo in votes)
            {
                vVoteInfo.ElectionGuid = electionGuid;
                vVoteInfo.PersonCombinedInfo = vVoteInfo.PersonCombinedInfoInVote = "zz";
                vVoteInfo.BallotStatusCode = BallotStatusEnum.Ok;
                vVoteInfo.VoteStatusCode = VoteHelper.VoteStatusCode.Ok;
            }

            var model = new ElectionAnalyzerNormal(_fakes, election, votes, ballots, SamplePeople);

            model.AnalyzeEverything();

            var summary = model.ResultSummaryFinal;
            summary.NumBallotsEntered.ShouldEqual(3);
            summary.SpoiledBallots.ShouldEqual(0);
            summary.SpoiledVotes.ShouldEqual(0);
            summary.BallotsNeedingReview.ShouldEqual(0);

            var results = model.Results.OrderByDescending(r => r.VoteCount).ToList();
            var resultTies = model.ResultTies.OrderBy(rt => rt.TieBreakGroup).ToList();

            resultTies.Count.ShouldEqual(1);
            resultTies[0].NumToElect.ShouldEqual(3);
            resultTies[0].NumInTie.ShouldEqual(4);
            resultTies[0].TieBreakRequired.ShouldEqual(true);

            results.Count.ShouldEqual(5);

            results[0].IsTied.ShouldEqual(false);
            results[0].CloseToPrev.ShouldEqual(false);
            results[0].CloseToNext.ShouldEqual(true);
            results[0].Section.ShouldEqual(ResultHelper.Section.Top);
            results[0].TieBreakRequired.ShouldEqual(false);
            results[0].ForceShowInOther.ShouldEqual(false);

            results[1].IsTied.ShouldEqual(true);
            results[1].TieBreakGroup.ShouldEqual(1);
            results[1].CloseToPrev.ShouldEqual(true);
            results[1].CloseToNext.ShouldEqual(true);
            results[1].Section.ShouldEqual(ResultHelper.Section.Top);
            results[1].TieBreakRequired.ShouldEqual(true);
            results[1].ForceShowInOther.ShouldEqual(false);

            results[2].IsTied.ShouldEqual(true);
            results[2].TieBreakGroup.ShouldEqual(1);
            results[2].CloseToPrev.ShouldEqual(true);
            results[2].CloseToNext.ShouldEqual(true);
            results[2].Section.ShouldEqual(ResultHelper.Section.Extra);
            results[2].TieBreakRequired.ShouldEqual(true);
            results[2].ForceShowInOther.ShouldEqual(false);

            results[3].IsTied.ShouldEqual(true);
            results[3].TieBreakGroup.ShouldEqual(1);
            results[3].CloseToPrev.ShouldEqual(true);
            results[3].CloseToNext.ShouldEqual(true);
            results[3].Section.ShouldEqual(ResultHelper.Section.Extra);
            results[3].TieBreakRequired.ShouldEqual(true);
            results[3].ForceShowInOther.ShouldEqual(false);

            results[4].IsTied.ShouldEqual(true);
            results[4].TieBreakGroup.ShouldEqual(1);
            results[4].CloseToPrev.ShouldEqual(true);
            results[4].CloseToNext.ShouldEqual(false);
            results[4].Section.ShouldEqual(ResultHelper.Section.Other);
            results[4].ForceShowInOther.ShouldEqual(true);
            results[4].TieBreakRequired.ShouldEqual(true);
            results[4].ForceShowInOther.ShouldEqual(true);
        }
    }
}