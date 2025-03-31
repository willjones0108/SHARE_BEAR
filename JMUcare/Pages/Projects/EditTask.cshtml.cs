using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JMUcare.Pages.Projects
{
    public class EditTaskModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public ProjectTaskModel Task { get; set; }

        public List<TaskAssignmentViewModel> TaskAssignments { get; set; } = new List<TaskAssignmentViewModel>();
        public List<DbUserModel> AvailableUsers { get; set; } = new List<DbUserModel>();

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public class TaskAssignmentViewModel
        {
            public int UserID { get; set; }
            public string FirstName { get; set; }
            public string LastName { get; set; }
            public string FullName => $"{FirstName} {LastName}";
            public string AccessLevel { get; set; }
        }

        // Changed from private to public
        public bool HasViewPermissionForProject(int userId, int projectId)
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
            if (projectAccess == "View" || projectAccess == "Edit")
            {
                return true;
            }

            // Check phase-level permission if applicable
            if (project.PhaseID > 0)
            {
                string phaseAccess = DBClass.GetUserAccessLevelForPhase(userId, project.PhaseID);
                if (phaseAccess == "View" || phaseAccess == "Edit")
                {
                    return true;
                }
            }

            // Check grant-level permission if applicable
            if (project.GrantID.HasValue)
            {
                string grantAccess = DBClass.GetUserAccessLevelForGrant(userId, project.GrantID.Value);
                if (grantAccess == "View" || grantAccess == "Edit")
                {
                    return true;
                }
            }

            return false;
        }

        public IActionResult OnGet()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Get task details by ID
            Task = GetTaskById(Id);
            if (Task == null)
            {
                TempData["ErrorMessage"] = "Task not found.";
                return RedirectToPage("/Projects/View", new { id = Id });
            }

            // Check view permissions
            if (!HasViewPermissionForProject(CurrentUserID, Task.ProjectID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Load task assignments
            LoadTaskAssignments();

            // Load available users
            LoadAvailableUsers();

            return Page();
        }

        public IActionResult OnPost()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check permissions
            if (!HasEditPermissionForProject(CurrentUserID, Task.ProjectID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                // Update task
                UpdateTask(Task);

                TempData["SuccessMessage"] = "Task updated successfully.";
                return RedirectToPage("/Projects/View", new { id = Task.ProjectID });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return Page();
            }
        }

        public IActionResult OnPostAddUser(int taskId, int userId, string accessLevel)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Get task to check project permissions
            var task = GetTaskById(taskId);
            if (task == null)
            {
                TempData["ErrorMessage"] = "Task not found.";
                return RedirectToPage(new { id = taskId });
            }

            if (!HasEditPermissionForProject(CurrentUserID, task.ProjectID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                var taskUser = new ProjectTaskUserModel
                {
                    TaskID = taskId,
                    UserID = userId,
                    Role = accessLevel // Using Role field to store access level
                };

                AddUserToTask(taskUser);

                TempData["SuccessMessage"] = "User assigned to task successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToPage(new { id = taskId });
        }

        public IActionResult OnPostRemoveUser(int taskId, int userId)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Get task to check project permissions
            var task = GetTaskById(taskId);
            if (task == null)
            {
                TempData["ErrorMessage"] = "Task not found.";
                return RedirectToPage(new { id = taskId });
            }

            if (!HasEditPermissionForProject(CurrentUserID, task.ProjectID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                RemoveUserFromTask(taskId, userId);

                TempData["SuccessMessage"] = "User removed from task successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToPage(new { id = taskId });
        }

        private ProjectTaskModel GetTaskById(int taskId)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(DBClass.JMUcareDBConnString);
            var query = @"
                SELECT * FROM Project_Task
                WHERE TaskID = @TaskID";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", taskId);

            connection.Open();
            using var reader = cmd.ExecuteReader();

            if (reader.Read())
            {
                return new ProjectTaskModel
                {
                    TaskID = reader.GetInt32(reader.GetOrdinal("TaskID")),
                    ProjectID = reader.GetInt32(reader.GetOrdinal("ProjectID")),
                    TaskContent = reader.GetString(reader.GetOrdinal("TaskContent")),
                    DueDate = reader.GetDateTime(reader.GetOrdinal("DueDate")),
                    Status = reader.GetString(reader.GetOrdinal("Status"))
                };
            }

            return null;
        }

        private void UpdateTask(ProjectTaskModel task)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(DBClass.JMUcareDBConnString);
            var query = @"
                UPDATE Project_Task
                SET TaskContent = @TaskContent,
                    DueDate = @DueDate,
                    Status = @Status
                WHERE TaskID = @TaskID";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", task.TaskID);
            cmd.Parameters.AddWithValue("@TaskContent", task.TaskContent);
            cmd.Parameters.AddWithValue("@DueDate", task.DueDate);
            cmd.Parameters.AddWithValue("@Status", task.Status);

            connection.Open();
            cmd.ExecuteNonQuery();
        }

        private void LoadTaskAssignments()
        {
            using var connection = new System.Data.SqlClient.SqlConnection(DBClass.JMUcareDBConnString);
            var query = @"
                SELECT u.UserID, u.FirstName, u.LastName, ptu.Role
                FROM DBUser u
                JOIN Project_Task_User ptu ON u.UserID = ptu.UserID
                WHERE ptu.TaskID = @TaskID";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", Id);

            connection.Open();
            using var reader = cmd.ExecuteReader();

            TaskAssignments.Clear();
            while (reader.Read())
            {
                TaskAssignments.Add(new TaskAssignmentViewModel
                {
                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    AccessLevel = reader.GetString(reader.GetOrdinal("Role"))
                });
            }
        }

        private void LoadAvailableUsers()
        {
            // Get all users who aren't already assigned to this task
            using var connection = new System.Data.SqlClient.SqlConnection(DBClass.JMUcareDBConnString);
            var query = @"
                SELECT u.UserID, u.FirstName, u.LastName, u.Email
                FROM DBUser u
                WHERE u.IsArchived = 0
                AND u.UserID NOT IN (
                    SELECT ptu.UserID
                    FROM Project_Task_User ptu
                    WHERE ptu.TaskID = @TaskID
                )";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", Id);

            connection.Open();
            using var reader = cmd.ExecuteReader();

            AvailableUsers.Clear();
            while (reader.Read())
            {
                AvailableUsers.Add(new DbUserModel
                {
                    UserID = reader.GetInt32(reader.GetOrdinal("UserID")),
                    FirstName = reader.GetString(reader.GetOrdinal("FirstName")),
                    LastName = reader.GetString(reader.GetOrdinal("LastName")),
                    Email = reader.GetString(reader.GetOrdinal("Email"))
                });
            }
        }

        private void AddUserToTask(ProjectTaskUserModel taskUser)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(DBClass.JMUcareDBConnString);
            var query = @"
                INSERT INTO Project_Task_User (TaskID, UserID, Role)
                VALUES (@TaskID, @UserID, @Role)";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", taskUser.TaskID);
            cmd.Parameters.AddWithValue("@UserID", taskUser.UserID);
            cmd.Parameters.AddWithValue("@Role", taskUser.Role);

            connection.Open();
            cmd.ExecuteNonQuery();
        }

        private void RemoveUserFromTask(int taskId, int userId)
        {
            using var connection = new System.Data.SqlClient.SqlConnection(DBClass.JMUcareDBConnString);
            var query = @"
                DELETE FROM Project_Task_User
                WHERE TaskID = @TaskID AND UserID = @UserID";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@TaskID", taskId);
            cmd.Parameters.AddWithValue("@UserID", userId);

            connection.Open();
            cmd.ExecuteNonQuery();
        }

        public IActionResult OnPostDeleteTask()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check permissions
            if (!HasEditPermissionForProject(CurrentUserID, Task.ProjectID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                // Get task data before deletion to use for redirection
                var projectId = Task.ProjectID;

                // Delete task and its users
                bool result = DBClass.DeleteTaskAndUsers(Task.TaskID);

                if (result)
                {
                    TempData["SuccessMessage"] = "Task deleted successfully.";
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete task.";
                }

                return RedirectToPage("/Projects/View", new { id = projectId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                return Page();
            }
        }

        // Changed from private to public
        public bool HasEditPermissionForProject(int userId, int projectId)
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
