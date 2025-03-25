public class MasterProjectMessageModel
{
    public int MasterProjectMessageID { get; set; }
    public int MasterProjectID { get; set; }
    public int SenderID { get; set; }
    public string MessageText { get; set; }
    public DateTime SentDateTime { get; set; }
}