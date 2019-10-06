using System;
using System.Data.Entity;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Web;
using Evernote.EDAM.Type;
using Microsoft.AspNet.Identity;
using Microsoft.AspNet.Identity.EntityFramework;
using Microsoft.Owin.Security;
using TallyJ.Code.Session;
using TallyJ.EF;

namespace TallyJ.CoreModels.Account2Models
{
  // You can add profile data for the user by adding more properties to your ApplicationUser class, please visit https://go.microsoft.com/fwlink/?LinkID=317594 to learn more.
  public class ApplicationUser : IdentityUser
  {
    public async Task<ClaimsIdentity> GenerateUserIdentityAsync(UserManager<ApplicationUser> manager)
    {
      await manager.AddClaimAsync(this.Id, new Claim("Source", "Local"));
      await manager.AddClaimAsync(this.Id, new Claim("Email", Email));
      await manager.AddClaimAsync(this.Id, new Claim("UniqueID", Email));
      await manager.AddClaimAsync(this.Id, new Claim("IsVoter", "True"));

      // Note the authenticationType must match the one defined in CookieAuthenticationOptions.AuthenticationType
      var userIdentity = await manager.CreateIdentityAsync(this, DefaultAuthenticationTypes.ApplicationCookie);
      
      // Add custom user claims here
      // --> doesn't work

      return userIdentity;
    }
  }

  public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
  {
    public ApplicationDbContext()
        : base("MainConnection3", throwIfV1Schema: false)
    {
      Database.SetInitializer<ApplicationDbContext>(null);
    }

    public static ApplicationDbContext Create()
    {
      return new ApplicationDbContext();
    }
  }
}