using System;

namespace Seaboard.Intranet.Domain
{
    public class DigitalDocument
    {
        public string BatchNumber { get; set; }
        public string DocumentId { get; set; }
        public string Name { get; set; }
        public string Description { get; set; }
        public string PersonalizedContent { get; set; }
        public DateTime DocumentDate { get; set; }
        public string Language { get; set; }
        public bool Status { get; set; }
    }
}
