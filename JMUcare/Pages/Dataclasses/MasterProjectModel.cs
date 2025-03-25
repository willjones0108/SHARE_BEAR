public class MasterProjectModel
{
    public int MasterProjectID { get; set; }
    public string Title { get; set; }
    public string Description { get; set; }
    public string Status { get; set; }
    public int CreatedBy { get; set; }
    public int ProjectLeadID { get; set; }
    public bool IsArchived { get; set; }
    public string Master_Project_Description { get; set; }
}