namespace JMUcare.Pages.Dataclasses
{
    public class DocumentModel
    {
        public int DocumentID { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public DateTime UploadedDate { get; set; }
        public int UploadedBy { get; set; }
        public string BlobUrl { get; set; }
        public string BlobName { get; set; }
        public bool IsArchived { get; set; }

        // Entity associations (only one will be non-null)
        public int? GrantID { get; set; }
        public int? PhaseID { get; set; }
        public int? ProjectID { get; set; }
        public int? TaskID { get; set; }

    }
}
