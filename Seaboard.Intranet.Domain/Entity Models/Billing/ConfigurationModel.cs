using System;

namespace Seaboard.Intranet.Domain.Models
{
    public class ConfigurationModel
    {
        public string FilePath { get; set; }
        public int Sheet { get; set; }
        public int RowNameColumn { get; set; }
        public int RowBeginningData { get; set; }
        public string CréditorExcelColumn { get; set; }
        public string DebtorExcelColumn { get; set; }
        public string SummaryTotalColumn { get; set; }
        public int SummaryRowBeginningData { get; set; }
        public string SqlEmailProfile { get; set; }
        public string DefaultNoteNC { get; set; }
        public string DefaultNoteND { get; set; }
        public string TaxRegistrationNumber { get; set; }
        public DateTime InvoiceStartDate { get; set; }
    }
}
