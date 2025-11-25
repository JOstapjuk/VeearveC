namespace Veearve.Models
{
    public class AnnualSummaryDto
    {
        public int TotalReadings { get; set; }
        public string TotalColdWater { get; set; }
        public string TotalHotWater { get; set; }
        public string TotalAmount { get; set; }
        public string PaidAmount { get; set; }
        public string UnpaidAmount { get; set; }
    }
}
