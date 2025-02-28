namespace JMUcare.Pages.Dataclasses
{
    public class AdminInteractionModel
    {
        public int AdminInteractionID { get; set; }
        public string MeetingPurpose { get; set; }
        public DateTime DateHeld { get; set; }
        public int PartnerID { get; set; }
        public int UserID { get; set; }
    }
}
