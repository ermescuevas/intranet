using Seaboard.Intranet.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    [Table("FOODMENULINE")]
    public class FoodMenuLine
    {
        [Key]
        public int FoodMenuLineId { get; set; }
        public int FoodId { get; set; }
        public Food Food { get; set; }
        public FoodSchedule FoodLineScheduleId { get; set; }
        public int FoodMenuId { get; set; }
        public virtual FoodMenu FoodMenu { get; set; }

    }
}
