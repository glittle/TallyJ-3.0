﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.Owin;
using Microsoft.Owin.Security;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.OwinRelated;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using TallyJ.CoreModels.Account2Models;
using static System.Configuration.ConfigurationManager;

namespace TallyJ.Controllers
{
  //[Authorize]
  public class AccountController : Controller
  {
    private ApplicationSignInManager _signInManager;

    private ApplicationUserManager _userManager;
    //
    // GET: /Account/LogOn

    [AllowAnonymous]
    public ActionResult LogOn()
    {
      return ContextDependentView();
    }

    public void IdentitySignout()
    {
      HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie,
          DefaultAuthenticationTypes.ExternalCookie);
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<ActionResult> LogOnLocal(LoginViewModel loginViewModel)
    {
      var voterHomeUrl = Url.Action("Index", "Voter");
      if (loginViewModel.Provider != "Local")
      {
        return Redirect(Url.Action("Index", "Public"));
      }
      return await LocalPwLogin(loginViewModel, voterHomeUrl);
    }

    [AllowAnonymous]
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult LogOnExt(LoginExtViewModel loginViewModel)
    {
      var voterHomeUrl = Url.Action("Index", "Voter");
      return new ChallengeResult(loginViewModel.Provider, voterHomeUrl, AppSettings["XsrfValue"]);
    }

    public async Task<ActionResult> LocalPwLogin(LoginViewModel model, string returnUrl)
    {
      if (!ModelState.IsValid)
      {
        StoreModelStateErrorMessagesInSession();
        return Redirect(returnUrl);
        //return View(model);
      }

      var owinContext = HttpContext.GetOwinContext();

      var result = SignInManager2.PasswordSignInAsync(model.Email, model.Password, false, shouldLockout: true).Result;

      switch (result)
      {
        case SignInStatus.Success:
          var user = await UserManager.FindByNameAsync(model.Email);

          var userid = UserManager.FindByEmail(user.UserName).Id;
          if (!UserManager.IsEmailConfirmed(userid))
          {
            // Send email
            var code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
            var callbackUrl = Url.Action("ConfirmEmail", "Account2", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme);
            await UserManager.SendEmailAsync(user.Id, "Confirm your account",
              $"Please confirm your account by clicking <a href=\"{callbackUrl}\">this link</a>.");

            // Show message
            ModelState.AddModelError("", "An email has been sent to your account with a link you need to use to confirm your account.");
            StoreModelStateErrorMessagesInSession();

            return Redirect(Url.Action("Index", "Public"));
          }

          var claimsIdentity = owinContext.Authentication.AuthenticationResponseGrant.Identity;
          owinContext.Authentication.SignIn(new AuthenticationProperties()
          {
            AllowRefresh = true,
            IsPersistent = false,
            ExpiresUtc = DateTime.UtcNow.AddHours(1)
          }, claimsIdentity);

          SessionKey.ActivePrincipal.SetInSession(owinContext.Authentication.AuthenticationResponseGrant.Principal);

          UserSession.RecordLogin("Local", UserSession.VoterEmail);

          return Redirect(returnUrl);
        case SignInStatus.LockedOut:
          StoreModelStateErrorMessagesInSession();
          return RedirectToAction("Lockout", "Account2");

        case SignInStatus.RequiresVerification:
          StoreModelStateErrorMessagesInSession();
          return RedirectToAction("SendCode", "Account2", new { ReturnUrl = returnUrl }); // , RememberMe = model.RememberMe

        case SignInStatus.Failure:
        default:
          ModelState.AddModelError("", "Invalid login attempt.");
          StoreModelStateErrorMessagesInSession();

          return Redirect(Url.Action("Index", "Public"));
      }
    }

    public ApplicationUserManager UserManager
    {
      get
      {
        return _userManager ?? HttpContext.GetOwinContext().GetUserManager<ApplicationUserManager>();
      }
      private set
      {
        _userManager = value;
      }
    }

    private ApplicationSignInManager SignInManager
    {
      get
      {
        return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
      }
      set
      {
        _signInManager = value;
      }
    }
    private void StoreModelStateErrorMessagesInSession()
    {
      Session[SessionKey.VoterLoginError] = ModelState.Values.SelectMany(msv => msv.Errors.Select(msv2 => msv2.ErrorMessage)).JoinedAsString("<br>");
    }

    public ApplicationSignInManager SignInManager2
    {
      get
      {
        return _signInManager ?? HttpContext.GetOwinContext().Get<ApplicationSignInManager>();
      }
      private set
      {
        _signInManager = value;
      }
    }

