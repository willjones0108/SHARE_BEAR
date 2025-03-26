using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Collections.Generic;

namespace JMUcare.Pages.Phases
{
    public class IndexModel : PageModel
    {
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(ILogger<IndexModel> logger)
        {
            _logger = logger;
        }

        public List<PhaseModel> Phases { get; set; } = new List<PhaseModel>();

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
                Phases = DBClass.GetPhasesForUser(CurrentUserID) ?? new List<PhaseModel>();
                _logger.LogInformation("Phases retrieved: {PhasesCount}", Phases.Count);
            }
        }

        public string GetPhasePageForUser(int phaseId)
        {
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, phaseId);

            if (accessLevel == "Edit")
            {
                return "/Phases/Edit";
            }
            else
            {
                return "/Phases/View";
            }
        }

        public bool IsUserAdmin()
        {
            return DBClass.IsUserAdmin(CurrentUserID);
        }
    }
}