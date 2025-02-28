namespace JMUcare.Pages.Dataclasses
{
    public class UserPermissionsModel
    {
        public int PermissionID { get; set; }
        public int UserID { get; set; }
        public string Permissions { get; set; }
        public string AccessLevel { get; set; }
    }
}
