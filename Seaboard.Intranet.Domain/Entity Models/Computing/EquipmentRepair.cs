using System;

namespace Seaboard.Intranet.Domain
{
    public class EquipmentRepair
    {
        public string RequestId { get; set; }
        public string Device { get; set; }
        public DateTime DocumentDate  { get; set; }
        public string Diagnostics { get; set; }
        public string Supplier { get; set; }
        public decimal Cost { get; set; }
        public string BaseDocumentNumber { get; set; }
        public int Status { get; set; }
        public string Note { get; set; }
    }
}
