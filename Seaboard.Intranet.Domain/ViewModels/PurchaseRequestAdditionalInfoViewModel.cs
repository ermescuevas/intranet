using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Seaboard.Intranet.Domain.ViewModels
{
    public class PurchaseRequestAdditionalInfoViewModel
    {
        public string Description { get; set; }
        public DateTime DocumentDate { get; set; }
        public DateTime RequiredDate { get; set; }
        public string Requester { get; set; }
        public string Department { get; set; }
        public string Aprover { get; set; }
        public string UserQuote { get; set; }
        public string UserPreAnalysis { get; set; }
        public string UserAnalysis { get; set; }
        public DateTime QuoteDate { get; set; }
        public DateTime PreAnalysisDate { get; set; }
        public DateTime AnalysisDate { get; set; }
    }
}
