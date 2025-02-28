namespace JMUcare.Pages.Dataclasses
{
    public class ProjectMessageModel
    {
        public int ProjectMessageID { get; set; }
        public int ProjectID { get; set; }
        public int SenderID { get; set; }
        public string MessageText { get; set; }
        public DateTime SentDateTime { get; set; }
    }
}
