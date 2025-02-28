namespace JMUcare.Pages.Dataclasses
{
    public class GrantMessageModel
    {
        public int GrantMessageID { get; set; }
        public int GrantID { get; set; }
        public int SenderID { get; set; }
        public string MessageText { get; set; }
        public DateTime SentDateTime { get; set; }
    }
}
