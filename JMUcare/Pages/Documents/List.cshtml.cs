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

namespace JMUcare.Pages.Documents
{
    public class ListModel : PageModel
    {
        private readonly BlobStorageService _blobStorageService;
        private readonly ILogger<ListModel> _logger;

        public List<DocumentModel> Documents { get; set; } = new List<DocumentModel>();
        public Dictionary<int, string> UserNames { get; set; } = new Dictionary<int, string>();

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
        public bool IsAdmin => DBClass.IsUserAdmin(CurrentUserID);

        [BindProperty]
        public string EntityType { get; set; }

        [BindProperty]
        public int EntityId { get; set; }

        public ListModel(BlobStorageService blobStorageService, ILogger<ListModel> logger)
        {
            _blobStorageService = blobStorageService;
            _logger = logger;
        }

        private (bool hasAccess, bool canEdit) CheckUserAccess(string entityType, int entityId)
        {
            if (string.IsNullOrEmpty(entityType))
                return (false, false);

            bool isAdmin = DBClass.IsUserAdmin(CurrentUserID);

            switch (entityType.ToLower())
            {
                case "grant":
                    string grantAccess = DBClass.GetUserAccessLevelForGrant(CurrentUserID, entityId);
                    return (grantAccess != "None", grantAccess == "Edit" || isAdmin);

                case "phase":
                    string phaseAccess = DBClass.GetUserAccessLevelForPhase(CurrentUserID, entityId);
                    return (phaseAccess != "None", phaseAccess == "Edit" || isAdmin);

                case "project":
                    string projectAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, entityId);
                    return (projectAccess != "None", projectAccess == "Edit" || isAdmin);

                default:
                    return (false, false);
            }
        }

        public IActionResult OnGet(string entityType = null, int entityId = 0)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check if user is admin, redirect if not
            if (!IsAdmin)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // If entityType and entityId are provided, load documents for that specific entity
            if (!string.IsNullOrEmpty(entityType) && entityId > 0)
            {
                try
                {
                    // Load documents for the specific entity
                    Documents = DBClass.GetDocumentsByEntityId(entityType, entityId);
                    _logger.LogInformation($"Loaded {Documents.Count} documents for {entityType} {entityId}");
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, $"Error loading documents for {entityType} {entityId}");
                    Documents = new List<DocumentModel>();
                    TempData["ErrorMessage"] = "Error loading documents: " + ex.Message;
                }
            }
            else
            {
                // No specific entity provided, load all documents (admin only)
                LoadDocuments();
            }

            // Load user names for display
            LoadUserNames();

            return Page();
        }

        private void LoadUserNames()
        {
            // Clear existing user names
            UserNames.Clear();

            // Get all unique user IDs from the documents
            var userIds = Documents.Select(d => d.UploadedBy).Distinct().ToList();

            // Load names for each user ID
            foreach (var userId in userIds)
            {
                UserNames[userId] = DBClass.GetUserDisplayName(userId);
            }
        }

        private void LoadDocuments()
        {
            // Since only admins can access this page, just load all documents
            Documents = DBClass.GetAllDocuments();

            // Load user names for display
            var userIds = Documents.Select(d => d.UploadedBy).Distinct().ToList();
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

        public bool CanUserEditDocument(DocumentModel document)
        {
            // Only admins can edit documents
            return IsAdmin;
        }

        public async Task<IActionResult> OnPostUploadDocumentAsync(IFormFile file)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check if user is admin, redirect if not
            if (!IsAdmin)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Log the incoming values for debugging
            _logger.LogInformation($"Upload started for EntityType: {EntityType}, EntityId: {EntityId}");

            // Enhanced validation
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "No file selected";
                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
            }

            // Validate file size
            if (file.Length > 52428800) // 50MB in bytes
            {
                TempData["ErrorMessage"] = "File size exceeds the 50MB limit";
                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
            }

            // Validate file extension
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "File type is not supported";
                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
            }

            try
            {
                // Generate appropriate folder path
                string folderPath = $"{EntityType.ToLower()}/{EntityId}";
                _logger.LogInformation($"Uploading to folder path: {folderPath}");

                // Upload to blob storage
                string blobName = await _blobStorageService.UploadDocumentAsync(file, folderPath);
                _logger.LogInformation($"File uploaded to blob storage as: {blobName}");

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
                    IsArchived = false
                };

                // Set the appropriate entity ID with lowercase comparison for safety
                switch (EntityType.ToLower())
                {
                    case "grant":
                        document.GrantID = EntityId;
                        break;
                    case "phase":
                        document.PhaseID = EntityId;
                        break;
                    case "project":
                        document.ProjectID = EntityId;
                        break;
                    case "task":
                        document.TaskID = EntityId;
                        break;
                    default:
                        _logger.LogWarning($"Unknown entity type: {EntityType}");
                        break;
                }

                // Log document properties before saving
                _logger.LogInformation($"Saving document: FileName={document.FileName}, " +
                    $"GrantID={document.GrantID}, PhaseID={document.PhaseID}, " +
                    $"ProjectID={document.ProjectID}, TaskID={document.TaskID}");

                // Save to database
                int documentId = DBClass.InsertDocument(document);
                _logger.LogInformation($"Document saved with ID: {documentId}");

                TempData["SuccessMessage"] = "File uploaded successfully";
                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file for {EntityType} {EntityId}", EntityType, EntityId);
                TempData["ErrorMessage"] = $"Error uploading file: {ex.Message}";
                return RedirectToPage(new { entityType = EntityType, entityId = EntityId });
            }
        }

        public async Task<IActionResult> OnGetDeleteDocumentAsync(int documentId)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check if user is admin, redirect if not
            if (!IsAdmin)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            var document = DBClass.GetDocumentById(documentId);

            if (document == null)
            {
                TempData["ErrorMessage"] = "Document not found";
                return RedirectToPage();
            }

            try
            {
                // Delete from blob storage
                await _blobStorageService.DeleteDocumentAsync(document.BlobName);

                // Soft delete in database
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

            return RedirectToPage();
        }
    }
}
