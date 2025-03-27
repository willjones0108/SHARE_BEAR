public class ProjectModel
{
    public int ProjectID { get; set; }
    public string Title { get; set; }
    public int CreatedBy { get; set; }
    public int? GrantID { get; set; }
    public int PhaseID { get; set; }
    public string ProjectType { get; set; }
    public string TrackingStatus { get; set; }
    public bool IsArchived { get; set; }
    public string Project_Description { get; set; }
}
