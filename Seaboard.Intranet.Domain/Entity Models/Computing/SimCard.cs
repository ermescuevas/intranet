using System;

namespace Seaboard.Intranet.Domain
{
    public class SimCard
    {
        public string SimCardCode { get; set; }
        public string SerialNumber { get; set; }
        public string Operator { get; set; }
        public string PhoneNumber { get; set; }
        public DateTime AcquiredDate { get; set; }
        public bool HasData { get; set; }
        public int DataQuantity { get; set; }
        public bool MinuteOpen { get; set; }
        public int QuantityMinutes { get; set; }
        public string AsignedEquipment { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int ChangePoints { get; set; }
        public int Status { get; set; }
    }
}
