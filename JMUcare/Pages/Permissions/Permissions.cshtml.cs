using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using System.Collections.Generic;
using System.Linq;

namespace JMUcare.Pages.Permissions
{
    public class ManageHierarchyModel : PageModel
    {
        public class PermissionViewModel
        {
            public int ItemId { get; set; }
            public string ItemName { get; set; }
            public string ItemType { get; set; }
            public List<UserPermissionViewModel> UserPermissions { get; set; } = new List<UserPermissionViewModel>();
            public List<PermissionViewModel> Children { get; set; } = new List<PermissionViewModel>();
            public bool CanEdit { get; set; }
        }

        public class UserPermissionViewModel
        {
            public int UserId { get; set; }
            public string FullName { get; set; }
            public string AccessLevel { get; set; }
        }

        [BindProperty(SupportsGet = true)]
        public int? GrantId { get; set; }

        public List<GrantModel> Grants { get; set; }
        public PermissionViewModel SelectedGrantPermissions { get; set; }
        public List<DbUserModel> AvailableUsers { get; set; }

        [BindProperty]
        public int NewPermissionItemId { get; set; }

        [BindProperty]
        public string NewPermissionItemType { get; set; }

        [BindProperty]
        public int NewPermissionUserId { get; set; }

        [BindProperty]
        public string NewPermissionAccessLevel { get; set; }

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
        public bool IsAdmin { get; set; }

        public IActionResult OnGet()
        {
            if (CurrentUserID == 0)
                return RedirectToPage("/Account/Login");

            IsAdmin = DBClass.IsUserAdmin(CurrentUserID);

            // Get all grants the user has access to
            Grants = DBClass.GetGrantsForUser(CurrentUserID);

            if (Grants == null || !Grants.Any())
                return Page();

            // If no grant is selected, use the first one the user has access to
            if (!GrantId.HasValue && Grants.Any())
                GrantId = Grants.First().GrantID;

            if (GrantId.HasValue)
            {
                // Build the permission hierarchy for the selected grant
                SelectedGrantPermissions = BuildGrantPermissionHierarchy(GrantId.Value);
            }

            // Get available users for dropdowns
            AvailableUsers = DBClass.GetUsers();

            return Page();
        }

        public IActionResult OnPostAddPermission()
        {
            if (CurrentUserID == 0)
                return RedirectToPage("/Account/Login");

            IsAdmin = DBClass.IsUserAdmin(CurrentUserID);

            // Check user has permission to manage this item
            bool hasPermission = IsAdmin;

            if (!hasPermission)
            {
                switch (NewPermissionItemType)
                {
                    case "Grant":
                        string grantAccess = DBClass.GetUserAccessLevelForGrant(CurrentUserID, NewPermissionItemId);
                        hasPermission = grantAccess == "Edit";
                        break;
                    case "Phase":
                        string phaseAccess = DBClass.GetUserAccessLevelForPhase(CurrentUserID, NewPermissionItemId);
                        hasPermission = phaseAccess == "Edit";
                        break;
                    case "Project":
                        string projectAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, NewPermissionItemId);
                        hasPermission = projectAccess == "Edit";
                        break;
                }
            }

            if (!hasPermission)
                return RedirectToPage("/AccessDenied");

            // Add the permission
            switch (NewPermissionItemType)
            {
                case "Grant":
                    DBClass.UpdateGrantPermission(NewPermissionItemId, NewPermissionUserId, NewPermissionAccessLevel);
                    break;
                case "Phase":
                    DBClass.InsertPhasePermission(NewPermissionItemId, NewPermissionUserId, NewPermissionAccessLevel);
                    break;
                case "Project":
                    DBClass.InsertProjectPermission(NewPermissionItemId, NewPermissionUserId, NewPermissionAccessLevel);
                    break;
            }

            return RedirectToPage(new { grantId = GrantId });
        }

