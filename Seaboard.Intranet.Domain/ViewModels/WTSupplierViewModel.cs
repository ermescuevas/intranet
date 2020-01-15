using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class WtSupplierViewModel
    {
        [Key]
        public string TransferId { get; set; }
        public string SupplierId { get; set; }
        public string Vendname { get; set; }
        [DisplayFormat(DataFormatString = "{0:M/d/yyy}")]
        public DateTime? Date { get; set; }
        public string Location { get; set; }
        public string Subject { get; set; }
        public string BankName { get; set; }
        public string AbaNumber { get; set; }
        public string SwiftOrBankCod { get; set; }
        public string Address { get; set; }
        public string CityState { get; set; }
        public string AccountName { get; set; }
        public string AccountNumber { get; set; }
        public string FurtherCredit { get; set; }
        public string AbaNumber2 { get; set; }
        public string WireReferences { get; set; }
        [DisplayFormat(DataFormatString = "{0:M/d/yyy}")]
        public DateTime? ValueDate { get; set; }
        public string Amount { get; set; }
    }
}
