using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class AmountOfItemViewModel
    {
        [Key]
        public string ArticleNum { get; set; }
        public string ArticleDes { get; set; }
        public decimal OrderPointquant { get; set; }
        public decimal OrderQuantity { get; set; }
        public decimal ExistingAmount { get; set; }
        public decimal AskUptolevel { get; set; }
    }
}
