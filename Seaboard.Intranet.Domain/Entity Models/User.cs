using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    [Table("USERS")]
    public class User
    {
        [Key]
        public string UserId { get; set; }
        public string UserName { get; set; }
        public string Password { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public string EmployeeId { get; set; }
        public string Email { get; set; }
        public string Identification { get; set; }
        public bool IsActiveDirectory { get; set; }
        public int Inactive { get; set; }
        public virtual ICollection<UserPermission> UserPermissions { get; set; }
        
    }
}
