using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    [Table("USERPERMISSIONS")]
    public class UserPermission
    {
        public int UserPermissionId { get; set; }
        public string UserId { get; set; }
        public virtual User User { get; set; }
        public int PermissionId { get; set; }
        public virtual Permission Permission { get; set; }
    }
}
