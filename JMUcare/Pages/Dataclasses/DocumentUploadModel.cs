using JMUcare.Pages.Dataclasses;
using System.Collections.Generic;

namespace JMUcare.Pages.Components
{
    public class DocumentUploadModel
    {
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public bool CanEdit { get; set; }
        public List<DocumentModel> Documents { get; set; } = new List<DocumentModel>();
    }
}
