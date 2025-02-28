namespace JMUcare.Pages.Dataclasses
{
    public class UserCredentialModel
    {
        public int UserCredentialID { get; set; }
        public int UserID { get; set; }
        public string HashedPassword { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}
