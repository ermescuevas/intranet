using System;

namespace Seaboard.Intranet.Domain
{
    public class EquipmentDelivery
    {
        public string RequestId { get; set; }
        public string Device { get; set; }
        public string AssignedUser { get; set; }
        public string DeliveryType { get; set; }
        public string SimCard { get; set; }
        public DateTime DocumentDate { get; set; }
        public string PropertyBy { get; set; }
        public decimal CostAmount { get; set; }
        public string InvoiceOwner { get; set; }
        public decimal AmountPayable { get; set; }
        public decimal AmountCoverable { get; set; }
        public string BaseDocumentNumber { get; set; }
        public string Accessories { get; set; }
        public string Note { get; set; }
        public int Status { get; set; }
    }
}
