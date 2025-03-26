using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;

namespace JMUcare.Pages.Grants
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public List<GrantModel> Grants { get; set; } = new List<GrantModel>();

        public int CurrentUserID
        {
            get
            {
                // Retrieve the user ID from session state
                return HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
            }
        }

        public void OnGet()
        {
            _logger.LogInformation("OnGet called. CurrentUserID: {CurrentUserID}", CurrentUserID);

            if (CurrentUserID == 0)
            {
                _logger.LogWarning("CurrentUserID is 0. Redirecting to login page.");
                // Handle the case where the user ID is not set in session
                // Redirect to login page or show an error message
                // For example:
                // RedirectToPage("/Account/Login");
            }
            else
            {
                Grants = DBClass.GetGrantsForUser(CurrentUserID) ?? new List<GrantModel>();
                _logger.LogInformation("Grants retrieved: {GrantsCount}", Grants.Count);
            }
        }

        public string GetGrantPageForUser(int grantId)
        {
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, grantId);

            if (accessLevel == "Edit")
            {
                return "/Grants/Edit";
            }
            else
            {
                return "/Grants/View";
            }
        }

        public bool IsUserAdmin()
        {
            return DBClass.IsUserAdmin(CurrentUserID);
        }
    }
}
