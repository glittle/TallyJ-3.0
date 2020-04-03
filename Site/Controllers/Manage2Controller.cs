// using System.Linq;
// using System.Threading.Tasks;
// using System.Web;
// using System.Web.Mvc;
// using Microsoft.AspNet.Identity;
// using Microsoft.AspNet.Identity.Owin;
// using Microsoft.Owin.Security;
// using TallyJ.Code;
// using TallyJ.Code.OwinRelated;
// using TallyJ.Code.Session;
// using TallyJ.CoreModels.VoterAccountModels;
//
// namespace TallyJ.Controllers
// {
//   [AllowVoter]
//   public class Manage2Controller : Controller
//   {
//     private ApplicationSignInManager _signInManager;
//     private ApplicationUserManager _userManager;
//     private string _userId;
//
//     public Manage2Controller()
//     {
//     }
//
//     //        public Manage2Controller(ApplicationUserManager userManager, ApplicationSignInManager signInManager)
//     //        {
//     //            UserManager = userManager;
//     //            SignInManager = signInManager;
//     //        }
//
//     public ApplicationSignInManager SignInManager
//     {
//       get
//       {
//         return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
//       }
//       private set
//       {
//         _signInManager = value;
//       }
//     }
//
//     public ApplicationUserManager UserManager
//     {
//       get
//       {
//         return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
//       }
//       private set
//       {
//         _userManager = value;
//       }
//     }
//
//     //
//     // GET: /Manage/Index
//     public async Task<ActionResult> Index(ManageMessageId? message)
//     {
//       ViewBag.StatusMessage =
//           message == ManageMessageId.ChangePasswordSuccess ? "Your password has been changed."
//           : message == ManageMessageId.SetPasswordSuccess ? "Your password has been set."
//           : message == ManageMessageId.SetTwoFactorSuccess ? "Your two-factor authentication provider has been set."
//           : message == ManageMessageId.Error ? "An error has occurred."
//           : message == ManageMessageId.AddPhoneSuccess ? "Your phone number was added."
//           : message == ManageMessageId.RemovePhoneSuccess ? "Your phone number was removed."
//           : "";
//
//       var userId = GetUserId();
//       var model = new IndexViewModel
//       {
//         HasPassword = HasPassword(),
//         PhoneNumber = await UserManager.GetPhoneNumberAsync(userId),
//         TwoFactor = await UserManager.GetTwoFactorEnabledAsync(userId),
//         Logins = await UserManager.GetLoginsAsync(userId),
//         BrowserRemembered = await AuthenticationManager.TwoFactorBrowserRememberedAsync(userId)
//       };
//       return View(model);
//     }
//
//     //
//     // POST: /Manage/RemoveLogin
//     [HttpPost]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> RemoveLogin(string loginProvider, string providerKey)
//     {
//       ManageMessageId? message;
//       var result = await UserManager.RemoveLoginAsync(GetUserId(), new UserLoginInfo(loginProvider, providerKey));
//       if (result.Succeeded)
//       {
//         var user = await UserManager.FindByIdAsync(GetUserId());
//         if (user != null)
//         {
//           await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
//         }
//         message = ManageMessageId.RemoveLoginSuccess;
//       }
//       else
//       {
//         message = ManageMessageId.Error;
//       }
//       return RedirectToAction("ManageLogins", new { Message = message });
//     }
//
//     //
//     // GET: /Manage/AddPhoneNumber
//     public ActionResult AddPhoneNumber()
//     {
//       return View();
//     }
//
//     //
//     // POST: /Manage/AddPhoneNumber
//     [HttpPost]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> AddPhoneNumber(AddPhoneNumberViewModel model)
//     {
//       if (!ModelState.IsValid)
//       {
//         return View(model);
//       }
//       // Generate the token and send it
//       var code = await UserManager.GenerateChangePhoneNumberTokenAsync(GetUserId(), model.Number);
//       if (UserManager.SmsService != null)
//       {
//         var message = new IdentityMessage
//         {
//           Destination = model.Number,
//           Body = "Your security code is: " + code
//         };
//         await UserManager.SmsService.SendAsync(message);
//       }
//       return RedirectToAction("VerifyPhoneNumber", new { PhoneNumber = model.Number });
//     }
//
//     //
//     // POST: /Manage/EnableTwoFactorAuthentication
//     [HttpPost]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> EnableTwoFactorAuthentication()
//     {
//       await UserManager.SetTwoFactorEnabledAsync(GetUserId(), true);
//       var user = await UserManager.FindByIdAsync(GetUserId());
//       if (user != null)
//       {
//         await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
//       }
//       return RedirectToAction("Index", "Manage2");
//     }
//
//     //
//     // POST: /Manage/DisableTwoFactorAuthentication
//     [HttpPost]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> DisableTwoFactorAuthentication()
//     {
//       await UserManager.SetTwoFactorEnabledAsync(GetUserId(), false);
//       var user = await UserManager.FindByIdAsync(GetUserId());
//       if (user != null)
//       {
//         await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
//       }
//       return RedirectToAction("Index", "Manage2");
//     }
//
//     //
//     // GET: /Manage/VerifyPhoneNumber
//     public async Task<ActionResult> VerifyPhoneNumber(string phoneNumber)
//     {
//       var code = await UserManager.GenerateChangePhoneNumberTokenAsync(GetUserId(), phoneNumber);
//       // Send an SMS through the SMS provider to verify the phone number
//       return phoneNumber == null ? View("Error") : View(new VerifyPhoneNumberViewModel { PhoneNumber = phoneNumber });
//     }
//
//     //
//     // POST: /Manage/VerifyPhoneNumber
//     [HttpPost]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> VerifyPhoneNumber(VerifyPhoneNumberViewModel model)
//     {
//       if (!ModelState.IsValid)
//       {
//         return View(model);
//       }
//       var result = await UserManager.ChangePhoneNumberAsync(GetUserId(), model.PhoneNumber, model.Code);
//       if (result.Succeeded)
//       {
//         var user = await UserManager.FindByIdAsync(GetUserId());
//         if (user != null)
//         {
//           await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
//         }
//         return RedirectToAction("Index", new { Message = ManageMessageId.AddPhoneSuccess });
//       }
//       // If we got this far, something failed, redisplay form
//       ModelState.AddModelError("", "Failed to verify phone");
//       return View(model);
//     }
//
//     //
//     // POST: /Manage/RemovePhoneNumber
//     [HttpPost]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> RemovePhoneNumber()
//     {
//       var result = await UserManager.SetPhoneNumberAsync(GetUserId(), null);
//       if (!result.Succeeded)
//       {
//         return RedirectToAction("Index", new { Message = ManageMessageId.Error });
//       }
//       var user = await UserManager.FindByIdAsync(GetUserId());
//       if (user != null)
//       {
//         await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
//       }
//       return RedirectToAction("Index", new { Message = ManageMessageId.RemovePhoneSuccess });
//     }
//
//     private string GetUserId()
//     {
//       return _userId ?? (_userId = UserManager.FindByName(UserSession.VoterEmail).Id);
//     }
//
//     //
//     // GET: /Manage/ChangePassword
//     public ActionResult ChangePassword()
//     {
//       return View();
//     }
//
//     //
//     // POST: /Manage/ChangePassword
//     [HttpPost]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> ChangePassword(ChangePasswordViewModel model)
//     {
//       if (!ModelState.IsValid)
//       {
//         return View(model);
//       }
//       var result = await UserManager.ChangePasswordAsync(GetUserId(), model.OldPassword, model.NewPassword);
//       if (result.Succeeded)
//       {
//         var user = await UserManager.FindByIdAsync(GetUserId());
//         if (user != null)
//         {
//           await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
//         }
//         return RedirectToAction("Index", "Vote", new { Message = ManageMessageId.ChangePasswordSuccess });
//       }
//       AddErrors(result);
//       return View(model);
//     }
//
//     //
//     // GET: /Manage/SetPassword
//     public ActionResult SetPassword()
//     {
//       return View();
//     }
//
//     //
//     // POST: /Manage/SetPassword
//     [HttpPost]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> SetPassword(SetPasswordViewModel model)
//     {
//       if (ModelState.IsValid)
//       {
//         var result = await UserManager.AddPasswordAsync(GetUserId(), model.NewPassword);
//         if (result.Succeeded)
//         {
//           var user = await UserManager.FindByIdAsync(GetUserId());
//           if (user != null)
//           {
//             await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
//           }
//           return RedirectToAction("Index", new { Message = ManageMessageId.SetPasswordSuccess });
//         }
//         AddErrors(result);
//       }
//
//       // If we got this far, something failed, redisplay form
//       return View(model);
//     }
//
//     //
//     // GET: /Manage/ManageLogins
//     public async Task<ActionResult> ManageLogins(ManageMessageId? message)
//     {
//       ViewBag.StatusMessage =
//           message == ManageMessageId.RemoveLoginSuccess ? "The external login was removed."
//           : message == ManageMessageId.Error ? "An error has occurred."
//           : "";
//       var user = await UserManager.FindByIdAsync(GetUserId());
//       if (user == null)
//       {
//         return View("Error");
//       }
//       var userLogins = await UserManager.GetLoginsAsync(GetUserId());
//       var otherLogins = AuthenticationManager.GetExternalAuthenticationTypes().Where(auth => userLogins.All(ul => auth.AuthenticationType != ul.LoginProvider)).ToList();
//       ViewBag.ShowRemoveButton = user.PasswordHash != null || userLogins.Count > 1;
//       return View(new ManageLoginsViewModel
//       {
//         CurrentLogins = userLogins,
//         OtherLogins = otherLogins
//       });
//     }
//
//     //
//     // POST: /Manage/LinkLogin
//     // [HttpPost]
//     // [ValidateAntiForgeryToken]
//     // public ActionResult LinkLogin(string provider)
//     // {
//     //   // Request a redirect to the external login provider to link a login for the current user
//     //   return new VoterAccountController.ChallengeResult(provider, Url.Action("LinkLoginCallback", "Manage2"), GetUserId());
//     // }
//
//     //
//     // GET: /Manage/LinkLoginCallback
//     public async Task<ActionResult> LinkLoginCallback()
//     {
//       var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync(XsrfKey, GetUserId());
//       if (loginInfo == null)
//       {
//         return RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
//       }
//       var result = await UserManager.AddLoginAsync(GetUserId(), loginInfo.Login);
//       return result.Succeeded ? RedirectToAction("ManageLogins") : RedirectToAction("ManageLogins", new { Message = ManageMessageId.Error });
//     }
//
//     protected override void Dispose(bool disposing)
//     {
//       if (disposing && _userManager != null)
//       {
//         _userManager.Dispose();
//         _userManager = null;
//       }
//
//       base.Dispose(disposing);
//     }
//
//     #region Helpers
//     // Used for XSRF protection when adding external logins
//     private const string XsrfKey = "XsrfId";
//
//     private IAuthenticationManager AuthenticationManager
//     {
//       get
//       {
//         return HttpContext.GetOwinContext().Authentication;
//       }
//     }
//
//     private void AddErrors(IdentityResult result)
//     {
//       foreach (var error in result.Errors)
//       {
//         ModelState.AddModelError("", error);
//       }
//     }
//
//     private bool HasPassword()
//     {
//       var user = UserManager.FindById(GetUserId());
//       if (user != null)
//       {
//         return user.PasswordHash != null;
//       }
//       return false;
//     }
//
//     private bool HasPhoneNumber()
//     {
//       var user = UserManager.FindById(GetUserId());
//       if (user != null)
//       {
//         return user.PhoneNumber != null;
//       }
//       return false;
//     }
//
//     public enum ManageMessageId
//     {
//       AddPhoneSuccess,
//       ChangePasswordSuccess,
//       SetTwoFactorSuccess,
//       SetPasswordSuccess,
//       RemoveLoginSuccess,
//       RemovePhoneSuccess,
//       Error
//     }
//
//     #endregion
//   }
// }