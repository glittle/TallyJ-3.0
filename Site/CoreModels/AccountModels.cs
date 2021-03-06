﻿using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Web.Security;

namespace TallyJ.CoreModels
{

  public class ChangePasswordModel
  {
    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Current password")]
    public string OldPassword
    {
      get;
      set;
    }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "New password")]
    public string NewPassword
    {
      get;
      set;
    }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm new password")]
    [Compare("NewPassword", ErrorMessage = "The new password and confirmation password do not match.")]
    public string ConfirmPassword
    {
      get;
      set;
    }
  }

  public class LogOnModelV1
  {
    [Required]
    //[RegularExpression(@"[^@.*]",ErrorMessage ="Email address not allowed")]
    [Display(Name = "User name")]
    public string UserName
    {
      get;
      set;
    }

    [Required]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string PasswordV1
    {
      get;
      set;
    }

    [Display(Name = "Remember me?")]
    public bool RememberMe
    {
      get;
      set;
    }
  }

  public class RegisterModel
  {
    [Required]
    [Display(Name = "Login ID")]
    [StringLength(50, ErrorMessage = "Login ID must be less than 50 characters long.")]
    public string UserName
    {
      get;
      set;
    }

    [Required]
    [DataType(DataType.EmailAddress)]
    [Display(Name = "Email address")]
    public string Email
    {
      get;
      set;
    }

    [Required]
    [StringLength(100, ErrorMessage = "The {0} must be at least {2} characters long.", MinimumLength = 6)]
    [DataType(DataType.Password)]
    [Display(Name = "Password")]
    public string Password
    {
      get;
      set;
    }

    [DataType(DataType.Password)]
    [Display(Name = "Confirm password")]
    [Compare("Password", ErrorMessage = "The password and confirmation password do not match.")]
    public string ConfirmPassword
    {
      get;
      set;
    }
  }
}
