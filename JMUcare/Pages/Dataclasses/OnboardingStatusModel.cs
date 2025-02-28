namespace JMUcare.Pages.Dataclasses
{
    public class OnboardingStatusModel
    {
        public int OnboardingID { get; set; }
        public int UserID { get; set; }
        public string Stage { get; set; }
        public int AssignedStaffID { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }
}
