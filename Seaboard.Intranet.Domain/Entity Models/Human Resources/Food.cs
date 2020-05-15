using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    [Table("FOOD")]
    public class Food
    {
        [Key]
        public int FoodId { get; set; }
        public string FoodName { get; set; }
    }
}
