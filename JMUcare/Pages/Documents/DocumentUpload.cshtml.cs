using JMUcare.Pages.Dataclasses;
using JMUcare.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Threading.Tasks;

namespace JMUcare.Pages.Documents
{
    public class DocumentUploadModel : PageModel
    {
        private readonly BlobStorageService _blobStorageService;
        public bool CanEdit { get; set; }
        public string EntityType { get; set; }
        public int EntityId { get; set; }
        public List<DocumentModel> Documents { get; set; } = new List<DocumentModel>();


        public DocumentUploadModel(BlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        [BindProperty]
        public IFormFile UploadedFile { get; set; }

        public async Task<IActionResult> OnPostAsync()
        {
            if (UploadedFile == null)
            {
                ModelState.AddModelError(string.Empty, "Please select a file to upload.");
                return Page();
            }

            // Upload the file to the local storage
            string folderPath = "documents";
            string filePath = await _blobStorageService.UploadDocumentAsync(UploadedFile, folderPath);

            // Optionally, save the file path to the database or perform other actions

            TempData["Message"] = "File uploaded successfully!";
            return RedirectToPage("List");
        }
    }
}



//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Threading.Tasks;
//using JMUcare.Pages.Dataclasses;
//using JMUcare.Pages.DBclass;
//using JMUcare.Services;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;
//using Microsoft.Extensions.Logging;

//namespace JMUcare.Pages.Documents
//{
//    public class DocumentUploadModel : PageModel
//    {
//        private readonly BlobStorageService _blobStorageService;
//        private readonly ILogger<DocumentUploadModel> _logger;

//        [BindProperty]
//        public string EntityType { get; set; }

//        [BindProperty]
//        public int EntityId { get; set; }

//        public bool CanEdit { get; set; }

//        public List<DocumentModel> Documents { get; set; } = new List<DocumentModel>();

//        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

//        public DocumentUploadModel(BlobStorageService blobStorageService, ILogger<DocumentUploadModel> logger)
//        {
//            _blobStorageService = blobStorageService;
//            _logger = logger;
//        }

//        public IActionResult OnGet(string entityType, int entityId)
//        {
//            if (CurrentUserID == 0)
//            {
//                return RedirectToPage("/HashedLogin/HashedLogin");
//            }

//            if (string.IsNullOrEmpty(entityType))
//            {
//                TempData["ErrorMessage"] = "Entity type is required";
//                return RedirectToPage("/Index");
//            }

//            // Redirect tasks to their parent projects
//            if (entityType.ToLower() == "task")
//            {
//                int projectId = DBClass.GetProjectIdForTask(entityId);
//                if (projectId > 0)
//                {
//                    TempData["InfoMessage"] = "Documents should be uploaded to the project level.";
//                    return RedirectToPage(new { entityType = "project", entityId = projectId });
//                }
//            }

//            EntityType = entityType;
//            EntityId = entityId;

//            // Check if the user has access to this entity
//            var (hasAccess, canEdit) = CheckUserAccess(entityType, entityId);
//            CanEdit = canEdit;

//            if (!hasAccess)
//            {
//                return RedirectToPage("/Shared/AccessDenied");
//            }

//            // Load documents
//            Documents = DBClass.GetDocumentsByEntityId(EntityType, EntityId);

//            return Page();
//        }


//        public async Task<IActionResult> OnPostUploadDocumentAsync(IFormFile file)
//        {
//            if (CurrentUserID == 0)
//            {
//                return RedirectToPage("/HashedLogin/HashedLogin");
//            }

//            // Enhanced validation
//            if (file == null || file.Length == 0)
//            {
//                TempData["ErrorMessage"] = "No file selected";
//                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
//            }

//            // Validate file size (e.g., max 50MB)
//            if (file.Length > 52428800) // 50MB in bytes
//            {
//                TempData["ErrorMessage"] = "File size exceeds the 50MB limit";
//                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
//            }

//            // Validate file extension (optional)
//            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png" };
//            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
//            if (!allowedExtensions.Contains(fileExtension))
//            {
//                TempData["ErrorMessage"] = "File type is not supported";
//                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
//            }

//            // Check access rights
//            var (_, hasEditAccess) = CheckUserAccess(EntityType, EntityId);
//            if (!hasEditAccess)
//            {
//                return RedirectToPage("/Shared/AccessDenied");
//            }

//            try
//            {
//                // Upload to blob storage
//                string blobName = await _blobStorageService.UploadDocumentAsync(file, EntityType.ToLower() + "/" + EntityId);

//                // Create document record
//                var document = new DocumentModel
//                {
//                    FileName = file.FileName,
//                    ContentType = file.ContentType,
//                    FileSize = file.Length,
//                    UploadedDate = DateTime.UtcNow,
//                    UploadedBy = CurrentUserID,
//                    BlobName = blobName,
//                    BlobUrl = await _blobStorageService.GenerateSasTokenAsync(blobName, TimeSpan.FromHours(1)),
//                    IsArchived = false
//                };

//                // Set the appropriate entity ID
//                switch (EntityType.ToLower())
//                {
//                    case "grant":
//                        document.GrantID = EntityId;
//                        break;
//                    case "phase":
//                        document.PhaseID = EntityId;
//                        break;
//                    case "project":
//                        document.ProjectID = EntityId;
//                        break;
//                    case "task":
//                        document.TaskID = EntityId;
//                        break;
//                }

//                // Save to database
//                int documentId = DBClass.InsertDocument(document);

//                TempData["SuccessMessage"] = "File uploaded successfully";
//                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error uploading file for {EntityType} {EntityId}", EntityType, EntityId);
//                TempData["ErrorMessage"] = $"Error uploading file: {ex.Message}";
//                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
//            }
//        }

//        public async Task<IActionResult> OnGetDeleteDocumentAsync(int documentId)
//        {
//            if (CurrentUserID == 0)
//            {
//                return RedirectToPage("/HashedLogin/HashedLogin");
//            }

//            var document = DBClass.GetDocumentById(documentId);

//            if (document == null)
//            {
//                TempData["ErrorMessage"] = "Document not found";
//                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
//            }

//            // Determine entity type and id from the document
//            int relevantEntityId = 0;
//            string entityType = string.Empty;

//            if (document.GrantID.HasValue)
//            {
//                relevantEntityId = document.GrantID.Value;
//                entityType = "grant";
//            }
//            else if (document.PhaseID.HasValue)
//            {
//                relevantEntityId = document.PhaseID.Value;
//                entityType = "phase";
//            }
//            else if (document.ProjectID.HasValue)
//            {
//                relevantEntityId = document.ProjectID.Value;
//                entityType = "project";
//            }


//            // Check access
//            var (_, hasEditAccess) = CheckUserAccess(entityType, relevantEntityId);
//            if (!hasEditAccess)
//            {
//                return RedirectToPage("/Shared/AccessDenied");
//            }

//            try
//            {
//                // Delete from blob storage
//                await _blobStorageService.DeleteDocumentAsync(document.BlobName);

//                // Soft delete in database
//                bool success = DBClass.ArchiveDocument(documentId);

//                if (success)
//                {
//                    TempData["SuccessMessage"] = "Document deleted successfully";
//                }
//                else
//                {
//                    TempData["ErrorMessage"] = "Error deleting document from database";
//                }
//            }
//            catch (Exception ex)
//            {
//                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
//                TempData["ErrorMessage"] = $"Error deleting document: {ex.Message}";
//            }

//            return RedirectToPage(new { entityType = entityType, entityId = relevantEntityId });
//        }

//        private (bool hasAccess, bool canEdit) CheckUserAccess(string entityType, int entityId)
//        {
//            if (string.IsNullOrEmpty(entityType))
//                return (false, false);

//            bool isAdmin = DBClass.IsUserAdmin(CurrentUserID);

//            switch (entityType.ToLower())
//            {
//                case "grant":
//                    string grantAccess = DBClass.GetUserAccessLevelForGrant(CurrentUserID, entityId);
//                    return (grantAccess != "None", grantAccess == "Edit" || isAdmin);

//                case "phase":
//                    string phaseAccess = DBClass.GetUserAccessLevelForPhase(CurrentUserID, entityId);
//                    return (phaseAccess != "None", phaseAccess == "Edit" || isAdmin);

//                case "project":
//                    string projectAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, entityId);
//                    return (projectAccess != "None", projectAccess == "Edit" || isAdmin);

//                // Modify this case to redirect to project instead
//                case "task":
//                    int projectId = DBClass.GetProjectIdForTask(entityId);
//                    // Redirect to project document upload
//                    return (false, false); // This will cause a redirect in the OnGet method

//                default:
//                    return (false, false);
//            }
//        }
//    }
//}
