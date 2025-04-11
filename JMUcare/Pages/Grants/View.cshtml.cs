using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using JMUcare.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;

namespace JMUcare.Pages.Grants
{
    public class ViewModel : PageModel
    {
        private readonly BlobStorageService _blobStorageService;
        private readonly ILogger<ViewModel> _logger;

        public ViewModel(BlobStorageService blobStorageService, ILogger<ViewModel> logger)
        {
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        // Existing properties
        public GrantModel Grant { get; set; }
        public int Id { get; set; }
        public List<PhaseModel> Phases { get; set; } = new List<PhaseModel>();
        public Dictionary<int, List<ProjectModel>> PhaseProjects { get; set; } = new Dictionary<int, List<ProjectModel>>();
        public bool CanAddPhase { get; set; }
        public bool CanAddProject { get; set; }
        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
        public bool IsAdmin => DBClass.IsUserAdmin(CurrentUserID);
        public bool IsGrantLead => Grant != null && Grant.GrantLeadID == CurrentUserID;

        // New properties for document management
        public List<DocumentModel> Documents { get; set; } = new List<DocumentModel>();
        public List<DocumentModel> AccessibleDocuments { get; set; } = new List<DocumentModel>();
        public Dictionary<int, string> UserNames { get; set; } = new Dictionary<int, string>();

        public bool CanViewAllDocuments => IsAdmin || IsGrantLead;

        public IActionResult OnGet(int id)
        {
            // Check if user is logged in
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            Id = id;

            // Load grant details
            Grant = DBClass.GetGrantById(id);

            if (Grant == null)
            {
                TempData["ErrorMessage"] = "Grant not found";
                return RedirectToPage("/Grants/Index");
            }

            // Check access level for this grant
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, id);
            if (accessLevel == "None" && !IsAdmin)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Set permission flags
            CanAddPhase = (accessLevel == "Edit" || IsAdmin);
            CanAddProject = (accessLevel == "Edit" || IsAdmin);

            // Load phases for this grant
            Phases = DBClass.GetPhasesByGrantId(id);

            // Load projects for each phase
            foreach (var phase in Phases)
            {
                var projects = DBClass.GetProjectsByPhaseId(phase.PhaseID);
                PhaseProjects[phase.PhaseID] = projects;
            }

            // Load documents for this grant based on user permissions
            LoadDocuments();

            return Page();
        }

        private void LoadDocuments()
        {
            // If user is admin or grant lead, they see all documents
            if (IsAdmin || (Grant != null && Grant.GrantLeadID == CurrentUserID))
            {
                Documents = DBClass.GetDocumentsByEntityId("grant", Id);
                AccessibleDocuments = new List<DocumentModel>(); // Empty since all docs are in the main list
            }
            else
            {
                // For regular users, only show specifically shared documents
                Documents = new List<DocumentModel>(); // No documents in the main admin/lead view

                // Get documents that this user can access
                AccessibleDocuments = DBClass.GetDocumentsByEntityId("grant", Id)
                    .Where(d => CanUserViewDocument(d))
                    .ToList();
            }

            // Load user names for all documents (both main list and accessible list)
            var allDocs = new List<DocumentModel>();
            allDocs.AddRange(Documents);
            allDocs.AddRange(AccessibleDocuments);

            var userIds = allDocs.Select(d => d.UploadedBy).Distinct().ToList();
            foreach (var userId in userIds)
            {
                UserNames[userId] = DBClass.GetUserDisplayName(userId);
            }
        }

        public string GetUploaderName(int uploaderId)
        {
            if (UserNames.ContainsKey(uploaderId))
            {
                return UserNames[uploaderId];
            }
            return "Unknown User";
        }

        public bool CanUserViewDocument(DocumentModel document)
        {
            // Admin and Grant Lead can view all documents
            if (IsAdmin || (Grant != null && Grant.GrantLeadID == CurrentUserID))
            {
                return true;
            }

            // Document owners can view their own documents
            if (document.UploadedBy == CurrentUserID)
            {
                return true;
            }

            // Users with grant edit access can view all documents
            string grantAccess = DBClass.GetUserAccessLevelForGrant(CurrentUserID, Id);
            if (grantAccess == "Edit")
            {
                return true;
            }

            // By default, users with just View access don't see any documents
            return false;
        }

        public bool CanUserEditDocument(DocumentModel document)
        {
            if (IsAdmin || document.UploadedBy == CurrentUserID || (Grant != null && Grant.GrantLeadID == CurrentUserID))
            {
                return true;
            }

            // Check grant-specific permissions
            string access = DBClass.GetUserAccessLevelForGrant(CurrentUserID, Id);
            return access == "Edit";
        }

        public string GetFileIcon(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".pdf" => "bi-file-earmark-pdf",
                ".doc" or ".docx" => "bi-file-earmark-word",
                ".xls" or ".xlsx" => "bi-file-earmark-excel",
                ".jpg" or ".jpeg" or ".png" => "bi-file-earmark-image",
                ".txt" => "bi-file-earmark-text",
                _ => "bi-file-earmark"
            };
        }

