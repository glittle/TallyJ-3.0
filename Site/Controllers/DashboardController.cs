using System;
using System.IO;
using System.Linq;
using System.Security.Permissions;
using System.Web;
using System.Web.Mvc;
using TallyJ.Code;
using TallyJ.Code.Enumerations;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.ExportImport;
using TallyJ.CoreModels.Helper;
using TallyJ.CoreModels.Hubs;
using TallyJ.EF;

namespace TallyJ.Controllers
{
    public class DashboardController : BaseController
        /// <summary>
        /// Displays the index view for the elections list.
        /// </summary>
        /// <returns>
        /// An <see cref="ActionResult"/> that represents the result of the action. 
        /// If there is no current election, it redirects to the appropriate action based on the user's teller status.
        /// </returns>
        /// <remarks>
        /// This method checks if there is a current election by verifying the <c>CurrentElectionGuid</</c> and <c>CurrentElection</c> properties of the <c>UserSession</c> class.
        /// If no current election is found (i.e., the GUID is empty or the election is null), it redirects the user to the "ElectionList" action if they are a known teller,
        /// or to the "LogOff" action in the "Account" controller if they are not. 
        /// If a current election exists, it returns the index view with a new instance of <c>ElectionsListViewModel</c>.
        /// </remarks>
    {
        [AllowTellersInActiveElection]
        public ActionResult Index()
        {
            if (UserSession.CurrentElectionGuid == Guid.Empty || UserSession.CurrentElection == null)
            {
                // no current election
                return UserSession.IsKnownTeller
                         ? RedirectToAction("ElectionList")
                         : RedirectToAction("LogOff", "Account");
            }

            return View(new ElectionsListViewModel());
        }
        /// <summary>
        /// Retrieves and displays a list of elections for the authenticated teller.
        /// </summary>
        /// <remarks>
        /// This method first checks if the current election GUID is empty. If it is, it attempts to retrieve the current computer associated with the user session.
        /// If no computer is found, it creates a temporary computer model for the user. If a computer is found, it updates the computer information using the ComputerCacher.
        /// After ensuring the computer information is up to date, it informs the public about the visible elections through the PublicHub.
        /// Finally, it returns a view containing a model of the elections list.
        /// </remarks>
        /// <returns>An ActionResult that renders the ElectionsListViewModel view.</returns>

        [ForAuthenticatedTeller]
        public ActionResult ElectionList()
        {
            if (UserSession.CurrentElectionGuid == Guid.Empty)
            {
                var currentComputer = UserSession.CurrentComputer;
                if (currentComputer == null)
                {
                    new ComputerModel().GetTempComputerForMe();
                }
                else
                {
                    new ComputerCacher().UpdateComputer(currentComputer);
                }
            }

            new PublicHub().TellPublicAboutVisibleElections();

            return View(new ElectionsListViewModel());
        }
        /// <summary>
        /// Retrieves additional information for the authenticated teller and returns it as a JSON result.
        /// </summary>
        /// <returns>A <see cref="JsonResult"/> containing the additional information for the authenticated teller.</returns>
        /// <remarks>
        /// This method calls the <c>MoreInfoStatic</c> method on an instance of <c>ElectionsListViewModel</c>,
        /// which is expected to gather the necessary data. The result is then converted into a JSON format
        /// using the <c>AsJsonResult</c> extension method. This is useful for providing dynamic data to the client-side
        /// in a structured format, allowing for easier integration with web applications that consume JSON data.
        /// The method is decorated with the <c>ForAuthenticatedTeller</c> attribute, ensuring that only authenticated 
        /// tellers can access this information.
        /// </remarks>

        [ForAuthenticatedTeller]
        public JsonResult MoreInfoStatic()
        {
            return new ElectionsListViewModel().MoreInfoStatic().AsJsonResult();
        /// <summary>
        /// Retrieves more information for live elections and returns it as a JSON result.
        /// </summary>
        /// <returns>A <see cref="JsonResult"/> containing the live election information.</returns>
        /// <remarks>
        /// This method creates an instance of the <see cref="ElectionsListViewModel"/> class and calls its 
        /// <see cref="MoreInfoLive"/> method to gather the necessary data. The resulting data is then 
        /// converted to a JSON format using the <see cref="AsJsonResult"/> extension method. This is useful 
        /// for providing real-time updates and details about ongoing elections to authenticated users.
        /// </remarks>
        }
        [ForAuthenticatedTeller]
        public JsonResult MoreInfoLive()
        {
            return new ElectionsListViewModel().MoreInfoLive().AsJsonResult();
        }
        /// <summary>
        /// Reloads the elections information for the authenticated teller.
        /// </summary>
        /// <returns>A JsonResult containing the success status and the list of elections.</returns>
        /// <remarks>
        /// This method is designed to be called by an authenticated teller to refresh the elections data.
        /// It creates an anonymous object that includes a success flag set to true and retrieves the elections information 
        /// using the <see cref="ElectionsListViewModel.GetMyElectionsInfo(bool)"/> method with a parameter indicating 
        /// that only active elections should be fetched. The result is then returned as a JSON response.
        /// </remarks>

        [ForAuthenticatedTeller]
        public JsonResult ReloadElections()
        {
            return new
            {
                Success = true,
                elections = new ElectionsListViewModel().GetMyElectionsInfo(true)
            }.AsJsonResult();
        }
        /// <summary>
        /// Updates the visibility status of an election listing for public view.
        /// </summary>
        /// <param name="listOnPage">Indicates whether the election should be listed on the public page.</param>
        /// <param name="electionGuid">The unique identifier for the election to be updated.</param>
        /// <returns>A JsonResult indicating the success of the operation and the current status of the election listing.</returns>
        /// <remarks>
        /// This method first verifies if the user has access to update the election by checking if the provided 
        /// <paramref name="electionGuid"/> corresponds to an existing election in the user's elections list. If the 
        /// election is found, it updates the visibility status based on the <paramref name="listOnPage"/> parameter.
        /// 
        /// If the election is successfully updated, it notifies relevant hubs about the change in status and 
        /// returns a success response with information about whether the election is currently open for public viewing.
        /// If the election is not found or if the user does not have permission to update it, an appropriate 
        /// failure response is returned.
        /// </remarks>

        [ForAuthenticatedTeller]
        public JsonResult UpdateListingForElection(bool listOnPage, Guid electionGuid)
        {
            // from the elections list page, when not "in" the election

            // verify we have access
            var election = new ElectionsListViewModel()
              .MyElections()
              .FirstOrDefault(e => e.ElectionGuid == electionGuid);

            if (election == null)
            {
                return new
                {
                    Success = false,
                    Message = "Unknown election"
                }.AsJsonResult();
            }

            // update
            if (UserSession.IsKnownTeller)
            {
                var electionCacher = new ElectionCacher(Db);

                Db.Election.Attach(election);

                election.ListForPublic = listOnPage;
                election.ListedForPublicAsOf = listOnPage ? DateTime.UtcNow : null;

                Db.SaveChanges();

                electionCacher.UpdateItemAndSaveCache(election);

                new PublicHub().TellPublicAboutVisibleElections();

                if (!listOnPage)
                {
                    new MainHub().CloseOutGuestTellers(electionGuid);
                }

                var info = new
                {
                    ElectionGuid = electionGuid,
                    StateName = election.TallyStatus.HasNoContent() ? ElectionTallyStatusEnum.NotStarted.ToString() : election.TallyStatus,
                    Online = election.OnlineCurrentlyOpen,
                    Passcode = election.ElectionPasscode,
                    Listed = election.ListedForPublicAsOf != null
                };

                new MainHub().StatusChangedForElection(electionGuid, info, info);

                return new
                {
                    Success = true,
                    IsOpen = listOnPage
                }.AsJsonResult();
            }

            return new
            {
                Success = false
            }.AsJsonResult();
        }


        /// <summary>
        /// Loads an election from a specified file and returns the result as a JSON response.
        /// </summary>
        /// <param name="loadFile">The file containing the election data to be loaded.</param>
        /// <returns>A <see cref="JsonResult"/> indicating the outcome of the election loading process.</returns>
        /// <remarks>
        /// This method handles the HTTP POST request to load election data from a file provided by the user.
        /// It utilizes the <see cref="ElectionLoader"/> class to perform the import operation on the specified file.
        /// The result of the import operation is returned as a JSON response, which can be used to inform the user about 
        /// the success or failure of the loading process. This method is decorated with attributes to ensure that only 
        /// authorized users (tellers) can perform this action during an active election.
        /// </remarks>


        [HttpPost]
        [AllowTellersInActiveElection]
        public JsonResult LoadV2Election(HttpPostedFileBase loadFile)
        {
            return new ElectionLoader().Import(loadFile);
        }
        /// <summary>
        /// Chooses a location for the current computer based on the provided identifier.
        /// </summary>
        /// <param name="id">The identifier of the location to which the current computer will be moved.</param>
        /// <returns>A JsonResult containing the status of the operation, indicating whether the computer was successfully moved to the specified location.</returns>
        /// <remarks>
        /// This method utilizes the <see cref="ComputerModel"/> class to move the current computer into the specified location identified by <paramref name="id"/>.
        /// It returns a JSON result that includes a property named "Selected", which reflects the outcome of the move operation.
        /// The method is decorated with the <see cref="AllowTellersInActiveElection"/> attribute, which may enforce certain permissions or conditions related to the operation.
        /// </remarks>

        [AllowTellersInActiveElection]
        public JsonResult ChooseLocation(int id)
        {
            return new { Selected = new ComputerModel().MoveCurrentComputerIntoLocation(id) }.AsJsonResult();
        }

        /// <summary>
        /// Chooses a teller for a specific election and returns the result as a JSON response.
        /// </summary>
        /// <param name="num">The identifier for the election.</param>
        /// <param name="teller">The identifier for the teller being chosen.</param>
        /// <param name="newName">An optional parameter representing the new name for the teller. Defaults to an empty string.</param>
        /// <returns>A <see cref="JsonResult"/> containing the result of the teller selection process.</returns>
        /// <remarks>
        /// This method utilizes the <see cref="TellerModel"/> class to perform the operation of choosing a teller for an active election.
        /// It takes in the election identifier and the teller identifier, along with an optional new name for the teller.
        /// The result of this operation is then converted into a JSON format, allowing it to be easily consumed by clients or front-end applications.
        /// This method is decorated with the <see cref="AllowTellersInActiveElection"/> attribute, which may enforce specific permissions or conditions related to active elections.
        /// </remarks>

        [AllowTellersInActiveElection]
        public JsonResult ChooseTeller(int num, int teller, string newName = "")
        {
            return new TellerModel().ChooseTeller(num, teller, newName).AsJsonResult();
        }
        /// <summary>
        /// Deletes a teller by their unique identifier.
        /// </summary>
        /// <param name="id">The unique identifier of the teller to be deleted.</param>
        /// <returns>A JsonResult indicating the outcome of the deletion operation.</returns>
        /// <remarks>
        /// This method attempts to delete a teller from the system using the provided <paramref name="id"/>. 
        /// It utilizes the <c>DeleteTeller</c> method from the <c>TellerModel</c> class to perform the deletion. 
        /// The result of the deletion operation is then converted to a JsonResult, which can be used to provide feedback 
        /// to the client regarding the success or failure of the operation. 
        /// This method is typically used in scenarios where a teller needs to be removed from an active election context.
        /// </remarks>

        [AllowTellersInActiveElection]
        public JsonResult DeleteTeller(int id)
        {
            return new TellerModel().DeleteTeller(id).AsJsonResult();
        }
        /// <summary>
        /// Removes a full teller from an election based on the provided email and join ID.
        /// </summary>
        /// <param name="email">The email of the teller to be removed.</param>
        /// <param name="joinId">The unique identifier of the join record associated with the teller.</param>
        /// <returns>A JsonResult indicating the success or failure of the removal operation.</returns>
        /// <remarks>
        /// This method first retrieves the join record for the specified join ID. If the join record is not found, it returns a failure message indicating an unknown ID.
        /// It then checks if the current user is the owner of the election associated with the join record. If the user is not the owner or has a role that prevents removal, 
        /// it returns a failure message indicating that removal is not allowed. If the conditions are met, the method proceeds to remove the join record from the database.
        /// After successfully removing the record, it logs the action indicating whether a full teller or a pending full teller was removed. 
        /// Finally, it returns a success message.
        /// </remarks>

        [ForAuthenticatedTeller]
        public JsonResult RemoveFullTeller(string email, int joinId)
        {
            var join = Db.JoinElectionUser
              .FirstOrDefault(je => je.C_RowId == joinId);

            if (join == null)
            {
                return new
                {
                    Success = false,
                    Message = "Unknown id"
                }.AsJsonResult();
            }

            var targetElectionGuid = join.ElectionGuid;

            // ensure that I am the owner of the election
            var myJoin = Db.JoinElectionUser
              .FirstOrDefault(je => je.ElectionGuid == targetElectionGuid && je.UserId == UserSession.UserGuid);
            if (myJoin == null || myJoin.Role != null)
            {
                return new
                {
                    Success = false,
                    Message = "Removal not allowed"
                }.AsJsonResult();
            }

            Db.JoinElectionUser.Remove(join);

            Db.SaveChanges();

            if (email.HasContent() || join.UserId != Guid.Empty)
            {
                new LogHelper(targetElectionGuid).Add($"Removed full teller - {email ?? join.InviteEmail}", true);
            }
            else
            {
                new LogHelper(targetElectionGuid).Add($"Removed pending full teller - {email ?? join.InviteEmail ?? "?"}", true);
            }

            //TODO notify this user and sign them out!
            return new
            {
                Success = true
            }.AsJsonResult();
        }
        /// <summary>
        /// Adds a full teller to an election, creating an anonymous user if necessary.
        /// </summary>
        /// <param name="email">The email address of the teller to be added.</param>
        /// <param name="election">The unique identifier of the election to which the teller is being added.</param>
        /// <returns>A JsonResult indicating the success of the operation and containing user details if successful.</returns>
        /// <remarks>
        /// This method first checks if the current user is authorized to add a teller to the specified election. 
        /// If the user is not authorized, it returns a failure message. 
        /// If the user is authorized, it checks for the existence of an anonymous user in the database. 
        /// If no anonymous user exists, it creates one with a default username and marks it as anonymous. 
        /// Then, it adds a new entry in the JoinElectionUser table for the full teller with the provided email. 
        /// Finally, it logs the registration of the full teller and returns a success message along with the user's details.
        /// </remarks>

        [ForAuthenticatedTeller]
        public JsonResult AddFullTeller(string email, Guid election)
        {
            // ensure that I am the owner of the election
            var myJoin = Db.JoinElectionUser
              .FirstOrDefault(je => je.ElectionGuid == election && je.UserId == UserSession.UserGuid);
            if (myJoin == null || myJoin.Role != null)
            {
                return new
                {
                    Success = false,
                    Message = "Adding not allowed"
                }.AsJsonResult();
            }

            // don't bother checking if already invited -- no harm in duplicate and JS has screened

            // ensure that there is an Anonymous user
            var anonUser = Db.Users.FirstOrDefault(u => u.UserId == Guid.Empty);
            if (anonUser == null)
            {
                // get the applicationId
                var applicationId = Db.Users.Single(a => a.UserId == UserSession.UserGuid).ApplicationId;

                // create the anonymous user
                anonUser = new Users
                {
                    ApplicationId = applicationId,
                    UserId = Guid.Empty,
                    UserName = "PENDING",
                    IsAnonymous = true,
                    LastActivityDate = DateTime.UtcNow
                };
                Db.Users.Add(anonUser);
            }

            var jeu = new JoinElectionUser
            {
                ElectionGuid = election,
                UserId = Guid.Empty, // will be filled when the user logs in
                Role = "Full",
                InviteEmail = email,
            };

            Db.JoinElectionUser.Add(jeu);

            Db.SaveChanges();

            new LogHelper(election).Add($"Registered full teller - {email}", true);

            var user = new
            {
                jeu.Role,
                InviteWhen = (DateTime?)null,
                jeu.InviteEmail,
                jeu.C_RowId,
                Email = (string)null,
                UserName = "PENDING", // should match the default user in the DB. Okay if not.
                LastActivityDate = (DateTime?)null,
                isCurrentUser = false
            };

            return new
            {
                Success = true,
                user
            }.AsJsonResult();
        }

        /// <summary>
        /// Sends an invitation to a user for joining an election.
        /// </summary>
        /// <param name="joinId">The identifier of the join request for the election.</param>
        /// <returns>A JsonResult indicating the success or failure of the invitation process, along with an appropriate message.</returns>
        /// <remarks>
        /// This method first retrieves the join request associated with the provided <paramref name="joinId"/>. 
        /// If no join request is found, it returns a failure message indicating an unknown ID. 
        /// It then checks if the current user is the owner of the election associated with the join request. 
        /// If the user is not the owner or has a role assigned, it returns a failure message stating that only owners can send invitations. 
        /// Next, it verifies if the election is valid by checking against the user's elections. 
        /// If the election is invalid, it returns a failure message. 
        /// If all checks pass, it proceeds to send an invitation email to the specified address using the <see cref="EmailHelper"/> class.
        /// </remarks>

        [ForAuthenticatedTeller]
        public JsonResult SendInvitation(int joinId)
        {
            var join = Db.JoinElectionUser
              .FirstOrDefault(je => je.C_RowId == joinId);

            if (join == null)
            {
                return new
                {
                    Success = false,
                    Message = "Unknown id"
                }.AsJsonResult();
            }

            var targetElectionGuid = join.ElectionGuid;

            // ensure that I am the owner of the election
            var myJoin = Db.JoinElectionUser
              .FirstOrDefault(je => je.ElectionGuid == targetElectionGuid && je.UserId == UserSession.UserGuid);
            if (myJoin == null || myJoin.Role != null)
            {
                return new
                {
                    Success = false,
                    Message = "Only owners can send invitations"
                }.AsJsonResult();
            }

            var election = new ElectionsListViewModel()
              .MyElections()
              .FirstOrDefault(e => e.ElectionGuid == targetElectionGuid);
            if (election == null)
            {
                return new
                {
                    Success = false,
                    Message = "Invalid election"
                }.AsJsonResult();
            }

            return new EmailHelper().SendFullTellerInvitation(election, join.InviteEmail);
        }

    }
}