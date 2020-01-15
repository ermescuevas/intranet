using Seaboard.Intranet.Domain.Enums;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class EmployeeViewModel
    {
        public string EmployeeId { get; set; }
        public string PersonName { get; set; }
        public string PersonAddress { get; set; }
        public DateTime BirthDay { get; set; }
        public DateTime StartDate { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string PersonId { get; set; }
        public string Department { get; set; }
        public string JobTitle { get; set; }
        public Months Month { get; set; }
        public string Year { get; set; }
    }
}
