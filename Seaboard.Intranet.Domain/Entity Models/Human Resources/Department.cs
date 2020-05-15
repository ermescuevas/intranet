using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Seaboard.Intranet.Domain.Models
{
    [Table("DEPARTMENTS")]
    public class Department
    {
        [Key]
        public string DepartmentId { get; set; }
        public string Description { get; set; }
        public virtual ICollection<EmployeeExtension> EmployeeExtensions { get; set; }
    }
}
