using System;

namespace Seaboard.Intranet.Domain
{
    public class NcfEntity
    {
        public string HeaderDocumentNumber { get; set; }
        public int DetailDocumentNumber { get; set; }
        public string Ncf { get; set; }
        public string NcfDescription { get; set; }
        public int SecuenceNumber { get; set; }
        public DateTime DueDate { get; set; }
    }
}
