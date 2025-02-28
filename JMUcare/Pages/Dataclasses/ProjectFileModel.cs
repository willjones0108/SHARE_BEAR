namespace JMUcare.Pages.Dataclasses
{
    public class ProjectFileModel
    {
        public int FileID { get; set; }
        public int ProjectID { get; set; }
        public string FilePath { get; set; }
        public int UploadedBy { get; set; }
        public DateTime UploadedDateTime { get; set; }
    }
}
