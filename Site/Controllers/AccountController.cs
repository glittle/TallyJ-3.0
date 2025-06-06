﻿using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using System.Web.Security;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using TallyJ.Code;
using TallyJ.Code.Session;
using TallyJ.CoreModels;
using static System.Configuration.ConfigurationManager;

namespace TallyJ.Controllers
{
  //[Authorize]
  public class AccountController : Controller
  {
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

    // [AllowAnonymous]
    // [HttpPost]
    // [ValidateAntiForgeryToken]
    // public async Task<ActionResult> LogOnLocal(LoginViewModel loginViewModel)
    // {
    //   var voterHomeUrl = Url.Action("Index", "Vote").FixSiteUrl();
    //   var homeUrl = Url.Action("Index", "Public").FixSiteUrl();
    //   if (loginViewModel.Provider != "Local")
    //   {
    //     return Redirect(homeUrl);
    //   }
    //
    //   if (!ModelState.IsValid)
    //   {
    //     LoginHelper.StoreModelStateErrorMessagesInSession(ModelState);
    //     return new RedirectResult(homeUrl);
    //     //return View(model);
    //   }
    //
    //   var helpers = new LoginHelper(ModelState,
    //     homeUrl,
    //     "",
    //     "Local",
    //     (id, code) => { return Url.Action("ConfirmEmail", "VoterAccount", new { userId = id, code }, protocol: HttpContext.Request.Url.Scheme).FixSiteUrl(); },
    //     () => RedirectToAction("Lockout", "VoterAccount"),
    //     () => RedirectToAction("SendCode", "VoterAccount", new { ReturnUrl = voterHomeUrl }));
    //   return await helpers.LocalPwLogin(loginViewModel, voterHomeUrl);
    // }

    // [AllowAnonymous]
    // [HttpPost]
    // [ValidateAntiForgeryToken]
    // public ActionResult LogOnExt(LoginExtViewModel loginViewModel)
    // {
    //   var voterHomeUrl = Url.Action("Index", "Vote").FixSiteUrl();
    //   // SessionKey.ExtPassword.SetInSession(loginViewModel.ExtPassword);
    //
    //   return new ChallengeResult(loginViewModel.Provider, voterHomeUrl, AppSettings["XsrfValue"]);
    // }


    [AllowAnonymous]
    [HttpPost]
    public ActionResult LogOn(LogOnModelV1 model, string returnUrl)
    {
      UserSession.UserGuidHasBeenLoaded = false;

      if (ModelState.IsValid)
      {
        if (Membership.ValidateUser(model.UserName, model.PasswordV1))
        {
          //                    FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);

          var membershipUser = Membership.GetUser(model.UserName);
          var email = membershipUser?.Email;

          // UserSession.ProcessLogin();

          var claims = new List<Claim>
          {
            new("UserName", model.UserName),
            new("UniqueID", "A:" + model.UserName),
            new("IsKnownTeller", "true")
          };

          UserSession.IsKnownTeller = true;
          UserSession.AdminAccountEmail = email;

          if (membershipUser?.Comment == "SysAdmin") claims.Add(new Claim("IsSysAdmin", "true"));

          var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationType);

          var authenticationProperties = new AuthenticationProperties
          {
            AllowRefresh = true,
            IsPersistent = false,
            ExpiresUtc = DateTime.UtcNow.AddDays(7)
          };

          System.Web.HttpContext.Current.GetOwinContext().Authentication.SignIn(authenticationProperties, identity);

          new LogHelper().Add("Admin Logged In - {0} ({1})".FilledWith(model.UserName, email), true);

          if (Url.IsLocalUrl(returnUrl)) return Redirect(returnUrl);

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
      HttpContext.GetOwinContext().Authentication.SignOut(
        DefaultAuthenticationTypes.ExternalCookie,
        DefaultAuthenticationTypes.ApplicationCookie);

      // ensure that the cookie is gone!
      Response.Cookies.Add(new HttpCookie(".AspNet.Cookies")
      {
        Secure = AppSettings["secure"].AsBoolean(true),
        Expires = DateTime.Now.AddDays(-99)
      });

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
        var membershipUser = Membership.CreateUser(model.UserName, model.Password, model.Email, null, null, true, null,
          out var createStatus);

        if (createStatus == MembershipCreateStatus.Success)
        {
          var email = membershipUser?.Email;
         
          var claims = new List<Claim>
          {
            new("UserName", model.UserName),
            new("UniqueID", "A:" + model.UserName),
            new("IsKnownTeller", "true")
          };

          UserSession.IsKnownTeller = true;
          UserSession.AdminAccountEmail = email;

          var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationType);

          var authenticationProperties = new AuthenticationProperties
          {
            AllowRefresh = true,
            IsPersistent = false,
            ExpiresUtc = DateTime.UtcNow.AddDays(7)
          };

          System.Web.HttpContext.Current.GetOwinContext().Authentication.SignIn(authenticationProperties, identity);

          new LogHelper().Add("Admin Registered - {0} ({1})".FilledWith(model.UserName, email), true);

          new ElectionsListViewModel().CheckIfAddedToNew();

          return RedirectToAction("Index", "Dashboard");
        }

        ModelState.AddModelError("", ErrorCodeToString(createStatus));
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
          var currentUser = Membership.GetUser(UserSession.LoginId, true);
          changePasswordSucceeded = currentUser.ChangePassword(model.OldPassword, model.NewPassword);
        }
        catch (Exception)
        {
          changePasswordSucceeded = false;
        }

        if (changePasswordSucceeded)
          return RedirectToAction("ChangePasswordSuccess");
        ModelState.AddModelError("", "The current password is incorrect or the new password is invalid.");
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

      ViewBag.FormAction = actionName;
      return View();
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