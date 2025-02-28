namespace JMUcare.Pages.Dataclasses
{
    public class SystemNotificationModel
    {
        public int NotificationID { get; set; }
        public int UserID { get; set; }
        public DateTime Timestamp { get; set; }
        public string Status { get; set; }
    }
}
