using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JMUcare.Pages.Projects
{
    public class EditProjectModel : PageModel
    {
        [BindProperty]
        public ProjectModel Project { get; set; }

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
                return Page();
            }

            if (!HasEditPermissionForProject(CurrentUserID, Project.ProjectID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                DBClass.UpdateProject(Project);
                TempData["SuccessMessage"] = "Project updated successfully.";
                return RedirectToPage("/Projects/View", new { id = Project.ProjectID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
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
