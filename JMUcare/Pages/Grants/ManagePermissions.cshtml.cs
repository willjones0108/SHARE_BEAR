using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using System.Collections.Generic;
using System.Linq;

namespace JMUcare.Pages.Grants
{
    public class ManagePermissionsModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public GrantModel Grant { get; set; }
        public List<(DbUserModel User, string AccessLevel)> CurrentPermissions { get; set; }
        public List<DbUserModel> AvailableUsers { get; set; }

        [BindProperty]
        public GrantPermissionModel NewPermission { get; set; }

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public IActionResult OnGet()
        {
            if (CurrentUserID == 0)
                return RedirectToPage("/Account/Login");

            Grant = DBClass.GetGrantById(Id);
            if (Grant == null)
                return NotFound();

            // Check if user has permission to manage access
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, Id);
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
                DBClass.GetUserAccessLevelForGrant(CurrentUserID, Id) != "Edit")
            {
                return RedirectToPage("/AccessDenied");
            }

            DBClass.UpdateGrantPermission(Id, NewPermission.UserID, NewPermission.AccessLevel);
            return RedirectToPage(new { id = Id });
        }


        public IActionResult OnPostRemove(int userId)
        {
            if (CurrentUserID == 0)
                return RedirectToPage("/Account/Login");

            if (!DBClass.IsUserAdmin(CurrentUserID) && 
                DBClass.GetUserAccessLevelForGrant(CurrentUserID, Id) != "Edit")
                return RedirectToPage("/AccessDenied");

            DBClass.UpdateGrantPermission(Id, userId, "None");
            return RedirectToPage(new { id = Id });
        }

        private void LoadPageData()
        {
            CurrentPermissions = DBClass.GetGrantUserPermissions(Id)
                .Where(u => !DBClass.IsUserAdmin(u.User.UserID))
                .ToList();

            // Get all non-admin users who don't already have access
            var currentUserIds = CurrentPermissions.Select(p => p.User.UserID).ToList();
            AvailableUsers = DBClass.GetUsers()
                .Where(u => !DBClass.IsUserAdmin(u.UserID) && !currentUserIds.Contains(u.UserID))
                .ToList();
        }
    }
}
