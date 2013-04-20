using System;
using TallyJ.Code;

namespace TallyJ.Models
{
    public partial class ResultSummary
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
                if (EnvelopesInPerson.HasValue || EnvelopesDroppedOff.HasValue || EnvelopesMailedIn.HasValue ||
                    EnvelopesCalledIn.HasValue)
                {
                    return EnvelopesInPerson.GetValueOrDefault()
                           + EnvelopesDroppedOff.GetValueOrDefault()
                           + EnvelopesMailedIn.GetValueOrDefault()
                           + EnvelopesCalledIn.GetValueOrDefault();
                }
                return null;
            }
        }

        public int? NumBallotsWithManual
        {
            get
            {
                if (NumBallotsEntered.HasValue || SpoiledManualBallots.HasValue)
                {
                    return NumBallotsEntered.GetValueOrDefault() + SpoiledManualBallots.GetValueOrDefault();
                }

                return null;
            }
        }
    }
}