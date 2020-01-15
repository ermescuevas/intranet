using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    [Table("PERMISSIONS")]
    public class Permission
    {
        [Key]
        public int PermissionId { get; set; }
        public string Name { get; set; }
        public string Action { get; set; }
        public string Controller { get; set; }
        public virtual ICollection<UserPermission> UserPermissions { get; set; }
    }
}
