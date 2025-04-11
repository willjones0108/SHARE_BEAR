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

        public bool HasViewPermissionForProject(int userId, int projectId)
        {
            if (DBClass.IsUserAdmin(userId))
            {
                return true;
            }

            ProjectModel project = DBClass.GetProjectById(projectId);
            if (project == null) return false;

            string projectAccess = DBClass.GetUserAccessLevelForProject(userId, projectId);
            if (projectAccess == "View" || projectAccess == "Edit")
            {
                return true;
            }

            if (project.PhaseID > 0)
            {
                string phaseAccess = DBClass.GetUserAccessLevelForPhase(userId, project.PhaseID);
                if (phaseAccess == "View" || phaseAccess == "Edit")
                {
                    return true;
                }
            }

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

            Task = DBClass.GetTaskById(Id);
            if (Task == null)
            {
                TempData["ErrorMessage"] = "Task not found.";
                return RedirectToPage("/Projects/View", new { id = Id });
            }

            if (!HasViewPermissionForProject(CurrentUserID, Task.ProjectID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            //DBClass.LoadTaskAssignments(Id, TaskAssignments);
            DBClass.LoadAvailableUsers(Id, AvailableUsers);

            return Page();
        }
        public IActionResult OnPost()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            if (!HasEditPermissionForProject(CurrentUserID, Task.ProjectID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                DBClass.UpdateTask(Task);

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

            var task = DBClass.GetTaskById(taskId);
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
                    Role = accessLevel
                };

                DBClass.AddUserToTask(taskUser);

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

            var task = DBClass.GetTaskById(taskId);
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
                DBClass.RemoveUserFromTask(taskId, userId);

                TempData["SuccessMessage"] = "User removed from task successfully.";
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
            }

            return RedirectToPage(new { id = taskId });
        }

        public bool HasEditPermissionForProject(int userId, int projectId)
        {
            if (DBClass.IsUserAdmin(userId))
            {
                return true;
            }

            ProjectModel project = DBClass.GetProjectById(projectId);
            if (project == null) return false;

            string projectAccess = DBClass.GetUserAccessLevelForProject(userId, projectId);
            if (projectAccess == "Edit")
            {
                return true;
            }

            if (project.PhaseID > 0)
            {
                string phaseAccess = DBClass.GetUserAccessLevelForPhase(userId, project.PhaseID);
                if (phaseAccess == "Edit")
                {
                    return true;
                }
            }

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
