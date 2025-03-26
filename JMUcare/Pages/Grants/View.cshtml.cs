using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;

namespace JMUcare.Pages.Grants
{
    public class ViewModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public GrantModel Grant { get; set; }

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

            // Get the grant details
            Grant = GetGrantById(Id);

            if (Grant == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this grant
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, Id);

            if (accessLevel == "None")
            {
                return RedirectToPage("/AccessDenied");
            }

            return Page();
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
    }
}
