using System;
using System.Collections.Generic;
using System.Linq;
using EntityFramework.Caching;
using EntityFramework.Extensions;
using TallyJ.Code;
using TallyJ.Code.Data;
using TallyJ.Code.Session;
using TallyJ.Code.UnityRelated;

namespace TallyJ.EF
{
    public enum BallotProcessEnum
    {
        // define the supported processes
        Unknown, // not defined yet
        None, // do not use any Gathering Ballots
        Roll, // roll call
        RegV, // register, vote, collect after
        RegC, // register, collect together
    }

    public enum EnvNumModeEnum
    {
        // define the supported processes
        None, // do not show any
        Absentee, // only for absentee
        All, // for all
    }


    [Serializable]
    public partial class Election : IIndexedForCaching
    {
        enum ExtraSettingKey
        {
            // keep names as short as possible
            BP, // Ballot Process?
            Env, // Envelope Mode
            T24, // use 24 hour time?
        }

        /// <summary>
        /// This is a "fake" column that is embedded into the OwnerLoginId column
        /// </summary>
        /// <remarks>
        /// Must be a string to serialize out to client
        /// </remarks>
        public string BallotProcessRaw
        {
            get { return GetExtraSetting(ExtraSettingKey.BP) ?? BallotProcessEnum.Roll.ToString(); }
            set
            {
                if (value != null && !Enum.IsDefined(typeof(BallotProcessEnum), value))
                {
                    throw new ApplicationException("Invalid process key: " + value);
                }

                SetExtraSettting(ExtraSettingKey.BP, value);
            }
        }


        public BallotProcessEnum BallotProcess
        {
            get
            {
                var bp = BallotProcessRaw;
                if (Enum.IsDefined(typeof(BallotProcessEnum), bp))
                {
                    return (BallotProcessEnum) Enum.Parse(typeof(BallotProcessEnum), bp);
                }

                return BallotProcessEnum.Unknown;
            }
        }

        /// <summary>
        /// This is a "fake" column that is embedded into the OwnerLoginId column
        /// </summary>
        /// <remarks>
        /// Must be a string to serialize out to client
        /// </remarks>
        public string EnvNumModeRaw
        {
            get { return GetExtraSetting(ExtraSettingKey.Env) ?? EnvNumModeEnum.Absentee.ToString(); }
            set
            {
                if (value != null && !Enum.IsDefined(typeof(EnvNumModeEnum), value))
                {
                    throw new ApplicationException("Invalid envelope number mode: " + value);
                }

                SetExtraSettting(ExtraSettingKey.Env, value);
            }
        }

        public EnvNumModeEnum EnvNumMode
        {
            get
            {
                var env = EnvNumModeRaw;
                if (Enum.IsDefined(typeof(EnvNumModeEnum), env))
                {
                    return (EnvNumModeEnum) Enum.Parse(typeof(EnvNumModeEnum), env);
                }

                return EnvNumModeEnum.Absentee;
            }
        }


        /// <summary>
        /// This is a "fake" column that is embedded into the OwnerLoginId column
        /// </summary>
        /// <remarks>
        /// Must be a string to serialize out to client
        /// </remarks>
        public bool T24
        {
            get { return GetExtraSetting(ExtraSettingKey.T24).AsBoolean(); }
            set { SetExtraSettting(ExtraSettingKey.T24, value ? "1" : "0"); }
        }
        //public string Test2
        //{
        //  // Replace this when a second fake field is created!

        //  get
        //  {
        //    return GetExtraSetting(ExtraSettingKey.Test2);
        //  }
        //  set
        //  {
        //    SetExtraSettting(ExtraSettingKey.Test2, value);
        //  }
        //}


        public bool IsSingleNameElection => 
          NumberToElect.GetValueOrDefault(0) == 1 && NumberExtra.GetValueOrDefault(0) == 0;

        public bool OnlineCurrentlyOpen
        {
          get
          {
            var now = DateTime.Now;
            return OnlineWhenOpen.HasValue
                   && OnlineWhenClose.HasValue
                   && OnlineWhenOpen.Value < now
                   && OnlineWhenOpen.Value < OnlineWhenClose.Value
                   && OnlineWhenClose.Value > now;
          }
        }

        public bool CanBeAvailableForGuestTellers
        {
            get
            {
                return ListForPublic.AsBoolean()
                       && ElectionPasscode.HasContent()
                       && ListedForPublicAsOf.HasValue;
            }
        }

        /// <Summary>Erase all ballots and results</Summary>
        public static void EraseBallotsAndResults(Guid electionGuid)
        {
            var db = UnityInstance.Resolve<IDbContextFactory>().DbContext;

            db.Result.Where(r => r.ElectionGuid == electionGuid).Delete();
            db.ResultTie.Where(r => r.ElectionGuid == electionGuid).Delete();
            db.ResultSummary.Where(r => r.ElectionGuid == electionGuid).Delete();

            // delete ballots in all locations... cascading will delete votes
            db.Ballot.Where(b =>
                db.Location.Where(x => x.ElectionGuid == electionGuid).Select(l => l.LocationGuid)
                    .Contains(b.LocationGuid)).Delete();
        }

        public long RowVersionInt
        {
            get
            {
                if (C_RowVersion == null)
                {
                    return 0;
                }

                return BitConverter.ToInt64(C_RowVersion, 0);
            }
        }


        const char FlagChar = '~';
        const char SplitChar = ';';
        private Dictionary<ExtraSettingKey, string> _extraDict;

        private Dictionary<ExtraSettingKey, string> ExtraSettings
        {
            get
            {
                if (_extraDict != null)
                {
                    return _extraDict;
                }
                // column contents...  ~Flag=1;FlagB=hello

                if (string.IsNullOrWhiteSpace(OwnerLoginId) || OwnerLoginId[0] != FlagChar)
                {
                    _extraDict = new Dictionary<ExtraSettingKey, string>();
                }
                else
                {
                    _extraDict = OwnerLoginId
                        .Substring(1) // skip flag char
                        .Trim()
                        .Split(SplitChar)
                        .Select(s => s.Split('='))
                        .Where(a => Enum.IsDefined(typeof(ExtraSettingKey), a[0]))
                        // any that are not recognized are ignored and lost
                        .ToDictionary(a => (ExtraSettingKey) Enum.Parse(typeof(ExtraSettingKey), a[0]), a => a[1]);
                }

                return _extraDict;
            }
        }

        private string GetExtraSetting(ExtraSettingKey setting)
        {
            string value;
            if (ExtraSettings.TryGetValue(setting, out value))
            {
                return value;
            }

            return null;
        }

        private void SetExtraSettting(ExtraSettingKey setting, string value)
        {
            var s = value ?? "";
            if (s.Contains("=") || s.Contains(SplitChar))
            {
                throw new ApplicationException("Invalid value for extra settings: " + s);
            }

            var dict = ExtraSettings;

            if (s == "")
            {
                if (dict.ContainsKey(setting))
                {
                    dict.Remove(setting);
                }
            }
            else
            {
                dict[setting] = s;
            }

            if (dict.Count == 0)
            {
                OwnerLoginId = null;
            }
            else
            {
                OwnerLoginId = FlagChar + dict.Select(kvp => kvp.Key + "=" + kvp.Value).JoinedAsString(SplitChar);
            }

            _extraDict = dict;
        }
    }
}