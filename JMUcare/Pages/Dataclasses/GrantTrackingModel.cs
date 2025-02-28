namespace JMUcare.Pages.Dataclasses
{
    public class GrantTrackingModel
    {
        public int TrackingID { get; set; }
        public int GrantID { get; set; }
        public string StageName { get; set; }
        public string Notes { get; set; }
        public DateTime CompletionDate { get; set; }
    }
}
