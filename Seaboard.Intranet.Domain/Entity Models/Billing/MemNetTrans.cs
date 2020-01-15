namespace Seaboard.Intranet.Domain.Models
{
    public class MemNetTrans
    {
        public string BatchNumber { get; set; }
        public string Description { get; set; }
        public string CustomerId { get; set; }
        public string CustomerName { get; set; }
        public string VendorId { get; set; }
        public string VendorName { get; set; }
        public string CurrencyId { get; set; }
        public string DocumentDate { get; set; }
        public string CloseDate { get; set; }
        public string Note { get; set; }
        public string Resolution { get; set; }
        public int Posted { get; set; }
    }
}
