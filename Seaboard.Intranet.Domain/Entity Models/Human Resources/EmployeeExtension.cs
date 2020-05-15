using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    [Table("PHONEDIRECTORY")]
    public class EmployeeExtension
    {
        [Key]
        public int Id { get; set; }
        public string Name { get; set; }
        public string LastName { get; set; }
        public string ExtensionNumber { get; set; }
        public string CellPhone { get; set; }
        public string DepartmentId { get; set; }
        public virtual Department Department { get; set; }
    }
}
