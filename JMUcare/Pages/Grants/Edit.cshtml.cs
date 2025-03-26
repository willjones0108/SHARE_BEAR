using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace JMUcare.Pages.Grants
{
    public class EditModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        [BindProperty]
        public GrantModel Grant { get; set; }

        public List<DbUserModel> Users { get; set; }

        public int CurrentUserID
        {
            get
            {
                return HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
            }
        }

        public IActionResult OnGet()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/Account/Login");
            }

            // Get users for the dropdown
            Users = DBClass.GetUsers();

            // Get the grant details using DBClass method to properly populate fields
            Grant = DBClass.GetGrantById(Id);

            if (Grant == null)
            {
                return NotFound();
            }

            // Check if user has edit permission
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, Id);

            if (accessLevel != "Edit")
            {
                return RedirectToPage("/Grants/View", new { id = Id });
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Users = DBClass.GetUsers();
                return Page();
            }

            // Check if user has edit permission
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, Id);

            if (accessLevel != "Edit")
            {
                return RedirectToPage("/AccessDenied");
            }

            // Get the original grant to check for grant lead changes
            var originalGrant = DBClass.GetGrantById(Id);

            // Check if grant lead has changed
            if (originalGrant.GrantLeadID != Grant.GrantLeadID)
            {
                // If the grant lead has changed, update permissions accordingly

                // 1. Check if the old lead had explicit permission (they might have been an admin)
                string oldLeadAccess = DBClass.GetUserAccessLevelForGrant(originalGrant.GrantLeadID, Id);

                // 2. Remove old grant lead's permission if they had explicit permission
                // This is done by setting a new permission with "None" access which will
                // effectively remove their access (unless they are an admin)
                if (oldLeadAccess != "None" && !DBClass.IsUserAdmin(originalGrant.GrantLeadID))
                {
                    // Use UpdateGrantPermission to effectively remove access
                    DBClass.UpdateGrantPermission(Id, originalGrant.GrantLeadID, "None");
                }

                // 3. Check if the new lead is an admin
                bool isNewLeadAdmin = DBClass.IsUserAdmin(Grant.GrantLeadID);

                // 4. Only add permission for the new lead if they're not an admin
                // Admins already have access to all grants, so no need to add explicit permission
                if (!isNewLeadAdmin)
                {
                    // Add "Edit" permission for the new lead
                    DBClass.UpdateGrantPermission(Id, Grant.GrantLeadID, "Edit");
                }
            }

            // Update grant in the database
            DBClass.UpdateGrant(Grant);

            return RedirectToPage("/Grants/Index");
        }

    }
}
