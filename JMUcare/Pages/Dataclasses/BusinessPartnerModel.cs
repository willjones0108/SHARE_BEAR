namespace JMUcare.Pages.Dataclasses
{
    public class BusinessPartnerModel
    {
        public int Business_Partner_ID { get; set; }
        public string Business_Name { get; set; }
        public string BusinessType { get; set; }
        public string OrgType { get; set; }
        public string ContactInfo { get; set; }
        public string Status_Flag { get; set; }
        public bool IsArchived { get; set; }
        public int AdminUserID { get; set; }
    }
}
