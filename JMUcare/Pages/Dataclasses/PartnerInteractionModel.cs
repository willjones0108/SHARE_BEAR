namespace JMUcare.Pages.Dataclasses
{
    public class PartnerInteractionModel
    {
        public int InteractionID { get; set; }
        public int PartnerID { get; set; }
        public int UserID { get; set; }
        public string InteractionType { get; set; }
        public DateTime DateLogged { get; set; }
    }
}
