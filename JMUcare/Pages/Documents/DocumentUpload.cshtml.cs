//using System;
//using System.Collections.Generic;
//using System.Threading.Tasks;
//using JMUcare.Pages.Dataclasses;
//using JMUcare.Pages.DBclass;
//using JMUcare.Services;
//using JMUcare.Pages.Components;
//using Microsoft.AspNetCore.Http;
//using Microsoft.AspNetCore.Mvc;
//using Microsoft.AspNetCore.Mvc.RazorPages;

//namespace JMUcare.Pages.Documents
//{
//    public class DocumentUploadModel : PageModel
//    {
//        private readonly BlobStorageService _blobStorageService;

//        [BindProperty]
//        public string EntityType { get; set; }

//        [BindProperty]
//        public int EntityId { get; set; }

//        public bool CanEdit { get; set; }

//        public List<DocumentModel> Documents { get; set; } = new List<DocumentModel>();

//        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

//        public DocumentUploadModel(BlobStorageService blobStorageService)
//        {
//            _blobStorageService = blobStorageService;
//        }

//        public IActionResult OnGet(string entityType, int entityId)
//        {
//            if (CurrentUserID == 0)
//            {
//                return RedirectToPage("/HashedLogin/HashedLogin");
//            }

//            EntityType = entityType;
//            EntityId = entityId;

//            // Check if the user has access to this entity
//            bool hasAccess = false;

//            switch (entityType.ToLower())
//            {
//                case "grant":
//                    string grantAccess = DBClass.GetUserAccessLevelForGrant(CurrentUserID, entityId);
//                    hasAccess = grantAccess != "None";
//                    CanEdit = grantAccess == "Edit" || DBClass.IsUserAdmin(CurrentUserID);
//                    break;
//                case "phase":
//                    string phaseAccess = DBClass.GetUserAccessLevelForPhase(CurrentUserID, entityId);
//                    hasAccess = phaseAccess != "None";
//                    CanEdit = phaseAccess == "Edit" || DBClass.IsUserAdmin(CurrentUserID);
//                    break;
//                case "project":
//                    string projectAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, entityId);
//                    hasAccess = projectAccess != "None";
//                    CanEdit = projectAccess == "Edit" || DBClass.IsUserAdmin(CurrentUserID);
//                    break;
//                case "task":
//                    int projectId = DBClass.GetProjectIdForTask(entityId);
//                    string taskAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, projectId);
//                    hasAccess = taskAccess != "None";
//                    CanEdit = taskAccess == "Edit" || DBClass.IsUserAdmin(CurrentUserID);
//                    break;
//            }

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

//            // Check if user has edit access
//            bool hasEditAccess = false;

//            switch (EntityType.ToLower())
//            {
//                case "grant":
//                    hasEditAccess = DBClass.GetUserAccessLevelForGrant(CurrentUserID, EntityId) == "Edit";
//                    break;
//                case "phase":
//                    hasEditAccess = DBClass.GetUserAccessLevelForPhase(CurrentUserID, EntityId) == "Edit";
//                    break;
//                case "project":
//                    hasEditAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, EntityId) == "Edit";
//                    break;
//                case "task":
//                    int projectId = DBClass.GetProjectIdForTask(EntityId);
//                    hasEditAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, projectId) == "Edit";
//                    break;
//            }

//            if (!hasEditAccess && !DBClass.IsUserAdmin(CurrentUserID))
//            {
//                return RedirectToPage("/Shared/AccessDenied");
//            }

//            if (file == null || file.Length == 0)
//            {
//                TempData["ErrorMessage"] = "No file selected";
//                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
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
//                    BlobUrl = "", // Will be generated on demand
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

//            // Check if user has edit access
//            bool hasEditAccess = false;

//            if (document.GrantID.HasValue)
//            {
//                hasEditAccess = DBClass.GetUserAccessLevelForGrant(CurrentUserID, document.GrantID.Value) == "Edit";
//                EntityType = "grant";
//                EntityId = document.GrantID.Value;
//            }
//            else if (document.PhaseID.HasValue)
//            {
//                hasEditAccess = DBClass.GetUserAccessLevelForPhase(CurrentUserID, document.PhaseID.Value) == "Edit";
//                EntityType = "phase";
//                EntityId = document.PhaseID.Value;
//            }
//            else if (document.ProjectID.HasValue)
//            {
//                hasEditAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, document.ProjectID.Value) == "Edit";
//                EntityType = "project";
//                EntityId = document.ProjectID.Value;
//            }
//            else if (document.TaskID.HasValue)
//            {
//                int projectId = DBClass.GetProjectIdForTask(document.TaskID.Value);
//                hasEditAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, projectId) == "Edit";
//                EntityType = "task";
//                EntityId = document.TaskID.Value;
//            }

//            if (!hasEditAccess && !DBClass.IsUserAdmin(CurrentUserID))
//            {
//                return RedirectToPage("/Shared/AccessDenied");
//            }

//            try
//            {
//                // Archive in database first
//                bool archived = DBClass.ArchiveDocument(documentId);
//                if (!archived)
//                {
//                    TempData["ErrorMessage"] = "Failed to delete document";
//                    return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
//                }

//                // Then delete from blob storage
//                await _blobStorageService.DeleteDocumentAsync(document.BlobName);

//                TempData["SuccessMessage"] = "Document deleted successfully";
//                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
//            }
//            catch (Exception ex)
//            {
//                TempData["ErrorMessage"] = $"Error deleting document: {ex.Message}";
//                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
//            }
//        }
//    }
//}
