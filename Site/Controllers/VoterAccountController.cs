﻿using System.Linq;
using System.Security.Claims;
using System.Web;
using System.Web.Mvc;
using Microsoft.AspNet.Identity;
using Microsoft.Owin.Security;
using Microsoft.Owin.Security.Cookies;
using MvcApplication.ViewModels;
using TallyJ.Code;
using TallyJ.Code.Session;

namespace TallyJ.Controllers
{
  public class VoterAccountController : Controller
  {
    public ActionResult LoginEmail(string returnUrl)
    {
      HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties
      {
        RedirectUri = returnUrl ?? Url.Action("Index", "Vote")
      }, "Auth0");
      return new HttpUnauthorizedResult();
    }

    public ActionResult LoginSms()
    {
      System.Web.HttpContext.Current.Items["CELL"] = true;

      HttpContext.GetOwinContext().Authentication.Challenge(new AuthenticationProperties
      {
        RedirectUri = Url.Action("Index", "Vote")
      }, "Auth0");
      return new HttpUnauthorizedResult();
    }

    // [Authorize]
    // public void Logout()
    // {
    //   HttpContext.GetOwinContext().Authentication.SignOut("Auth0", DefaultAuthenticationTypes.ExternalCookie, DefaultAuthenticationTypes.ApplicationCookie);
    //   // HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
    //   // HttpContext.GetOwinContext().Authentication.SignOut(DefaultAuthenticationTypes.ExternalCookie);
    //
    //   UserSession.ProcessLogout();
    // }


    // [Authorize]
    // public ActionResult UserProfile()
    // {
    //   var claimsIdentity = User.Identity as ClaimsIdentity;
    //
    //   var model = new UserProfileViewModel()
    //   {
    //     Name = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Name)?.Value,
    //     EmailAddress = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value,
    //     ProfileImage = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == "picture")?.Value,
    //     Country = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == "country")?.Value
    //   };
    //
    //   return null;
    // }

    // [Authorize]
    // public ActionResult Tokens()
    // {
    //   var claimsIdentity = User.Identity as ClaimsIdentity;
    //
    //   // Extract tokens
    //   string accessToken = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == "access_token")?.Value;
    //   string idToken = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == "id_token")?.Value;
    //   string refreshToken = claimsIdentity?.Claims.FirstOrDefault(c => c.Type == "refresh_token")?.Value;
    //
    //   // Save tokens in ViewBag
    //   ViewBag.AccessToken = accessToken;
    //   ViewBag.IdToken = idToken;
    //   ViewBag.RefreshToken = refreshToken;
    //
    //   return null;
    // }

    [Authorize]
    public ActionResult Claims()
    {
      return View();
    }
  }
}

namespace MvcApplication.ViewModels
{
  public class UserProfileViewModel
  {
    public string EmailAddress { get; set; }

    public string Name { get; set; }

    public string ProfileImage { get; set; }

    public string Country { get; set; }
  }
}

