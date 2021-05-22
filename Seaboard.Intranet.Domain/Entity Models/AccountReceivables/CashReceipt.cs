using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.Models
{
    public class CashReceipt
    {
        public string CashReceiptId { get; set; }
        public string BatchNumber { get; set; }
        public string Description { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string Currency { get; set; }
        public DateTime DocumentDate { get; set; }
        public decimal Amount { get; set; }
        public CashReceiptType CashReceiptType { get; set; }
        public bool Posted { get; set; }
        public bool Voided { get; set; }
        public string Note { get; set; }
        public string BankId { get; set; }
        public IEnumerable<CashReceiptInvoice> InvoiceLines { get; set; }
    }

    public enum CashReceiptType
    {
        Transferencia = 1,
        Cheque = 0,
        Efectivo = 3,
        Tarjeta = 2
    }
}
