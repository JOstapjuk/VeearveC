namespace Veearve.Models
{
    public class EmailReminderDto
    {
        public bool EmailSent { get; set; }
        public string Message { get; set; }
        public string Recipient { get; set; }
    }
}