// using System;
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
// using TallyJ.EF;
//
// namespace TallyJ.Controllers
// {
//   [Authorize]
//   public class VoterAccountController : Controller
//   {
//     private ApplicationSignInManager _signInManager;
//     private ApplicationUserManager _userManager;
//
//     public VoterAccountController()
//     {
//     }
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
//     //        // GET: /Account/Login
//     //        [AllowAnonymous]
//     //        public ActionResult Login(string returnUrl)
//     //        {
//     //            ViewBag.ReturnUrl = returnUrl;
//     //            return View();
//     //        }
//
//     //
//     // POST: /Account/Login
//     //        [HttpPost]
//     //        [AllowAnonymous]
//     //        [ValidateAntiForgeryToken]
//     //        public async Task<ActionResult> Login(LoginViewModel model, string returnUrl)
//     //        {
//     //            if (!ModelState.IsValid)
//     //            {
//     //                return View(model);
//     //            }
//     //
//     //            // This doesn't count login failures towards account lockout
//     //            // To enable password failures to trigger account lockout, change to shouldLockout: true
//     //            var result = await SignInManager.PasswordSignInAsync(model.Email, model.Password, model.RememberMe, shouldLockout: true);
//     //            switch (result)
//     //            {
//     //                case SignInStatus.Success:
//     //                    return RedirectToLocal(returnUrl);
//     //                case SignInStatus.LockedOut:
//     //                    return View("Lockout");
//     //                case SignInStatus.RequiresVerification:
//     //                    return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = model.RememberMe });
//     //                case SignInStatus.Failure:
//     //                default:
//     //                    ModelState.AddModelError("", "Invalid login attempt.");
//     //                    return View(model);
//     //            }
//     //        }
//
//     //
//     // GET: /Account/VerifyCode
//     [AllowAnonymous]
//     public async Task<ActionResult> VerifyCode(string provider, string returnUrl, bool rememberMe)
//     {
//       // Require that the user has already logged in via username/password or external login
//       if (!await SignInManager.HasBeenVerifiedAsync())
//       {
//         return View("Error");
//       }
//       return View(new VerifyCodeViewModel { Provider = provider, ReturnUrl = returnUrl, RememberMe = rememberMe });
//     }
//
//     //
//     // POST: /Account/VerifyCode
//     [HttpPost]
//     [AllowAnonymous]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> VerifyCode(VerifyCodeViewModel model)
//     {
//       if (!ModelState.IsValid)
//       {
//         return View(model);
//       }
//
//       // The following code protects for brute force attacks against the two factor codes. 
//       // If a user enters incorrect codes for a specified amount of time then the user account 
//       // will be locked out for a specified amount of time. 
//       // You can configure the account lockout settings in IdentityConfig
//       var result = await SignInManager.TwoFactorSignInAsync(model.Provider, model.Code, isPersistent: model.RememberMe, rememberBrowser: model.RememberBrowser);
//       switch (result)
//       {
//         case SignInStatus.Success:
//           return RedirectToLocal(model.ReturnUrl);
//         case SignInStatus.LockedOut:
//           return View("Lockout");
//         case SignInStatus.Failure:
//         default:
//           ModelState.AddModelError("", "Invalid code.");
//           return View(model);
//       }
//     }
//
//     //
//     // GET: /Account/Register
//     [AllowAnonymous]
//     public ActionResult Register()
//     {
//       var model = new RegisterViewModel();
//       var voterEmail = UserSession.VoterEmail;
//       if (voterEmail.HasContent())
//       {
//         model.Email = voterEmail;
//       }
//       return View(model);
//     }
//
//     [AllowAnonymous]
//     public ActionResult Lockout()
//     {
//       return View();
//     }
//
//     //
//     // POST: /Account/Register
//     [HttpPost]
//     [AllowAnonymous]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> Register(RegisterViewModel model)
//     {
//       if (ModelState.IsValid)
//       {
//         var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
//         var result = await UserManager.CreateAsync(user, model.Password);
//         if (result.Succeeded)
//         {
//           //          await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
//
//           // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
//           // Send an email with this link
//           string code = await UserManager.GenerateEmailConfirmationTokenAsync(user.Id);
//
//           //          var hostSite = SettingsHelper.Get("HostSite", "");
//           //          var callbackUrl = $"{hostSite}/Account2/ConfirmEmail?userId={user.Id}&code={code}";
//           var callbackUrl = Url.Action("ConfirmEmail", "VoterAccount", new { userId = user.Id, code }, protocol: Request.Url.Scheme).FixSiteUrl();
//
//           await UserManager.SendEmailAsync(user.Id, "Confirm your account", $"<p>Hello,</p><p>Please confirm your account by clicking <a href=\"{callbackUrl}\">here</a>.</p>");
//
//           //          return RedirectToAction("Index", "Voter");
//           var msg = "An email has been sent to your account with a link you need to use to confirm your account. You must confirm it "
//                             + "before you can log in.";
//
//           Session[SessionKey.VoterLoginError] = msg;
//
//           return Redirect(Url.Action("Index", "Public").FixSiteUrl());
//         }
//         AddErrors(result);
//       }
//
//       // If we got this far, something failed, redisplay form
//       return View(model);
//     }
//
//     //
//     // GET: /Account/ConfirmEmail
//     [AllowAnonymous]
//     public async Task<ActionResult> ConfirmEmail(string userId, string code)
//     {
//       HandleErrorInfo model;
//
//       if (userId != null && code != null)
//       {
//         var result = await UserManager.ConfirmEmailAsync(userId, code);
//         if (result.Succeeded)
//         {
//           return View("ConfirmEmail");
//         }
//
//         model = new HandleErrorInfo(new ApplicationException(result.Errors.JoinedAsString("; ")), this.GetType().Name, "ConfirmEmail");
//       }
//       else
//       {
//         model = new HandleErrorInfo(new ApplicationException("Invalid request"), this.GetType().Name, "ConfirmEmail");
//       }
//       return View("Error", model);
//     }
//
//     //
//     // GET: /Account/ForgotPassword
//     [AllowAnonymous]
//     public ActionResult ForgotPassword()
//     {
//       return View();
//     }
//
//     //
//     // POST: /Account/ForgotPassword
//     [HttpPost]
//     [AllowAnonymous]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> ForgotPassword(ForgotPasswordViewModel model)
//     {
//       if (ModelState.IsValid)
//       {
//         var user = await UserManager.FindByNameAsync(model.Email);
//         if (user == null || !(await UserManager.IsEmailConfirmedAsync(user.Id)))
//         {
//           // Don't reveal that the user does not exist or is not confirmed
//           return View("ForgotPasswordConfirmation");
//         }
//
//         // For more information on how to enable account confirmation and password reset please visit https://go.microsoft.com/fwlink/?LinkID=320771
//         // Send an email with this link
//         string code = await UserManager.GeneratePasswordResetTokenAsync(user.Id);
//         var callbackUrl = Url.Action("ResetPassword", "VoterAccount", new { userId = user.Id, code = code }, protocol: Request.Url.Scheme).FixSiteUrl();
//         await UserManager.SendEmailAsync(user.Id, "Reset Password", "Please reset your password for TallyJ by clicking <a href=\"" + callbackUrl + "\">here</a>.");
//         return RedirectToAction("ForgotPasswordConfirmation", "VoterAccount");
//       }
//
//       // If we got this far, something failed, redisplay form
//       return View(model);
//     }
//
//     //
//     // GET: /Account/ForgotPasswordConfirmation
//     [AllowAnonymous]
//     public ActionResult ForgotPasswordConfirmation()
//     {
//       return View();
//     }
//
//     //
//     // GET: /Account/ResetPassword
//     [AllowAnonymous]
//     public ActionResult ResetPassword(string code)
//     {
//       return code == null ? View("Error") : View();
//     }
//
//     //
//     // POST: /Account/ResetPassword
//     [HttpPost]
//     [AllowAnonymous]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> ResetPassword(ResetPasswordViewModel model)
//     {
//       if (!ModelState.IsValid)
//       {
//         return View(model);
//       }
//       var user = await UserManager.FindByNameAsync(model.Email);
//       if (user == null)
//       {
//         // Don't reveal that the user does not exist
//         return RedirectToAction("ResetPasswordConfirmation", "VoterAccount");
//       }
//       var result = await UserManager.ResetPasswordAsync(user.Id, model.Code, model.Password);
//       if (result.Succeeded)
//       {
//         return RedirectToAction("ResetPasswordConfirmation", "VoterAccount");
//       }
//       AddErrors(result);
//       return View();
//     }
//
//     //
//     // GET: /Account/ResetPasswordConfirmation
//     [AllowAnonymous]
//     public ActionResult ResetPasswordConfirmation()
//     {
//       return View();
//     }
//
//     //
//     // POST: /Account/ExternalLogin
//     //    [HttpPost]
//     //    [AllowAnonymous]
//     //    [ValidateAntiForgeryToken]
//     //    public ActionResult ExternalLogin(string provider, string returnUrl)
//     //    {
//     //      // Request a redirect to the external login provider
//     //      return new ChallengeResult(provider, Url.Action("ExternalLoginCallback", "Account2", new { ReturnUrl = returnUrl }));
//     //    }
//
//     //
//     // GET: /Account/SendCode
//     [AllowAnonymous]
//     public async Task<ActionResult> SendCode(string returnUrl, bool rememberMe)
//     {
//       var userId = await SignInManager.GetVerifiedUserIdAsync();
//       if (userId == null)
//       {
//         return View("Error");
//       }
//       var userFactors = await UserManager.GetValidTwoFactorProvidersAsync(userId);
//       var factorOptions = userFactors.Select(purpose => new SelectListItem { Text = purpose, Value = purpose }).ToList();
//       return View(new SendCodeViewModel { Providers = factorOptions, ReturnUrl = returnUrl, RememberMe = rememberMe });
//     }
//
//     //
//     // POST: /Account/SendCode
//     [HttpPost]
//     [AllowAnonymous]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> SendCode(SendCodeViewModel model)
//     {
//       if (!ModelState.IsValid)
//       {
//         return View();
//       }
//
//       // Generate the token and send it
//       if (!await SignInManager.SendTwoFactorCodeAsync(model.SelectedProvider))
//       {
//         return View("Error");
//       }
//       return RedirectToAction("VerifyCode", new { Provider = model.SelectedProvider, ReturnUrl = model.ReturnUrl, RememberMe = model.RememberMe });
//     }
//
//     //
//     // GET: /Account/ExternalLoginCallback
//     [AllowAnonymous]
//     public async Task<ActionResult> ExternalLoginCallback(string returnUrl)
//     {
//       var loginInfo = await AuthenticationManager.GetExternalLoginInfoAsync();
//       if (loginInfo == null)
//       {
//         return RedirectToAction("Index", "Public");
//       }
//
//       // Sign in the user with this external login provider if the user already has a login
//       var result = await SignInManager.ExternalSignInAsync(loginInfo, isPersistent: false);
//       switch (result)
//       {
//         case SignInStatus.Success:
//           return RedirectToLocal(returnUrl);
//         case SignInStatus.LockedOut:
//           return View("Lockout");
//         case SignInStatus.RequiresVerification:
//           return RedirectToAction("SendCode", new { ReturnUrl = returnUrl, RememberMe = false });
//         case SignInStatus.Failure:
//         default:
//           // If the user does not have an account, then prompt the user to create an account
//           ViewBag.ReturnUrl = returnUrl;
//           ViewBag.LoginProvider = loginInfo.Login.LoginProvider;
//           return View("ExternalLoginConfirmation", new ExternalLoginConfirmationViewModel { Email = loginInfo.Email });
//       }
//     }
//
//     //
//     // POST: /Account/ExternalLoginConfirmation
//     [HttpPost]
//     [AllowAnonymous]
//     [ValidateAntiForgeryToken]
//     public async Task<ActionResult> ExternalLoginConfirmation(ExternalLoginConfirmationViewModel model, string returnUrl)
//     {
//       if (UserSession.IsAuthenticated)
//       {
//         return RedirectToAction("Index", "Manage2");
//       }
//
//       if (ModelState.IsValid)
//       {
//         // Get the information about the user from the external login provider
//         var info = await AuthenticationManager.GetExternalLoginInfoAsync();
//         if (info == null)
//         {
//           return View("ExternalLoginFailure");
//         }
//         var user = new ApplicationUser { UserName = model.Email, Email = model.Email };
//         var result = await UserManager.CreateAsync(user);
//         if (result.Succeeded)
//         {
//           result = await UserManager.AddLoginAsync(user.Id, info.Login);
//           if (result.Succeeded)
//           {
//             await SignInManager.SignInAsync(user, isPersistent: false, rememberBrowser: false);
//             return RedirectToLocal(returnUrl);
//           }
//         }
//         AddErrors(result);
//       }
//
//       ViewBag.ReturnUrl = returnUrl;
//       return View(model);
//     }
//
//     //
//     // POST: /Account/LogOff
//     [HttpPost]
//     [ValidateAntiForgeryToken]
//     public ActionResult LogOff()
//     {
//       AuthenticationManager.SignOut(DefaultAuthenticationTypes.ApplicationCookie);
//       return RedirectToAction("Index", "Public");
//     }
//
//     //
//     // GET: /Account/ExternalLoginFailure
//     [AllowAnonymous]
//     public ActionResult ExternalLoginFailure()
//     {
//       return View();
//     }
//
//     protected override void Dispose(bool disposing)
//     {
//       if (disposing)
//       {
//         if (_userManager != null)
//         {
//           _userManager.Dispose();
//           _userManager = null;
//         }
//
//         if (_signInManager != null)
//         {
//           _signInManager.Dispose();
//           _signInManager = null;
//         }
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
//         var msg = error;
//         if (msg == "Passwords must have at least one non letter or digit character.")
//         {
//           msg = "Passwords must have at least one special character that is not a letter or digit.";
//         }
//         ModelState.AddModelError("", msg);
//       }
//     }
//
//     private ActionResult RedirectToLocal(string returnUrl)
//     {
//       if (Url.IsLocalUrl(returnUrl))
//       {
//         return Redirect(returnUrl);
//       }
//       return RedirectToAction("Index", "Voter");
//     }
//
//     internal class ChallengeResult : HttpUnauthorizedResult
//     {
//       public ChallengeResult(string provider, string redirectUri)
//           : this(provider, redirectUri, null)
//       {
//       }
//
//       public ChallengeResult(string provider, string redirectUri, string userId)
//       {
//         LoginProvider = provider;
//         RedirectUri = redirectUri;
//         UserId = userId;
//       }
//
//       public string LoginProvider { get; set; }
//       public string RedirectUri { get; set; }
//       public string UserId { get; set; }
//
//       public override void ExecuteResult(ControllerContext context)
//       {
//         var properties = new AuthenticationProperties { RedirectUri = RedirectUri };
//         if (UserId != null)
//         {
//           properties.Dictionary[XsrfKey] = UserId;
//         }
//         context.HttpContext.GetOwinContext().Authentication.Challenge(properties, LoginProvider);
//       }
//     }
//     #endregion
//   }
// }