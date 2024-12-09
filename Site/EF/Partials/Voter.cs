using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Diagnostics;
using System.Linq;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;

namespace TallyJ.EF
{
  [MetadataType(typeof(PersonVoterMetadata))]
  [DebuggerDisplay($"{{CanVote}} {{CanReceiveVotes}} {{IneligibleReasonGuid}}")]
  public partial class Voter : IIndexedForCaching
  {
    public Person Person { get; set; } // manually loaded

    private class PersonVoterMetadata
    {
      [DebuggerDisplay("Local = {RegistrationTime.ToLocalTime()}, UTC = {RegistrationTime}")]
      public object RegistrationTime { get; set; }
    }

    /// <remarks>
    /// The values in the string must not contain <see cref="ArraySplit"/> |
    /// </remarks>
    public List<string> RegistrationLog
    {
      get => RegLog.HasContent() ? RegLog.Split(ArraySplit).ToList() : new List<string>();
      set => RegLog = value.JoinedAsString(ArraySplit);
    }
    
    const char ArraySplit = '|'; // for when the value is an array or List<string>

  }
}