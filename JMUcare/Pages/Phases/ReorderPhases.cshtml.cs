// File: JMUcare/Pages/Phases/ReorderPhases.cshtml.cs
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace JMUcare.Pages.Phases
{
    public class ReorderPhasesModel : PageModel
    {
        [BindProperty]
        public int GrantId { get; set; }

        [BindProperty]
        public List<int> PhaseIds { get; set; }

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public IActionResult OnGet()
        {
            return Page();
        }

        public IActionResult OnPost()
        {
            if (CurrentUserID == 0)
            {
                return Unauthorized();
            }

            // Check if user has permission to reorder phases
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, GrantId);
            bool isAdmin = DBClass.IsUserAdmin(CurrentUserID);

            if (accessLevel != "Edit" && !isAdmin)
            {
                return Forbid();
            }

            // Ensure we have proper data
            if (PhaseIds == null || PhaseIds.Count == 0)
            {
                return BadRequest("No phase IDs provided");
            }

            // Initialize positions if needed
            DBClass.InitializePhasePositions(GrantId);

            // Update positions for all phases in the list
            for (int i = 0; i < PhaseIds.Count; i++)
            {
                int phaseId = PhaseIds[i];
                int newPosition = i + 1; // Position is 1-based

                // Update the phase position
                DBClass.UpdatePhasePosition(phaseId, newPosition);
            }

            // Use the UpdatePhaseStatusesForGrant method instead
            DBClass.UpdatePhaseStatusesForGrant(GrantId);

            return RedirectToPage("/Grants/View", new { id = GrantId });
        }
    }
}
