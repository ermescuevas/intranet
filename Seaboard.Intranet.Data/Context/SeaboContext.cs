using Seaboard.Intranet.Domain.Models;
using System.Data.Entity;

namespace Seaboard.Intranet.Data
{
    public class SeaboContext : DbContext
    {
        public SeaboContext() { Database.CommandTimeout = 180; }
        public DbSet<FoodMenu> FoodMenus { get; set; }
        public DbSet<EmployeeExtension> EmployeeExtensions { get; set; }
        public DbSet<Department> Departments { get; set; }
        public DbSet<User> Users { get; set; }
        public DbSet<Permission> Permissions { get; set; }
    }
}