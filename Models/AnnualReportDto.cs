namespace Veearve.Models
{
    public class AnnualReportDto
    {
        public int Year { get; set; }
        public AnnualSummaryDto Summary { get; set; }
        public List<Reading> Readings { get; set; }
    }
}
