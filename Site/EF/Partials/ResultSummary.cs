﻿using System;
using TallyJ.Code;

namespace TallyJ.EF
{
  public partial class ResultSummary : IIndexedForCaching
  {
    public int PercentParticipation
    {
      get
      {
        return NumEligibleToVote.AsInt() == 0
          ? 0
          : Math.Round(
            (NumBallotsWithManual.AsInt() * 100D) /
            NumEligibleToVote.AsInt(), 0).AsInt();
      }
    }

    /// <Summary>Total of all collected</Summary>
    public int? SumOfEnvelopesCollected
    {
      get
      {
        if (InPersonBallots.HasValue || DroppedOffBallots.HasValue || MailedInBallots.HasValue ||
            CalledInBallots.HasValue || OnlineBallots.HasValue || ImportedBallots.HasValue || Custom1Ballots.HasValue ||
            Custom2Ballots.HasValue || Custom3Ballots.HasValue)
        {
          return InPersonBallots.GetValueOrDefault()
                 + DroppedOffBallots.GetValueOrDefault()
                 + MailedInBallots.GetValueOrDefault()
                 + CalledInBallots.GetValueOrDefault()
                 + OnlineBallots.GetValueOrDefault()
                 + ImportedBallots.GetValueOrDefault()
                 + Custom1Ballots.GetValueOrDefault()
                 + Custom2Ballots.GetValueOrDefault()
                 + Custom3Ballots.GetValueOrDefault()
            ;
        }
        return null;
      }
    }

    public int? NumBallotsWithManual
    {
      get
      {
        if (BallotsReceived.HasValue || SpoiledBallots.HasValue)
        {
          return BallotsReceived.GetValueOrDefault() + SpoiledBallots.GetValueOrDefault();
        }

        return null;
      }
    }
  }
}