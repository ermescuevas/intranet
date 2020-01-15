using Seaboard.Intranet.Domain.Enums;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Web;

namespace Seaboard.Intranet.Domain.Models
{
    [Table("FOODMENU")]
    public class FoodMenu
    {
        [Key]
        public int FoodMenuId { get; set; }
        [DisplayName("Descripción")]
        public string FoodMenuDescription { get; set; }
        public DateTime FoodMenuDate { get; set; }
        [DisplayName("Horario de comida")]
        public virtual ICollection<FoodMenuLine> FoodMenuLines { get; set; }
        public FoodSchedule FoodScheduleId { get; set; }
    }
}