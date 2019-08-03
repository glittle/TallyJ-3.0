using System.Collections.Generic;
using Microsoft.Owin.Security.Facebook;

namespace TallyJ.Code.OwinRelated
{
  public class CustomFacebookAuthenticationOptions : FacebookAuthenticationOptions
  {
    //        private ICollection<string> _fields;

    /// <summary>
    /// Initializes a new <see cref="T:Microsoft.Owin.Security.Facebook.FacebookAuthenticationOptions" />
    /// </summary>
    public CustomFacebookAuthenticationOptions()
        : base()
    {
      this.Scope.Clear();
      this.Scope.Add("email");

      this.Fields.Clear();
      this.Fields.Add("email");
    }
  }
}