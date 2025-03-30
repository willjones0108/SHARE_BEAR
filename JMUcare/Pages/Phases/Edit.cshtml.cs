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

            if (accessLevel != "Edit")
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Get list of users for dropdowns
            Users = DBClass.GetUsers(); // this gets non-archived users
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
                return Page();
            }

            DBClass.UpdatePhase(Phase);

            return RedirectToPage("/Phases/Index");
        }
    }
}
