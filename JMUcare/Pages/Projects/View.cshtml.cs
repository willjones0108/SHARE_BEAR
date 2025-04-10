using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace JMUcare.Pages.Projects
{
    public class ViewModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ProjectModel Project { get; set; }
        public List<ProjectTaskModel> Tasks { get; set; }
        public int PhaseId { get; set; }
        public int? GrantId { get; set; }
        public string PhaseName { get; set; }
        public string GrantName { get; set; }
        public bool CanEditProject { get; set; }
        public bool CanAddTask { get; set; }
        public List<DbUserModel> Users { get; set; }

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public IActionResult OnGet()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            try
            {
                // Get project data and associated tasks
                Project = DBClass.GetProjectById(Id);

                if (Project == null)
                {
                    // Instead of returning NotFound(), set an error message and continue
                    TempData["ErrorMessage"] = "Project not found or could not be loaded.";
                    return Page(); // Return the page with the error message
                }

                // Check permissions - user must have project edit/view permission, phase edit/view permission, grant view/edit permission or admin role
                if (!HasAccessToProject(CurrentUserID, Id))
                {
                    return RedirectToPage("/Shared/AccessDenied");
                }

                // Initialize properties with default values to prevent nulls
                PhaseName = string.Empty;
                GrantName = string.Empty;
                Tasks = new List<ProjectTaskModel>();

                // Get related data
                Tasks = DBClass.GetTasksByProjectId(Id, CurrentUserID) ?? new List<ProjectTaskModel>();
                GetRelatedInfo();

                // Set permission flags
                CanEditProject = HasEditPermission(CurrentUserID, Id);
                CanAddTask = CanEditProject; // Same permission for adding tasks as editing project

                // Get all users for task assignment
                Users = DBClass.GetUsers();

                return Page();
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error loading project: {ex.Message}");

                // Set an error message
                TempData["ErrorMessage"] = "An error occurred while loading the project data.";
                return Page();
            }
        }





        private void GetRelatedInfo()
        {
            PhaseId = Project.PhaseID;
            GrantId = Project.GrantID;

            // Get phase name
            if (PhaseId > 0)
            {
                var phase = DBClass.GetPhaseById(PhaseId);
                PhaseName = phase?.PhaseName ?? string.Empty;

                // If grant ID is not set directly on project, get it from phase
                if (!GrantId.HasValue && phase != null)
                {
                    GrantId = DBClass.GetGrantIdByPhaseId(PhaseId);
                }
            }

            // Get grant name if we have a grant ID
            if (GrantId.HasValue)
            {
                var grant = DBClass.GetGrantById(GrantId.Value);
                GrantName = grant?.GrantTitle ?? string.Empty;
            }
            else
            {
                GrantName = string.Empty;
            }
        }



        private bool HasAccessToProject(int userId, int projectId)
        {
            // Check if user is admin
            if (DBClass.IsUserAdmin(userId))
            {
                return true;
            }

            // Check project-level permission
            string projectAccess = DBClass.GetUserAccessLevelForProject(userId, projectId);
            if (projectAccess == "Edit" || projectAccess == "Read")
            {
                return true;
            }

            // Check phase-level permission
            if (PhaseId > 0)
            {
                string phaseAccess = DBClass.GetUserAccessLevelForPhase(userId, PhaseId);
                if (phaseAccess == "Edit" || phaseAccess == "Read")
                {
                    return true;
                }
            }

            // Check grant-level permission
            if (GrantId.HasValue)
            {
                string grantAccess = DBClass.GetUserAccessLevelForGrant(userId, GrantId.Value);
                if (grantAccess == "Edit" || grantAccess == "Read")
                {
                    return true;
                }
            }

            return false;
        }

        private bool HasEditPermission(int userId, int projectId)
        {
            // Check if user is admin
            if (DBClass.IsUserAdmin(userId))
            {
                return true;
            }

            // Check project-level edit permission
            string projectAccess = DBClass.GetUserAccessLevelForProject(userId, projectId);
            if (projectAccess == "Edit")
            {
                return true;
            }

            // Check phase-level edit permission
            if (PhaseId > 0)
            {
                string phaseAccess = DBClass.GetUserAccessLevelForPhase(userId, PhaseId);
                if (phaseAccess == "Edit")
                {
                    return true;
                }
            }

            // Check grant-level edit permission
            if (GrantId.HasValue)
            {
                string grantAccess = DBClass.GetUserAccessLevelForGrant(userId, GrantId.Value);
                if (grantAccess == "Edit")
                {
                    return true;
                }
            }

            return false;
        }
        public IActionResult OnPostArchiveGrant(int grantId)
        {
            // Check if user has permission
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, grantId);
            if (accessLevel != "Edit" && !DBClass.IsUserAdmin(CurrentUserID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            if (DBClass.ArchiveGrant(grantId))
            {
                TempData["SuccessMessage"] = "Grant and its associated phases, projects, and tasks archived successfully.";
                return RedirectToPage("/Grants/Index");
            }
            else
            {
                TempData["ErrorMessage"] = "An error occurred while archiving the grant.";
                return RedirectToPage(new { id = grantId });
            }
        }

        public IActionResult OnPostDeleteTask(int taskId)
        {
            if (DBClass.DeleteTask(taskId))
            {
                TempData["SuccessMessage"] = "Task deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the task.";
            }

            return RedirectToPage(new { id = Id });
        }
        public IActionResult OnPostArchiveTask(int taskId)
        {
            if (DBClass.ArchiveTask(taskId))
            {
                TempData["SuccessMessage"] = "Task archived successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "An error occurred while archiving the task.";
            }

            return RedirectToPage(new { id = Id });
        }
        public async Task<IActionResult> OnPostUploadDocumentAsync(IFormFile file, string entityType, int entityId)
        {
            if (CurrentUserID == 0)
                return RedirectToPage("/HashedLogin/HashedLogin");

            // Check permissions
            string accessLevel = DBClass.GetUserAccessLevelForProject(CurrentUserID, entityId);
            bool isAdmin = DBClass.IsUserAdmin(CurrentUserID);
            if (accessLevel != "Edit" && !isAdmin)
                return RedirectToPage("/Shared/AccessDenied");

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "No file was selected for upload.";
                return RedirectToPage(new { id = entityId });
            }

            try
            {
                var blobService = new JMUcare.Services.BlobStorageService(
                    HttpContext.RequestServices.GetRequiredService<IConfiguration>());

                // Determine upload folder path based on entity type
                string folderPath = entityType.ToLower() switch
                {
                    "project" => $"projects/{entityId}",
                    "phase" => $"phases/{entityId}",
                    "grant" => $"grants/{entityId}",
                    _ => "other"
                };

                // Upload to blob storage
                string blobName = await blobService.UploadDocumentAsync(file, folderPath);

                // Generate a SAS URL with time-limited access
                string sasUrl = await blobService.GenerateSasTokenAsync(blobName, TimeSpan.FromHours(24));

                // Save document metadata to database
                var document = new DocumentModel
                {
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadedDate = DateTime.UtcNow,
                    UploadedBy = CurrentUserID,
                    BlobUrl = sasUrl,
                    BlobName = blobName,
                    ProjectID = entityId
                };

                // Save to database
                int documentId = DBClass.InsertDocument(document);

                if (documentId > 0)
                {
                    TempData["SuccessMessage"] = "Document uploaded successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to save document metadata.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error uploading document: {ex.Message}";
            }

            return RedirectToPage(new { id = entityId });
        }

        public async Task<IActionResult> OnGetDeleteDocumentAsync(int documentId, string entityType, int entityId)
        {
            if (CurrentUserID == 0)
                return RedirectToPage("/HashedLogin/HashedLogin");

            // Get the document
            var document = DBClass.GetDocumentById(documentId);
            if (document == null)
            {
                TempData["ErrorMessage"] = "Document not found.";
                return RedirectToPage(new { id = entityId });
            }

            // Check if user has permission to delete
            bool canDelete = false;

            // Users can delete their own uploads
            if (document.UploadedBy == CurrentUserID)
                canDelete = true;

            // Admins can delete any document
            if (DBClass.IsUserAdmin(CurrentUserID))
                canDelete = true;

            // Editors can delete documents in their entity
            if (!canDelete)
            {
                string projectAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, entityId);
                canDelete = projectAccess == "Edit";
            }

            if (!canDelete)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                var blobService = new JMUcare.Services.BlobStorageService(
                    HttpContext.RequestServices.GetRequiredService<IConfiguration>());

                // Delete from blob storage
                await blobService.DeleteDocumentAsync(document.BlobName);

                // Delete from database (or mark as archived)
                bool success = DBClass.ArchiveDocument(documentId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Document deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Error deleting document from database.";
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting document: {ex.Message}";
            }

            return RedirectToPage(new { id = entityId });
        }
        public IActionResult OnPostArchiveProject(int projectId)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check if user has edit permission
            if (!HasEditPermission(CurrentUserID, projectId))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                // Call DBClass method to archive project and delete associated data
                bool success = DBClass.ArchiveProjectAndTasks(projectId);

                if (success)
                {
                    TempData["SuccessMessage"] = "Project archived successfully, including all associated documents and permissions.";

                    // Redirect to project listing page
                    if (GrantId.HasValue)
                    {
                        return RedirectToPage("/Grants/View", new { id = GrantId.Value });
                    }
                    else
                    {
                        return RedirectToPage("/Grants/ProjectIndex");
                    }
                }
                else
                {
                    TempData["ErrorMessage"] = "An error occurred while archiving the project.";
                    return RedirectToPage(new { id = projectId });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error archiving project: {ex.Message}";
                return RedirectToPage(new { id = projectId });
            }
        }


    }
}
