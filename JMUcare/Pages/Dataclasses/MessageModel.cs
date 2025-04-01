namespace JMUcare.Pages.Dataclasses
{
    public class MessageModel
    {
        public int MessageID { get; set; }
        public int SenderID { get; set; }
        public string SenderName { get; set; }
        public int RecipientID { get; set; }
        public string RecipientName { get; set; }
        public string MessageText { get; set; }
        public DateTime SentDateTime { get; set; }
        public string Status { get; set; }
    }
}
