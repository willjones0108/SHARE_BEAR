public class GrantModel
{
    public int GrantID { get; set; }
    public string GrantTitle { get; set; }
    public string Category { get; set; }
    public string FundingSource { get; set; }
    public decimal Amount { get; set; }
    public string Status { get; set; }
    public int CreatedBy { get; set; }
    public int GrantLeadID { get; set; }
    public string Description { get; set; }  
    public string TrackingStatus { get; set; }
    public bool IsArchived { get; set; }
}
