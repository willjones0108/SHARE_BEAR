using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System;
using System.Threading;

namespace JMUcare.Pages.Projects
{
    public class AddTaskModel : PageModel
    {
        [BindProperty]
        public int ProjectID { get; set; }

        [BindProperty]
        public string TaskContent { get; set; }

        [BindProperty]
        public DateTime DueDate { get; set; }

        [BindProperty]
        public string Status { get; set; }

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public IActionResult OnPost()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check permissions
            bool hasPermission = HasEditPermissionForProject(CurrentUserID, ProjectID);
            if (!hasPermission)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                // Create task model from form fields
                var task = new ProjectTaskModel
                {
                    ProjectID = ProjectID,
                    TaskContent = TaskContent,
                    DueDate = DueDate,
                    Status = Status
                };

                // Insert task
                DBClass.InsertProjectTask(task);

                // Add a small delay to ensure database operations complete
                // This helps prevent the race condition where data retrieval happens before 
                // the insert operation is fully committed
                Thread.Sleep(500); // 500ms delay

                // Add success message to TempData that can be displayed on the View page
                TempData["SuccessMessage"] = "Task added successfully.";

                // Redirect to project view
                return RedirectToPage("/Projects/View", new { id = ProjectID });
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error adding task: {ex.Message}");

                // Add error message to TempData
                TempData["ErrorMessage"] = "An error occurred while adding the task.";

                // Redirect to project view
                return RedirectToPage("/Projects/View", new { id = ProjectID });
            }
        }

        private bool HasEditPermissionForProject(int userId, int projectId)
        {
            // Check if user is admin
            if (DBClass.IsUserAdmin(userId))
            {
                return true;
            }

            // Get project details to check related permissions
            ProjectModel project = DBClass.GetProjectById(projectId);
            if (project == null) return false;

            // Check project-level permission
            string projectAccess = DBClass.GetUserAccessLevelForProject(userId, projectId);
            if (projectAccess == "Edit")
            {
                return true;
            }

            // Check phase-level permission if applicable
            if (project.PhaseID > 0)
            {
                string phaseAccess = DBClass.GetUserAccessLevelForPhase(userId, project.PhaseID);
                if (phaseAccess == "Edit")
                {
                    return true;
                }
            }

            // Check grant-level permission if applicable
            if (project.GrantID.HasValue)
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
