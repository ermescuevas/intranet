namespace Seaboard.Intranet.Domain.Models
{
    public class MemNetTransDetail
    {
        public string BatchNumber { get; set; }
        public int Check { get; set; }
        public string Module { get; set; }
        public string CustomerVendor { get; set; }
        public string DocumentNumber { get; set; }
        public string DocumentDate { get; set; }
        public string DueDate { get; set; }
        public string CurrencyId { get; set; }
        public decimal CurrentAmount { get; set; }
        public decimal AppliedAmount { get; set; }
        public string Ncf { get; set; }
        public int DocumentType { get; set; }
    }
}
