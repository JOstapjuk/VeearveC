namespace Veearve.Models
{
    public class BulkEmailResultDto
    {
        public string Message { get; set; }
        public int TotalUnpaidBills { get; set; }
        public int EmailsSent { get; set; }
        public int EmailsFailed { get; set; }
        public List<EmailDetailDto> Details { get; set; }
    }
}
