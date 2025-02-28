namespace JMUcare.Pages.Dataclasses
{
    public class ProjectModel
    {
        public int ProjectID { get; set; }
        public int GrantID { get; set; }
        public string Title { get; set; }
        public int CreatedBy { get; set; }
        public string TrackingStatus { get; set; }
        public bool IsArchived { get; set; }
    }
}
