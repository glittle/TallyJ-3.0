using System;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.Models;

namespace TallyJ.CoreModels
{
  public class VoteHelper
  {
    private readonly bool _forBallot;
    private readonly bool _everyoneCanReceiveVotes;
    private Guid? _notInTieBreakGuidNullable;
    private Guid _notInTieBreakGuid;

    public static class VoteStatusCode
    {
      public const string Ok = "Ok";
      public const string Changed = "Changed"; // if the person info does not match
      public const string Spoiled = "Spoiled";
    }

    public VoteHelper(bool forBallot) : this(forBallot, UserSession.CurrentElection.CanReceive == ElectionModel.CanVoteOrReceive.All)
    {
    }

    public VoteHelper(bool forBallot, bool everyoneCanReceiveVotes)
    {
      _forBallot = forBallot;
      _everyoneCanReceiveVotes = everyoneCanReceiveVotes;
      _notInTieBreakGuid = IneligibleReasonEnum.Ineligible_Not_in_TieBreak.Value;
      _notInTieBreakGuidNullable = _notInTieBreakGuid.AsNullableGuid();
    }

    /// <Summary>Extend the Ineligible reason to include whether they can receive votes</Summary>
    public Guid? IneligibleToReceiveVotes(Guid? ineligible, bool? thisPersonCanReceiveVotes)
    {
      return IneligibleToReceiveVotes(ineligible.GetValueOrDefault(), thisPersonCanReceiveVotes.AsBoolean()).AsNullableGuid();
    }

    /// <Summary>Extend the Ineligible reason to include whether they can receive votes</Summary>
    public Guid IneligibleToReceiveVotes(Guid ineligible, bool thisPersonCanReceiveVotes)
    {
      if (ineligible != Guid.Empty)
      {
        return ineligible;
      }
      return (_everyoneCanReceiveVotes || !_forBallot || thisPersonCanReceiveVotes ? Guid.Empty : _notInTieBreakGuid);
    }
  }
}