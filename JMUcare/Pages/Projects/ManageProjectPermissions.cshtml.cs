using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using System.Collections.Generic;
using System.Linq;

namespace JMUcare.Pages.Projects
{
    public class ManagePermissionsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public ProjectModel Project { get; set; }
        public List<(DbUserModel User, string AccessLevel)> CurrentPermissions { get; set; }
        public List<DbUserModel> AvailableUsers { get; set; }

        [BindProperty]
        public ProjectPermissionModel NewPermission { get; set; } = new ProjectPermissionModel();

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public IActionResult OnGet()
        {
            if (CurrentUserID == 0)
                return RedirectToPage("/Account/Login");

            Project = DBClass.GetProjectById(Id);
            if (Project == null)
                return NotFound();

            // Check if user has permission to manage access
            string accessLevel = DBClass.GetUserAccessLevelForProject(CurrentUserID, Id);
            if (accessLevel != "Edit" && !DBClass.IsUserAdmin(CurrentUserID))
                return RedirectToPage("/AccessDenied");

            LoadPageData();
            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid || CurrentUserID == 0)
            {
                LoadPageData(); // Ensure AvailableUsers is populated when returning the page
                return Page();
            }

            if (!DBClass.IsUserAdmin(CurrentUserID) &&
                DBClass.GetUserAccessLevelForProject(CurrentUserID, Id) != "Edit")
            {
                return RedirectToPage("/AccessDenied");
            }

            DBClass.InsertProjectPermission(Id, NewPermission.UserID, NewPermission.AccessLevel);
            return RedirectToPage("/Projects/View", new { id = Id });
        }

        public IActionResult OnPostRemove(int userId)
        {
            if (CurrentUserID == 0)
                return RedirectToPage("/Account/Login");

            if (!DBClass.IsUserAdmin(CurrentUserID) &&
                DBClass.GetUserAccessLevelForProject(CurrentUserID, Id) != "Edit")
                return RedirectToPage("/AccessDenied");

            DBClass.InsertProjectPermission(Id, userId, "None");
            return RedirectToPage("/Projects/View", new { id = Id });
        }

        private void LoadPageData()
        {
            CurrentPermissions = DBClass.GetProjectUserPermissions(Id)
                .Where(u => !DBClass.IsUserAdmin(u.User.UserID))
                .ToList() ?? new List<(DbUserModel User, string AccessLevel)>();

            var currentUserIds = CurrentPermissions.Select(p => p.User.UserID).ToList();
            AvailableUsers = DBClass.GetUsers()
                .Where(u => !DBClass.IsUserAdmin(u.UserID) && !currentUserIds.Contains(u.UserID))
                .ToList() ?? new List<DbUserModel>();
        }
    }

    public class ProjectPermissionModel
    {
        public int ProjectID { get; set; }
        public int UserID { get; set; }
        public string AccessLevel { get; set; }
    }
}
