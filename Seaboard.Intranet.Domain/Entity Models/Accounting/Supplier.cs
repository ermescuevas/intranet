namespace Seaboard.Intranet.Domain.Models
{
    public class Supplier
    {
        public string SupplierId { get; set; }
        public string RNC { get; set; }
        public string SupplierName { get; set; }
        public string ShortName { get; set; }
        public string Contact { get; set; }
        public string Address1 { get; set; }
        public string Address2 { get; set; }
        public string Address3 { get; set; }
        public string State { get; set; }
        public string City { get; set; }
        public string Country { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public string Phone3 { get; set; }
        public string Fax { get; set; }
        public string PaymentCondition { get; set; }
        public int ExpenseType { get; set; }
        public string Email { get; set; }
    }
}