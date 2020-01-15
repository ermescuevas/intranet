using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class ExchangeRateViewModel
    {
        public string CurrencyId { get; set; }
        public int Day { get; set; }
        public int Month { get; set; }
        public int Year { get; set; }
        public decimal Purchase { get; set; }
        public decimal Sales { get; set; }
        public decimal TccRate { get; set; }
    }
}