        public IActionResult OnPostRemovePermission(int itemId, string itemType, int userId)
        {
            if (CurrentUserID == 0)
                return RedirectToPage("/Account/Login");

            IsAdmin = DBClass.IsUserAdmin(CurrentUserID);

            // Check user has permission to manage this item
            bool hasPermission = IsAdmin;

            if (!hasPermission)
            {
                switch (itemType)
                {
                    case "Grant":
                        string grantAccess = DBClass.GetUserAccessLevelForGrant(CurrentUserID, itemId);
                        hasPermission = grantAccess == "Edit";
                        break;
                    case "Phase":
                        string phaseAccess = DBClass.GetUserAccessLevelForPhase(CurrentUserID, itemId);
                        hasPermission = phaseAccess == "Edit";
                        break;
                    case "Project":
                        string projectAccess = DBClass.GetUserAccessLevelForProject(CurrentUserID, itemId);
                        hasPermission = projectAccess == "Edit";
                        break;
                }
            }

            if (!hasPermission)
                return RedirectToPage("/AccessDenied");

            // Remove the permission
            switch (itemType)
            {
                case "Grant":
                    DBClass.UpdateGrantPermission(itemId, userId, "None");
                    break;
                case "Phase":
                    DBClass.InsertPhasePermission(itemId, userId, "None");
                    break;
                case "Project":
                    DBClass.InsertProjectPermission(itemId, userId, "None");
                    break;
            }

            return RedirectToPage(new { grantId = GrantId });
        }

        private PermissionViewModel BuildGrantPermissionHierarchy(int grantId)
        {
            // Get grant data
            var grant = DBClass.GetGrantById(grantId);
            if (grant == null)
                return null;

            var result = new PermissionViewModel
            {
                ItemId = grant.GrantID,
                ItemName = grant.GrantTitle,
                ItemType = "Grant",
                CanEdit = IsAdmin || DBClass.GetUserAccessLevelForGrant(CurrentUserID, grantId) == "Edit"
            };

            // Get grant permissions
            var grantPermissions = DBClass.GetGrantUserPermissions(grantId);
            if (grantPermissions != null)
            {
                foreach (var permission in grantPermissions)
                {
                    result.UserPermissions.Add(new UserPermissionViewModel
                    {
                        UserId = permission.User.UserID,
                        FullName = $"{permission.User.FirstName} {permission.User.LastName}",
                        AccessLevel = permission.AccessLevel
                    });
                }
            }

            // Get phases for this grant and build their permissions
            var phases = DBClass.GetPhasesByGrantId(grantId);
            foreach (var phase in phases)
            {
                var phaseViewModel = new PermissionViewModel
                {
                    ItemId = phase.PhaseID,
                    ItemName = phase.PhaseName,
                    ItemType = "Phase",
                    CanEdit = IsAdmin ||
                              DBClass.GetUserAccessLevelForGrant(CurrentUserID, grantId) == "Edit" ||
                              DBClass.GetUserAccessLevelForPhase(CurrentUserID, phase.PhaseID) == "Edit"
                };

                // Get phase permissions
                var phasePermissions = DBClass.GetPhaseUserPermissions(phase.PhaseID);
                if (phasePermissions != null)
                {
                    foreach (var permission in phasePermissions)
                    {
                        phaseViewModel.UserPermissions.Add(new UserPermissionViewModel
                        {
                            UserId = permission.User.UserID,
                            FullName = $"{permission.User.FirstName} {permission.User.LastName}",
                            AccessLevel = permission.AccessLevel
                        });
                    }
                }

                // Get projects for this phase and build their permissions
                var projects = DBClass.GetProjectsByPhaseId(phase.PhaseID);
                foreach (var project in projects)
                {
                    var projectViewModel = new PermissionViewModel
                    {
                        ItemId = project.ProjectID,
                        ItemName = project.Title,
                        ItemType = "Project",
                        CanEdit = IsAdmin ||
                                  DBClass.GetUserAccessLevelForGrant(CurrentUserID, grantId) == "Edit" ||
                                  DBClass.GetUserAccessLevelForPhase(CurrentUserID, phase.PhaseID) == "Edit" ||
                                  DBClass.GetUserAccessLevelForProject(CurrentUserID, project.ProjectID) == "Edit"
                    };

                    // Get project permissions
                    var projectPermissions = DBClass.GetProjectUserPermissions(project.ProjectID);
                    if (projectPermissions != null)
                    {
                        foreach (var permission in projectPermissions)
                        {
                            projectViewModel.UserPermissions.Add(new UserPermissionViewModel
                            {
                                UserId = permission.User.UserID,
                                FullName = $"{permission.User.FirstName} {permission.User.LastName}",
                                AccessLevel = permission.AccessLevel
                            });
                        }
                    }

                    phaseViewModel.Children.Add(projectViewModel);
                }

                result.Children.Add(phaseViewModel);
            }

            return result;
        }
    }
}
