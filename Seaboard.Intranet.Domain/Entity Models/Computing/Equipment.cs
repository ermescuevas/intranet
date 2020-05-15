using System;

namespace Seaboard.Intranet.Domain
{
    public class Equipment
    {
        public string DeviceCode { get; set; }
        public string SerialNumber { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Description { get; set; }
        public string Operator { get; set; }
        public MobileAcquireMode AcquireMode { get; set; }
        public decimal Cost { get; set; }
        public int MobileTechnology { get; set; }
        public bool CanUseData { get; set; }
        public DateTime AcquiredDate { get; set; }
        public string AsignedUser { get; set; }
        public string PhoneNumber { get; set; }
        public string SimCard { get; set; }
        public DateTime DeliveryDate { get; set; }
        public int Status { get; set; }
    }
}
