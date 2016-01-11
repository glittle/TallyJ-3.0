using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using System.Web.Security;
using TallyJ.Code;
using TallyJ.Code.Helpers;
using TallyJ.Code.Session;
using TallyJ.CoreModels;

namespace TallyJ.Controllers
{
  //[Authorize]
  public class AccountController : Controller
  {
    //
    // GET: /Account/LogOn

    [AllowAnonymous]
    public ActionResult LogOn()
    {
      return ContextDependentView();
    }

    //
    // POST: /Account/JsonLogOn

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
    public ActionResult LogOn(LogOnModel model, string returnUrl)
    {
      if (ModelState.IsValid)
      {
        if (Membership.ValidateUser(model.UserName, model.Password))
        {
          FormsAuthentication.SetAuthCookie(model.UserName, model.RememberMe);
          UserSession.ProcessLogin();
          new LogHelper().Add("Logged In", true);

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
          FormsAuthentication.SetAuthCookie(model.UserName, true);
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

    private IEnumerable<string> GetErrorsFromModelState()
    {
      return ModelState.SelectMany(x => x.Value.Errors
          .Select(error => error.ErrorMessage));
    }

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