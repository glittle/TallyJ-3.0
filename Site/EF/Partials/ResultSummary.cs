using System;
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
            CalledInBallots.HasValue)
        {
          return InPersonBallots.GetValueOrDefault()
                 + DroppedOffBallots.GetValueOrDefault()
                 + MailedInBallots.GetValueOrDefault()
                 + CalledInBallots.GetValueOrDefault();
        }
        return null;
      }
    }

    public int? NumBallotsWithManual
    {
      get
      {
        if (BallotsReceived.HasValue || SpoiledManualBallots.HasValue)
        {
          return BallotsReceived.GetValueOrDefault() + SpoiledManualBallots.GetValueOrDefault();
        }

        return null;
      }
    }
  }
}