        public string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            int order = 0;
            double size = bytes;

            while (size >= 1024 && order < sizes.Length - 1)
            {
                order++;
                size /= 1024;
            }

            return $"{size:0.##} {sizes[order]}";
        }

        public async Task<IActionResult> OnPostUploadDocumentAsync(IFormFile file, string entityType, int entityId)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Enhanced validation
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "No file selected";
                return RedirectToPage(new { id = entityId });
            }

            // Validate file size (e.g., max 50MB)
            if (file.Length > 52428800) // 50MB in bytes
            {
                TempData["ErrorMessage"] = "File size exceeds the 50MB limit";
                return RedirectToPage(new { id = entityId });
            }

            // Validate file extension
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "File type is not supported";
                return RedirectToPage(new { id = entityId });
            }

            // Verify the entity type is valid
            if (entityType.ToLower() != "grant")
            {
                TempData["ErrorMessage"] = "Invalid entity type";
                return RedirectToPage(new { id = entityId });
            }

            // Check access rights for the grant
            Grant = DBClass.GetGrantById(entityId);
            if (Grant == null)
            {
                TempData["ErrorMessage"] = "Grant not found";
                return RedirectToPage("/Grants/Index");
            }

            bool canUpload = IsAdmin || Grant.GrantLeadID == CurrentUserID;
            if (!canUpload)
            {
                string grantAccess = DBClass.GetUserAccessLevelForGrant(CurrentUserID, entityId);
                canUpload = grantAccess == "Edit";
            }

            if (!canUpload)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                // Upload to blob storage
                string blobName = await _blobStorageService.UploadDocumentAsync(file, entityType.ToLower() + "/" + entityId);

                // Create document record
                var document = new DocumentModel
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadedDate = DateTime.UtcNow,
                    UploadedBy = CurrentUserID,
                    BlobName = blobName,
                    BlobUrl = await _blobStorageService.GenerateSasTokenAsync(blobName, TimeSpan.FromHours(1)),
                    IsArchived = false,
                    GrantID = entityId
                };

                // Save to database
                int documentId = DBClass.InsertDocument(document);

                TempData["SuccessMessage"] = "File uploaded successfully";
                return RedirectToPage(new { id = entityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                TempData["ErrorMessage"] = $"Error uploading file: {ex.Message}";
                return RedirectToPage(new { id = entityId });
            }
        }

        // Delete document handler
        public async Task<IActionResult> OnGetDeleteDocumentAsync(int documentId, int grantId)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            var document = DBClass.GetDocumentById(documentId);

            if (document == null)
            {
                TempData["ErrorMessage"] = "Document not found";
                return RedirectToPage(new { id = grantId });
            }

            // Check if user has rights to delete this document
            if (!CanUserEditDocument(document))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                // Delete from blob storage
                await _blobStorageService.DeleteDocumentAsync(document.BlobName);

                // Soft delete in database (archive)
                bool success = DBClass.ArchiveDocument(documentId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Document deleted successfully";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error deleting document from database";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document {DocumentId}", documentId);
                TempData["ErrorMessage"] = $"Error deleting document: {ex.Message}";
            }

            return RedirectToPage(new { id = grantId });
        }

        // Original ARCHIVE GRANT Handler
        public IActionResult OnPostArchiveGrant(int grantId)
        {
            // Check if user is logged in
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check if user has admin rights or is a grant editor
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, grantId);
            bool canArchive = (accessLevel == "Edit" || IsAdmin);

            if (!canArchive)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            bool success = DBClass.ArchiveGrant(grantId);

            if (success)
            {
                TempData["SuccessMessage"] = "Grant archived successfully";
                return RedirectToPage("/Grants/Index");
            }
            else
            {
                TempData["ErrorMessage"] = "Error archiving grant";
                return RedirectToPage();
            }
        }

        // Original ARCHIVE PHASE Handler
        public IActionResult OnPostArchivePhase(int phaseId)
        {
            // Check if user is logged in
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Get phase details to find the grant ID
            PhaseModel phase = DBClass.GetPhaseById(phaseId);

            if (phase == null)
            {
                TempData["ErrorMessage"] = "Phase not found";
                return RedirectToPage();
            }

            int grantId = phase.GrantID;

            // Check if user has edit rights for this phase
            string accessLevel = DBClass.GetUserAccessLevelForPhase(CurrentUserID, phaseId);
            bool canArchive = (accessLevel == "Edit" || IsAdmin);

            if (!canArchive)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            bool success = DBClass.ArchivePhase(phaseId);

            if (success)
            {
                TempData["SuccessMessage"] = "Phase archived successfully";
            }
            else
            {
                TempData["ErrorMessage"] = "Error archiving phase";
            }

            return RedirectToPage(new { id = grantId });
        }
    }
}