    //        [AllowAnonymous]
    //        public async Task<ActionResult> XExternalLoginCallback(string returnUrl)
    //        {
    //            Session["Dummy"] = 1; // touch Session so that OWIN cookies work!
    //            var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync("XsrfKey", AppSettings["XsrfValue"]);
    //            if (loginInfo == null)
    //                return RedirectToAction("Index", "Public");
    //
    //            // AUTHENTICATED!
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


    //[AllowAnonymous]
    //[HttpPost]
    //public JsonResult JsonLogOn(LogOnModel model, string returnUrl)
    //{
    //  if (ModelState.IsValid)
    //  {
    //    if (Membership.ValidateUser(model.UserName, model.Password))
    //    {
    //      FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
    //      UserSession.ProcessLogin();
    //      UserSession.IsKnownTeller = true;

    //      return Json(new
    //      {
    //        success = true,
    //        redirect = returnUrl
    //      });
    //    }
    //    else
    //    {
    //      ModelState.AddModelError("", "The user name or password provided is incorrect.");
    //    }
    //  }

    //  // If we got this far, something failed
    //  return Json(new
    //  {
    //    errors = GetErrorsFromModelState()
    //  });
    //}

    //
    // POST: /Account/LogOn

    //[AllowAnonymous]
    //public ActionResult AutoLogOn(string returnUrl)
    //{
    //    AssertAtRuntime.That(new SiteInfo().CurrentDataSource==DataSource.SingleElectionXml);

    //}

    [AllowAnonymous]
    [HttpPost]
    public ActionResult LogOn(LogOnModelV1 model, string returnUrl)
    {
      if (ModelState.IsValid)
      {
        if (Membership.ValidateUser(model.UserName, model.PasswordV1))
        {
          //                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);

          UserSession.ProcessLogin();

          var claims = new List<Claim>
                    {
                        new Claim("UserName", model.UserName),
                        new Claim("UniqueID", model.UserName),
//                        new Claim("Email", email),
                        new Claim("IsKnownTeller", "true"),
                    };

          var identity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ExternalCookie);

          var authenticationProperties = new AuthenticationProperties()
          {
            AllowRefresh = true,
            IsPersistent = false,
            ExpiresUtc = DateTime.UtcNow.AddDays(7)
          };

          System.Web.HttpContext.Current.GetOwinContext().Authentication.SignIn(authenticationProperties, identity);

          var email = Membership.GetUser(model.UserName)?.Email;

          new LogHelper().Add("Logged In - {0} ({1})".FilledWith(model.UserName, email), true);

          UserSession.IsKnownTeller = true;

          if (Url.IsLocalUrl(returnUrl))
          {
            return Redirect(returnUrl);
          }

          return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError("", "The user name or password provided is incorrect.");
      }

      // If we got this far, something failed, redisplay form
      return View(model);
    }

    //
    // GET: /Account/LogOff

    [AllowAnonymous]
    public ActionResult LogOff()
    {
      HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie,
          DefaultAuthenticationTypes.ExternalCookie);

      UserSession.ProcessLogout();

      return RedirectToAction("Index", "Public");
    }

    [AllowAnonymous]
    public ActionResult LogOut()
    {
      return LogOff();
    }

    //
    // GET: /Account/Register

    [AllowAnonymous]
    public ActionResult Register()
    {
      return ContextDependentView();
    }

    //
    // POST: /Account/JsonRegister

    //[AllowAnonymous]
    //[HttpPost]
    //public ActionResult JsonRegister(RegisterModel model)
    //{
    //  if (ModelState.IsValid)
    //  {
    //    // Attempt to register the user
    //    MembershipCreateStatus createStatus;
    //    Membership.CreateUser(model.UserName, model.Password, model.Email, null, null, true, null, out createStatus);

    //    if (createStatus == MembershipCreateStatus.Success)
    //    {
    //      FormsAuthentication.SetAuthCookie(model.UserName, createPersistentCookie: false);
    //      return Json(new
    //      {
    //        success = true
    //      });
    //    }
    //    else
    //    {
    //      ModelState.AddModelError("", ErrorCodeToString(createStatus));
    //    }
    //  }

    //  // If we got this far, something failed
    //  return Json(new
    //  {
    //    errors = GetErrorsFromModelState()
    //  });
    //}

    //
    // POST: /Account/Register

