using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System;
using System.Data.SqlClient;

namespace JMUcare.Pages.Projects
{
    public class EditTaskModel : PageModel
    {
        [BindProperty]
        public ProjectTaskModel Task { get; set; }

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public IActionResult OnPost()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check permissions
            bool hasPermission = HasEditPermissionForProject(CurrentUserID, Task.ProjectID);
            if (!hasPermission)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Update task
            UpdateProjectTask(Task);

            // Redirect to project view
            return RedirectToPage("/Projects/View", new { id = Task.ProjectID });
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

        private void UpdateProjectTask(ProjectTaskModel task)
        {
            using (SqlConnection connection = new SqlConnection(DBClass.JMUcareDBConnString))
            {
                string sqlQuery = @"
                    UPDATE Project_Task 
                    SET TaskContent = @TaskContent, 
                        DueDate = @DueDate, 
                        Status = @Status
                    WHERE TaskID = @TaskID";

                using (SqlCommand cmd = new SqlCommand(sqlQuery, connection))
                {
                    cmd.Parameters.AddWithValue("@TaskID", task.TaskID);
                    cmd.Parameters.AddWithValue("@TaskContent", task.TaskContent);
                    cmd.Parameters.AddWithValue("@DueDate", task.DueDate);
                    cmd.Parameters.AddWithValue("@Status", task.Status);

                    connection.Open();
                    cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
