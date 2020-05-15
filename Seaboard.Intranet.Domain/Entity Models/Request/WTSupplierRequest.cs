using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class WtSupplierRequest
    {
        [Key]
        public string TransferId { get; set; }
        public string SupplierId { get; set; }
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
        public DateTime? ValueDate { get; set; }
    }

    public class TransferbyId
    {        
        public string TransferId { get; set; }
    }
}
