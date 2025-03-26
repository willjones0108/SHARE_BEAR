namespace JMUcare.Pages.Dataclasses
{
    public class PhaseModel
    {
        public int PhaseID { get; set; }
        public string PhaseName { get; set; }
        public string Description { get; set; }
        public string Status { get; set; }
        public int CreatedBy { get; set; }
        public int PhaseLeadID { get; set; }

        public int GrantID { get; set; }

    }
}
