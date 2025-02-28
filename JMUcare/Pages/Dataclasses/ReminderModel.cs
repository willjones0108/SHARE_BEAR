namespace JMUcare.Pages.Dataclasses
{
    public class ReminderModel
    {
        public int ReminderID { get; set; }
        public int UserID { get; set; }
        public DateTime DueDate { get; set; }
        public bool Acknowledged { get; set; }
        public string Status { get; set; }
    }
}
