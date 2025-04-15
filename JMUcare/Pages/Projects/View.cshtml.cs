// Update to Projects/View.cshtml.cs

using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.DBclass;
using JMUcare.Pages.Dataclasses;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using JMUcare.Services; // Add this for BlobStorageService
using Microsoft.Extensions.Logging; // Add this for logger

namespace JMUcare.Pages.Projects
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

        // All existing properties remain the same
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ProjectModel Project { get; set; }
        public List<ProjectTaskModel> Tasks { get; set; }
        public bool IsFolder { get; set; }
        public bool CanEditProject { get; set; }
        public bool CanAddTask { get; set; }
        public int PhaseId { get; set; }
        public string PhaseName { get; set; }
        public int? GrantId { get; set; }
        public string GrantName { get; set; }

        // Document-related properties remain the same
        public List<DocumentModel> Documents { get; set; } = new List<DocumentModel>();
        public Dictionary<int, string> UserNames { get; set; } = new Dictionary<int, string>();
        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
        public bool IsAdmin => DBClass.IsUserAdmin(CurrentUserID);

        // Existing methods remain the same
        public async Task OnGetAsync()
        {
            if (CurrentUserID == 0)
            {
                Response.Redirect("/HashedLogin/HashedLogin");
                return;
            }

            await LoadProjectData();
        }

        private async Task LoadProjectData()
        {
            // Existing implementation remains the same
            Project = DBClass.GetProjectById(Id);
            if (Project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                Response.Redirect("/Grants/Index");
                return;
            }

            // Check if this is a folder or task
            IsFolder = Project.ProjectType.Equals("folder", StringComparison.OrdinalIgnoreCase);

            // Get the phase this project belongs to
            if (Project.PhaseID > 0)
            {
                PhaseId = Project.PhaseID;
                var phase = DBClass.GetPhaseById(PhaseId);
                if (phase != null)
                {
                    PhaseName = phase.PhaseName;

                    // Get the grant this phase belongs to
                    int? grantIdForPhase = DBClass.GetGrantIdByPhaseId(PhaseId);
                    if (grantIdForPhase.HasValue)
                    {
                        GrantId = grantIdForPhase.Value;
                        var grant = DBClass.GetGrantById(GrantId.Value);
                        if (grant != null)
                        {
                            GrantName = grant.GrantTitle;
                        }
                    }
                }
            }
            else if (Project.GrantID.HasValue)
            {
                // Project directly belongs to a grant
                GrantId = Project.GrantID.Value;
                var grant = DBClass.GetGrantById(GrantId.Value);
                if (grant != null)
                {
                    GrantName = grant.GrantTitle;
                }
            }

            // Get tasks if this is a folder
            if (IsFolder)
            {
                Tasks = DBClass.GetTasksByProjectId(Id);

                // Load documents for this folder
                Documents = DBClass.GetDocumentsByEntityId("project", Id);

                // Get user names for document uploaders
                var userIds = Documents.Select(d => d.UploadedBy).Distinct().ToList();
                foreach (var userId in userIds)
                {
                    UserNames[userId] = GetUserDisplayName(userId);
                }
            }

            // Check permissions
            string accessLevel = DBClass.GetUserAccessLevelForProject(CurrentUserID, Id);
            CanEditProject = accessLevel == "Edit" || IsAdmin;
            CanAddTask = CanEditProject && IsFolder;
        }

        // Keep all existing methods

        // Update the upload document method to use blob storage
        public async Task<IActionResult> OnPostUploadDocumentAsync(IFormFile file, string entityType, int entityId)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Validation
            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "No file selected";
                return RedirectToPage(new { id = entityId });
            }

            // Validate file size (max 50MB)
            if (file.Length > 52428800)
            {
                TempData["ErrorMessage"] = "File size exceeds the 50MB limit";
                return RedirectToPage(new { id = entityId });
            }

            // Validate file extension
            var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".jpg", ".png", ".gif" };
            var fileExtension = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(fileExtension))
            {
                TempData["ErrorMessage"] = "File type is not supported";
                return RedirectToPage(new { id = entityId });
            }

            try
            {
                // Upload to blob storage
                string folderPath = $"project/{entityId}";
                string blobName = await _blobStorageService.UploadDocumentAsync(file, folderPath);

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
                    ProjectID = entityId
                };

                // Save to database
                int documentId = DBClass.InsertDocument(document);

                TempData["SuccessMessage"] = "File uploaded successfully";
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error uploading file");
                TempData["ErrorMessage"] = $"Upload failed: {ex.Message}";
            }

            return RedirectToPage(new { id = entityId });
        }

        // Update the delete document method to handle blob storage
        public async Task<IActionResult> OnGetDeleteDocumentAsync(int documentId, int projectId)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            try
            {
                // Get the document to check permissions
                var document = DBClass.GetDocumentById(documentId);

                if (document == null)
                {
                    TempData["ErrorMessage"] = "Document not found.";
                    return RedirectToPage(new { id = projectId });
                }

                // Check permissions (can delete if admin, owner of the document, or has edit rights)
                bool canDelete = IsAdmin || document.UploadedBy == CurrentUserID || CanEditProject;

                if (!canDelete)
                {
                    TempData["ErrorMessage"] = "You don't have permission to delete this document.";
                    return RedirectToPage(new { id = projectId });
                }

                // Delete from blob storage
                await _blobStorageService.DeleteDocumentAsync(document.BlobName);

                // Archive or delete the document
                bool success = DBClass.ArchiveDocument(documentId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Document deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete the document.";
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error deleting document");
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
            }

            return RedirectToPage(new { id = projectId });
        }

        // Rest of the existing code remains the same
        public string GetUploaderName(int uploaderId)
        {
            if (UserNames.ContainsKey(uploaderId))
            {
                return UserNames[uploaderId];
            }
            return "Unknown User";
        }

        private string GetUserDisplayName(int userId)
        {
            try
            {
                var user = DBClass.GetUserById(userId);
                if (user != null)
                {
                    return $"{user.FirstName} {user.LastName}";
                }
            }
            catch { }
            return "Unknown User";
        }

        public string GetFileIcon(string fileName)
        {
            string extension = Path.GetExtension(fileName).ToLowerInvariant();

            return extension switch
            {
                ".pdf" => "bi-file-earmark-pdf",
                ".doc" or ".docx" => "bi-file-earmark-word",
                ".xls" or ".xlsx" => "bi-file-earmark-excel",
                ".jpg" or ".jpeg" or ".png" or ".gif" => "bi-file-earmark-image",
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
    }
}
