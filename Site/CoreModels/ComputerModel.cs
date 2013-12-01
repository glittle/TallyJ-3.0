using System;
using System.Collections.Generic;
using System.Data.Entity.Infrastructure;
using System.Data.Entity.SqlServer;
using System.Linq;
using System.Web;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels
{
    public class ComputerModel : DataConnectedModel
    {
        public Computer CreateComputerRecordForMe()
        {
            ClearOutOldComputerRecords();

            var computer = new Computer
                             {
                                 ShadowElectionGuid = Guid.NewGuid(),
                                 LastContact = DateTime.Now,
                                 BrowserInfo = HttpContext.Current.Request.Browser.Type
                             };

            Db.Computer.Add(computer);
            Db.SaveChanges();

            SessionKey.CurrentComputer.SetInSession(computer);

            return computer;
        }

        /// <Summary>Remove all computer records in the entire database (all elections) that are not active</Summary>
        private void ClearOutOldComputerRecords()
        {
            const int maxMinutesOfNoContact = 5;

            var now = DateTime.Now;
            var computers = Db.Computer.ToList();

            foreach (var oldComputer in computers.Where(c => !c.LastContact.HasValue || (now - c.LastContact.Value).TotalMinutes > maxMinutesOfNoContact))
            {
                Db.Computer.Remove(oldComputer);
            }
            Db.SaveChanges();
        }

        /// <Summary>Add computer into election, with unique computer code</Summary>
        public void AddCurrentComputerIntoElection(Guid electionGuid)
        {
            var computer = UserSession.CurrentComputer ?? CreateComputerRecordForMe();

            Db.Computer.Attach(computer);

            computer.ElectionGuid = electionGuid;
            computer.LocationGuid = null;
            SessionKey.CurrentLocation.SetInSession<Location>(null);

            computer.ComputerCode = DetermineNextFreeComputerCode(
              Db.Computer
                .Where(c => c.ElectionGuid == electionGuid)
                .Select(c => c.ComputerCode)
                //--> 
                //.Union(Db.vBallotInfoes
                //         .Where(b => b.ElectionGuid == electionGuid)
                //         .Select(b => b.ComputerCode)
                //)
                .Distinct()
                .OrderBy(s => s)
                .ToList());

            SessionKey.CurrentComputer.SetInSession(computer);

            Db.SaveChanges();
        }

        /// <Summary>Move this computer into this location (don't change the computer code)</Summary>
        public bool AddCurrentComputerIntoLocation(int id)
        {
            var location =
              ContextItems.LocationModel.Locations.SingleOrDefault(
                l => l.C_RowId == id);

            if (location == null)
            {
                SessionKey.CurrentLocation.SetInSession<Location>(null);
                return false;
            }

            var computer = UserSession.CurrentComputer;
            if (computer.ElectionGuid.HasValue)
            {
                Db.Computer.Attach(computer);
                computer.LocationGuid = location.LocationGuid;
                computer.LastContact = DateTime.Now;
                Db.SaveChanges();
            }
            SessionKey.CurrentLocation.SetInSession(location);

            // reset ballot #
            SessionKey.CurrentBallotId.SetInSession(0);
            if (UserSession.CurrentElection.IsSingleNameElection)
            {
                // for single name elections, only have one ballot per computer per location. (But if altered from a normal election to a single name election, may have multiple.)
                var ballotId =
                  Db.Ballot.Where(
                    b => b.LocationGuid == location.LocationGuid && b.ComputerCode == computer.ComputerCode)
                    .Select(b => b.C_RowId).FirstOrDefault();
                if (ballotId != 0)
                {
                    SessionKey.CurrentBallotId.SetInSession(ballotId);
                }
            }

            return true;
        }

        public string DetermineNextFreeComputerCode(IEnumerable<string> existingCodesSortedAsc)
        {
            var codeToUse = 'A';
            var twoDigit = false;
            var firstDigit = (char)('A' - 1);

            foreach (var computerCode in existingCodesSortedAsc)
            {
                char testChar;
                if (computerCode.Length == 2)
                {
                    twoDigit = true;
                    testChar = computerCode[1];
                    firstDigit = computerCode[0];
                }
                else
                {
                    testChar = computerCode[0];
                }
                if (testChar == codeToUse)
                {
                    // push the answer to the next one
                    codeToUse = (char)(codeToUse + 1);
                    if (codeToUse == 'I' || codeToUse == 'L' || codeToUse == 'O')
                    {
                        codeToUse++;
                    }
                    if (codeToUse > 'Z')
                    {
                        twoDigit = true;
                        codeToUse = 'A';
                        firstDigit = (char)(firstDigit + 1);
                    }
                }
            }
            if (codeToUse > 'Z')
            {
                return "" + firstDigit + (char)('A' - 1 + codeToUse - 'Z');
            }
            if (twoDigit)
            {
                return "" + firstDigit + codeToUse;
            }
            return "" + codeToUse;
        }

        public bool ProcessPulse()
        {
            var computer = UserSession.CurrentComputer;
            if (computer == null)
            {
                return false;
            }
            Db.Computer.Attach(computer);

            var lastContact = DateTime.Now;
            computer.LastContact = lastContact;

            try
            {
                Db.SaveChanges();
            }
            catch (DbUpdateConcurrencyException ex)
            {
                ((IObjectContextAdapter)Db).ObjectContext.Detach(computer);
                // if this computer has been inactive, its computer record may have been removed
                CreateComputerRecordForMe();
                AddCurrentComputerIntoElection(UserSession.CurrentElectionGuid);
            }

            if (computer.ElectionGuid != UserSession.CurrentElectionGuid)
            {
                return false;
            }

            return true;
        }

        public void DeleteAtLogout(int computerRowId)
        {
            //TODO: should this be deleted at logout??
        }
    }
}