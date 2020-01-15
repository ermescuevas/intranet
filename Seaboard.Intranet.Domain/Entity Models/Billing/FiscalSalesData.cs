using System;

namespace Seaboard.Intranet.Domain
{
    public class FiscalSalesTransaction
    {
        public string Rnc { get; set; }
        public string Ncf { get; set; }
        public string ApplyNcf { get; set; }
        public string IncomeType { get; set; }
        public DateTime DocumentDate { get; set; }
        public decimal DocumentAmount { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal WithholdTax { get; set; }
    }
}
