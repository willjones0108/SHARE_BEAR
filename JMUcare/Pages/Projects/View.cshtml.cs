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
                Tasks = DBClass.GetTasksByProjectId(Id) ?? new List<ProjectTaskModel>();
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
    }
}
