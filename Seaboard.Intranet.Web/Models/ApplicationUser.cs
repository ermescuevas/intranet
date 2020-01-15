using Microsoft.AspNet.Identity.EntityFramework;

namespace Seaboard.Intranet.Web.Models
{
    public class ApplicationUser : IdentityUser { }

    public class ApplicationDbContext : IdentityDbContext<ApplicationUser>
    {
        public ApplicationDbContext() : base("SeaboContext") { }
    }
}