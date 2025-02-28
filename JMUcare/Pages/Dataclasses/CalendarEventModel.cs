namespace JMUcare.Pages.Dataclasses
{
    public class CalendarEventModel
    {
        public int EventID { get; set; }
        public string EventType { get; set; }
        public DateTime StartDateTime { get; set; }
        public DateTime EndDateTime { get; set; }
        public int UserID { get; set; }
        public int GrantID { get; set; }
        public int ProjectID { get; set; }
        public int TaskID { get; set; }
        public int ReminderID { get; set; }
        public string History { get; set; }
        public string Status { get; set; }
    }
}
