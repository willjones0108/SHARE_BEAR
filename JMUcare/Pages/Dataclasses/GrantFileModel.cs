namespace JMUcare.Pages.Dataclasses
{
    public class GrantFileModel
    {
        public int GrantFileID { get; set; }
        public int GrantID { get; set; }
        public string FilePath { get; set; }
        public int UploadedBy { get; set; }
        public DateTime UploadedDateTime { get; set; }
    }
}
