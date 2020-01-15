namespace Seaboard.Intranet.Domain
{
    public class Transaction
    {
        public string CustomerId { get; set; }
        public string DocumentType { get; set; }
        public string ItemId { get; set; }
        public string Description { get; set; }
        public decimal Amount { get; set; }
        public decimal Quantity { get; set; }
        public string ReferenceInvoice { get; set; }
        public string ReferenceNcf { get; set; }
        public string Moneda { get; set; }
        public string Notes { get; set; }
        public string Flag { get; set; }
    }
}
