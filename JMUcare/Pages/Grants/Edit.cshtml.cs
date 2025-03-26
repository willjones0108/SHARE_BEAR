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

            // Get the grant details
            Grant = GetGrantById(Id);

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

            // Update grant in the database
            // You'll need to add this method to your DBClass
            UpdateGrant(Grant);

            return RedirectToPage("/Grants/Index");
        }

        private GrantModel GetGrantById(int grantId)
        {
            // Implementation needed to fetch a specific grant by ID
            // You'll need to add this method to your DBClass
            // For now, placeholder implementation
            foreach (var grant in DBClass.GetGrantsForUser(CurrentUserID))
            {
                if (grant.GrantID == grantId)
                {
                    return grant;
                }
            }

            return null;
        }

        private void UpdateGrant(GrantModel grant)
        {
            // Implementation needed to update a grant in the database
            // You'll need to add this method to your DBClass
        }
    }
}
