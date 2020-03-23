using System;
using System.Linq;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TallyJ.Code;
using TallyJ.Code.OwinRelated;
using TallyJ.Code.Session;
using TallyJ.CoreModels.VoterAccountModels;

namespace TallyJ.Controllers.LoginHelpers
{
  public class LoginHelper
  {
    private readonly string _homeUrl;
    private readonly string _mustProvideYourTallyjPassword;
    private readonly string _loginSource;
    private ModelStateDictionary ModelState { get; }
    public Func<string, string, string> GetConfirmEmailUrl { get; }
    public Func<ActionResult> GetLogoutResult { get; }
    public Func<ActionResult> GetSendCodeResult { get; }
    private ApplicationSignInManager _signInManager;
    private ApplicationUserManager _userManager;

    public LoginHelper(ModelStateDictionary modelState,
      string homeUrl,
      string mustProvideYourTallyjPassword,
      string loginSource,
      Func<string, string, string> getConfirmEmailUrl,
      Func<ActionResult> getLogoutResult,
      Func<ActionResult> getSendCodeResult)
    {
      _homeUrl = homeUrl;
      _mustProvideYourTallyjPassword = mustProvideYourTallyjPassword;
      _loginSource = loginSource;
      ModelState = modelState;
      GetConfirmEmailUrl = getConfirmEmailUrl;
      GetLogoutResult = getLogoutResult;
      GetSendCodeResult = getSendCodeResult;
    }

    //
    private ApplicationUserManager UserManager => _userManager ?? (_userManager = HttpContext.Current.GetOwinContext().GetUserManager<ApplicationUserManager>());
    private ApplicationSignInManager SignInManager2 => _signInManager ?? (_signInManager = HttpContext.Current.GetOwinContext().Get<ApplicationSignInManager>());

    public static void StoreModelStateErrorMessagesInSession(ModelStateDictionary modelState)
    {
      SessionKey.VoterLoginError.SetInSession(modelState.Values.SelectMany(msv => msv.Errors.Select(msv2 => msv2.ErrorMessage)).JoinedAsString("<br>"));
    }

    //        [AllowAnonymous]
    //        public async Task<ActionResult> XExternalLoginCallback(string returnUrl)
    //        {
    //            Session["Dummy"] = 1; // touch Session so that OWIN cookies work!
    //            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync("XsrfKey", AppSettings["XsrfValue"]);
    //            if (loginInfo == null)
    //                return RedirectToAction("Index", "Public");
    //
    //            var providerKey = loginInfo.Login.ProviderKey;
    //
    //
    //            // Application specific code goes here.
    //            //            var userBus = new busUser();
    //            //            var user = userBus.ValidateUserWithExternalLogin(providerKey);
    //            //            if (user == null)
    //            //            {
    //            //                return RedirectToAction("LogOn", new
    //            //                {
    //            //                    message = "Unable to log in with " + loginInfo.Login.LoginProvider +
    //            //                              ". " + userBus.ErrorMessage
    //            //                });
    //            //            }
    //
    //            // store on AppUser
    //            AppUserState appUserState = new AppUserState();
    //            //            appUserState.FromUser(user);
    //            IdentitySignin(appUserState, providerKey, isPersistent: true);
    //
    //            return Redirect(returnUrl);
    //        }


    public async Task<ActionResult> LocalPwLogin(LoginViewModel model, string returnUrl)
    {
      var owinContext = HttpContext.Current.GetOwinContext();

      var result = SignInManager2.PasswordSignInAsync(model.Email, model.Password, false, shouldLockout: true).Result;

      switch (result)
      {
        case SignInStatus.Success:
          var user = await UserManager.FindByNameAsync(model.Email);

          var userid = UserManager.FindByEmail(user.UserName).Id;
          
          //TODO when adding password, mark email as Confirmed

          if (!UserManager.IsEmailConfirmed(userid))
          {
            // Send email
            var code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
            var callbackUrl = GetConfirmEmailUrl(user.Id, code);
            await UserManager.SendEmailAsync(user.Id, "Confirm your account",
              $"Please confirm your account by clicking <a href=\"{callbackUrl}\">this link</a>.");

            // Show message
            ModelState.AddModelError("", "An email has been sent to your account with a link you need to use to confirm your account.");
            StoreModelStateErrorMessagesInSession(ModelState);

            return new RedirectResult(_homeUrl);
          }

          var claimsIdentity = owinContext.Authentication.AuthenticationResponseGrant.Identity;
          owinContext.Authentication.SignIn(new AuthenticationProperties()
          {
            AllowRefresh = true,
            IsPersistent = false,
            ExpiresUtc = DateTime.UtcNow.AddHours(1)
          }, claimsIdentity);

          SessionKey.ActivePrincipal.SetInSession(owinContext.Authentication.AuthenticationResponseGrant.Principal);

          var source = _loginSource;
          if (source != "Local")
          {
            source += " (with password)";
          }
          UserSession.RecordVoterLogin(source, UserSession.VoterEmail);

          return new RedirectResult(returnUrl);

        case SignInStatus.LockedOut:
          StoreModelStateErrorMessagesInSession(ModelState);
          new LogHelper().Add($"Voter locked out (from {_loginSource})", true, model.Email);
          return GetLogoutResult();

        case SignInStatus.RequiresVerification:
          // is this used now?
          StoreModelStateErrorMessagesInSession(ModelState);
          return GetSendCodeResult();

        case SignInStatus.Failure:
        default:
          ModelState.AddModelError("", "Invalid login attempt." + _mustProvideYourTallyjPassword);
          StoreModelStateErrorMessagesInSession(ModelState);
          new LogHelper().Add($"Failed voter login attempt from {_loginSource}", true, model.Email);


          return new RedirectResult(_homeUrl);
      }
    }

  }
}