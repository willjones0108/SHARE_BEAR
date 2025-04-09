using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;

namespace JMUcare.Pages.Sections
{
    public class EditProjectModel : PageModel
    {
        [BindProperty]
        public ProjectModel Project { get; set; }

        public List<PhaseModel> Phases { get; set; } // Added for phase selection
        public List<GrantModel> Grants { get; set; } // Added for grant selection

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public IActionResult OnGet(int id)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            Project = DBClass.GetProjectById(id);

            if (Project == null)
            {
                TempData["ErrorMessage"] = "Project not found.";
                return RedirectToPage("/Projects/Index");
            }

            if (!HasEditPermissionForProject(CurrentUserID, id))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Load phases and grants for dropdowns
            Phases = DBClass.GetPhasesForUser(CurrentUserID);
            Grants = DBClass.GetGrantsForUser(CurrentUserID);

            return Page();
        }

        public IActionResult OnPost()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            if (!ModelState.IsValid)
            {
                // Reload phases and grants for dropdowns
                Phases = DBClass.GetPhasesForUser(CurrentUserID);
                Grants = DBClass.GetGrantsForUser(CurrentUserID);
                return Page();
            }

            if (!HasEditPermissionForProject(CurrentUserID, Project.ProjectID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                // Get current project to preserve fields we're not updating
                var currentProject = DBClass.GetProjectById(Project.ProjectID);
                if (currentProject != null)
                {
                    // Preserve CreatedBy field
                    Project.CreatedBy = currentProject.CreatedBy;
                }

                // Update the project
                DBClass.UpdateProject(Project);

                // If phase has changed, update the phase-project relationship
                if (currentProject != null && currentProject.PhaseID != Project.PhaseID)
                {
                    // Logic to update phase-project relationship would go here
                    // This might require custom DBClass methods
                    DBClass.InsertPhaseProject(Project.PhaseID, Project.ProjectID);
                }

                TempData["SuccessMessage"] = "Project updated successfully.";
                return RedirectToPage("/Projects/View", new { id = Project.ProjectID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";

                // Reload phases and grants for dropdowns
                Phases = DBClass.GetPhasesForUser(CurrentUserID);
                Grants = DBClass.GetGrantsForUser(CurrentUserID);
                return Page();
            }
        }

        public bool HasEditPermissionForProject(int userId, int projectId)
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
            var project = DBClass.GetProjectById(projectId);
            if (project != null && project.PhaseID > 0)
            {
                string phaseAccess = DBClass.GetUserAccessLevelForPhase(userId, project.PhaseID);
                if (phaseAccess == "Edit")
                {
                    return true;
                }
            }

            // Check grant-level edit permission
            if (project != null && project.GrantID.HasValue)
            {
                string grantAccess = DBClass.GetUserAccessLevelForGrant(userId, project.GrantID.Value);
                if (grantAccess == "Edit")
                {
                    return true;
                }
            }

            return false;
        }
    }
}
