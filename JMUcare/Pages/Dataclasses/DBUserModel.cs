namespace JMUcare.Pages.Dataclasses
{
    public class DbUserModel
    {
        public int UserID { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Email { get; set; }
        public int UserRoleID { get; set; }
        public DateTime UpdatedAt { get; set; }
        public bool IsArchived { get; set; }
    }
}
