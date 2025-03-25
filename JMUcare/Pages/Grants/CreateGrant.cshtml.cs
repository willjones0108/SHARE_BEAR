using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;

namespace JMUcare.Pages.Grants
{
    public class CreateGrantModel : PageModel
    {
        [BindProperty]
        public GrantModel Grant { get; set; }

        public List<DbUserModel> Users { get; set; }

        public void OnGet()
        {
            // Get list of users for dropdowns
            Users = DBClass.GetUsers(); // this gets non-archived users
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Users = DBClass.GetUsers(); // repopulate dropdown
                return Page();
            }

            int grantId = DBClass.InsertGrant(Grant);

            // Add Grant Lead as editor
            DBClass.InsertGrantPermission(grantId, Grant.GrantLeadID, "Edit");

            // Optionally also give CreatedBy some permission
            DBClass.InsertGrantPermission(grantId, Grant.CreatedBy, "Edit");

            return RedirectToPage("/Grants/Index");
        }

    }
}
