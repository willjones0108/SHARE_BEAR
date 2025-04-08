using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace JMUcare.Pages.Phases
{
    public class EditModel : PageModel
    {
        [BindProperty]
        public PhaseModel Phase { get; set; }

        public List<DbUserModel> Users { get; set; }

        public List<SelectListItem> StatusOptions { get; private set; }

        public int CurrentUserID
        {
            get
            {
                // Retrieve the user ID from session state
                return HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
            }
        }

        // For permissions section
        public List<UserPermissionViewModel> PhasePermissions { get; set; }

        [BindProperty]
        public int NewPermissionUserId { get; set; }

        [BindProperty]
        public string NewPermissionAccessLevel { get; set; }

        public bool IsAdmin { get; set; }

        public class UserPermissionViewModel
        {
            public int UserId { get; set; }
            public string FullName { get; set; }
            public string AccessLevel { get; set; }
        }

        public EditModel()
        {
            // Initialize status options with common phase milestones
            StatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Planning", Text = "Planning" },
                new SelectListItem { Value = "Execution", Text = "Execution" },
                new SelectListItem { Value = "Monitoring", Text = "Monitoring" },
                new SelectListItem { Value = "Closure", Text = "Closure" }
            };
        }

        public IActionResult OnGet(int id)
        {
            // Check if user is logged in
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/Account/Login");
            }

            // Get the phase details
            Phase = DBClass.GetPhaseById(id);

            if (Phase == null)
            {
                return NotFound();
            }

            // Check if user has permission to edit this phase
            string accessLevel = DBClass.GetUserAccessLevelForPhase(CurrentUserID, id);
            IsAdmin = DBClass.IsUserAdmin(CurrentUserID);

            if (accessLevel != "Edit")
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Get list of users for dropdowns
            Users = DBClass.GetUsers(); // this gets non-archived users

            // Get phase permissions
            LoadPhasePermissions(id);

            return Page();
        }

        public IActionResult OnPost()
        {
            // Re-check permissions on post as well for security
            if (CurrentUserID == 0 || DBClass.GetUserAccessLevelForPhase(CurrentUserID, Phase.PhaseID) != "Edit")
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            if (!ModelState.IsValid)
            {
                Users = DBClass.GetUsers(); // repopulate dropdown
                LoadPhasePermissions(Phase.PhaseID);
                return Page();
            }

            DBClass.UpdatePhase(Phase);

            // Store the grant ID before redirecting
            int grantId = Phase.GrantID;

            // Explicitly redirect to Grants/View page with the grantId
            return RedirectToPage("/Grants/View", new { id = grantId });
        }


        public IActionResult OnPostAddPermission(int phaseId)
        {
            // Re-check permissions on post as well for security
            if (CurrentUserID == 0 || DBClass.GetUserAccessLevelForPhase(CurrentUserID, phaseId) != "Edit")
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            if (NewPermissionUserId > 0 && !string.IsNullOrEmpty(NewPermissionAccessLevel))
            {
                DBClass.InsertPhasePermission(phaseId, NewPermissionUserId, NewPermissionAccessLevel);
            }

            // Stay on the edit page
            return RedirectToPage(new { id = phaseId });
        }

        public IActionResult OnPostRemovePermission(int phaseId, int userId)
        {
            // Re-check permissions on post as well for security
            if (CurrentUserID == 0 || DBClass.GetUserAccessLevelForPhase(CurrentUserID, phaseId) != "Edit")
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            DBClass.InsertPhasePermission(phaseId, userId, "None");

            // Stay on the edit page
            return RedirectToPage(new { id = phaseId });
        }


        private void LoadPhasePermissions(int phaseId)
        {
            PhasePermissions = new List<UserPermissionViewModel>();
            var permissions = DBClass.GetPhaseUserPermissions(phaseId);

            if (permissions != null)
            {
                foreach (var permission in permissions)
                {
                    PhasePermissions.Add(new UserPermissionViewModel
                    {
                        UserId = permission.User.UserID,
                        FullName = $"{permission.User.FirstName} {permission.User.LastName}",
                        AccessLevel = permission.AccessLevel
                    });
                }
            }
        }
    }
}