    [AllowAnonymous]
    [HttpPost]
    public ActionResult Register(RegisterModel model)
    {
      if (ModelState.IsValid)
      {
        // Attempt to register the user
        MembershipCreateStatus createStatus;
        Membership.CreateUser(model.UserName, model.Password, model.Email, null, null, true, null,
            out createStatus);

        if (createStatus == MembershipCreateStatus.Success)
        {
          var claims = new List<Claim>
                    {
                        new Claim("UserName", model.UserName),
                        new Claim("UniqueID", model.UserName),
//                        new Claim("Email", model.Email),
                        new Claim("IsKnownTeller", "true"),
                    };

          var identity = new ClaimsIdentity(claims, DefaultAuthenticationTypes.ExternalCookie);

          var authenticationProperties = new AuthenticationProperties()
          {
            AllowRefresh = true,
            IsPersistent = false,
            ExpiresUtc = DateTime.UtcNow.AddDays(7)
          };

          System.Web.HttpContext.Current.GetOwinContext().Authentication.SignIn(authenticationProperties, identity);

          //                    FormsAuthentication.SetAuthCookie(model.UserName, true);
          UserSession.ProcessLogin();
          UserSession.IsKnownTeller = true;

          return RedirectToAction("Index", "Dashboard");
        }
        else
        {
          ModelState.AddModelError("", ErrorCodeToString(createStatus));
        }
      }

      // If we got this far, something failed, redisplay form
      return View(model);
    }

    //
    // GET: /Account/ChangePassword

    [AllowAnonymous]
    public ActionResult ChangePassword()
    {
      return View();
    }

    //
    // POST: /Account/ChangePassword

    [HttpPost]
    public ActionResult ChangePassword(ChangePasswordModel model)
    {
      if (ModelState.IsValid)
      {
        // ChangePassword will throw an exception rather
        // than return false in certain failure scenarios.
        bool changePasswordSucceeded;
        try
        {
          var currentUser = Membership.GetUser(User.Identity.Name, userIsOnline: true);
          changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
        }
        catch (Exception)
        {
          changePasswordSucceeded = false;
        }

        if (changePasswordSucceeded)
        {
          return RedirectToAction("ChangePasswordSuccess");
        }
        else
        {
          ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
        }
      }

      // If we got this far, something failed, redisplay form
      return View(model);
    }

    //
    // GET: /Account/ChangePasswordSuccess

    public ActionResult ChangePasswordSuccess()
    {
      return View();
    }

    [AllowAnonymous]
    private ActionResult ContextDependentView(string overrideActionName = "")
    {
      var actionName = overrideActionName.DefaultTo(ControllerContext.RouteData.GetRequiredString("action"));
      if (Request.QueryString["content"] != null)
      {
        ViewBag.FormAction = "Json" + actionName;
        return PartialView();
      }
      else
      {
        ViewBag.FormAction = actionName;
        return View();
      }
    }

    //        private IEnumerable<string> GetErrorsFromModelState()
    //        {
    //            return ModelState.SelectMany(x => x.Value.Errors
    //                .Select(error => error.ErrorMessage));
    //        }

    #region Status Codes

    private static string ErrorCodeToString(MembershipCreateStatus createStatus)
    {
      // See http://go.microsoft.com/fwlink/?LinkID=177550 for
      // a full list of status codes.
      switch (createStatus)
      {
        case MembershipCreateStatus.DuplicateUserName:
          return "User name already exists. Please enter a different user name.";

        case MembershipCreateStatus.DuplicateEmail:
          return
              "A user name for that e-mail address already exists. Please enter a different e-mail address.";

        case MembershipCreateStatus.InvalidPassword:
          return "The password provided is invalid. Please enter a valid password value.";

        case MembershipCreateStatus.InvalidEmail:
          return "The e-mail address provided is invalid. Please check the value and try again.";

        case MembershipCreateStatus.InvalidAnswer:
          return "The password retrieval answer provided is invalid. Please check the value and try again.";

        case MembershipCreateStatus.InvalidQuestion:
          return "The password retrieval question provided is invalid. Please check the value and try again.";

        case MembershipCreateStatus.InvalidUserName:
          return "The user name provided is invalid. Please check the value and try again.";

        case MembershipCreateStatus.ProviderError:
          return
              "The authentication provider returned an error. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

        case MembershipCreateStatus.UserRejected:
          return
              "The user creation request has been canceled. Please verify your entry and try again. If the problem persists, please contact your system administrator.";

        default:
          return
              "An unknown error occurred. Please verify your entry and try again. If the problem persists, please contact your system administrator.";
      }
    }

    #endregion
  }
}