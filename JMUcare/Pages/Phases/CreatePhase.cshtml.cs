using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace JMUcare.Pages.Phases
{
    public class CreatePhasesModel : PageModel
    {
        [BindProperty]
        public PhaseModel Phase { get; set; }

        public List<DbUserModel> Users { get; set; }
        public List<GrantModel> Grants { get; set; } // New property

        public List<SelectListItem> StatusOptions { get; private set; }

        public int CurrentUserID
        {
            get
            {
                // Retrieve the user ID from session state
                return HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
            }
        }

        public CreatePhasesModel()
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

        public IActionResult OnGet()
        {
            // Check if user is logged in
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/Account/Login");
            }

            // Check if user is an admin
            if (!DBClass.IsUserAdmin(CurrentUserID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Get list of users and grants for dropdowns
            Users = DBClass.GetUsers(); // this gets non-archived users
            Grants = DBClass.GetGrantsForUser(CurrentUserID); // this gets grants for the current user
            return Page();
        }

        public IActionResult OnPost()
        {
            // Re-check admin permissions on post as well for security
            if (CurrentUserID == 0 || !DBClass.IsUserAdmin(CurrentUserID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            if (!ModelState.IsValid)
            {
                Users = DBClass.GetUsers(); // repopulate dropdown
                Grants = DBClass.GetGrantsForUser(CurrentUserID); // repopulate dropdown
                return Page();
            }

            // Set the current user as the creator if not specified
            if (Phase.CreatedBy == 0)
            {
                Phase.CreatedBy = CurrentUserID;
            }

            int phaseId = DBClass.InsertPhase(Phase);

            // Add Phase Lead as editor
            DBClass.InsertPhasePermission(phaseId, Phase.PhaseLeadID, "Edit");

            // Optionally also give CreatedBy some permission
            DBClass.InsertPhasePermission(phaseId, Phase.CreatedBy, "Edit");

            // Insert Grant_Phase record
            DBClass.InsertGrantPhase(Phase.GrantID, phaseId);

            return RedirectToPage("/Phases/Index");
        }
    }
}
