using Seaboard.Intranet.Domain.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain
{
    public class DepartmentExtension
    {
        [Key]
        public Department Department { get; set; }
        public List<EmployeeExtension> EmployeeExtensions { get; set; }
    }
}
