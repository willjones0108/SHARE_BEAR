using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace JMUcare.Pages.Projects
{
    public class CreateProjectModel : PageModel
    {
        [BindProperty]
        public ProjectModel Project { get; set; }

        public List<DbUserModel> Users { get; set; }
        public List<PhaseModel> Phases { get; set; } // New property

        public List<SelectListItem> StatusOptions { get; private set; }

        public int CurrentUserID
        {
            get
            {
                // Retrieve the user ID from session state
                return HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
            }
        }

        public CreateProjectModel()
        {
            // Initialize status options with common project milestones
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

            // Get list of users and phases for dropdowns
            Users = DBClass.GetUsers(); // this gets non-archived users
            Phases = DBClass.GetPhasesForUser(CurrentUserID); // this gets phases for the current user
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
                Phases = DBClass.GetPhasesForUser(CurrentUserID); // repopulate dropdown
                return Page();
            }

            // Set the current user as the creator if not specified
            if (Project.CreatedBy == 0)
            {
                Project.CreatedBy = CurrentUserID;
            }

            int projectId = DBClass.InsertProject(Project);

            // Insert Phase_Project record
            DBClass.InsertPhaseProject(Project.PhaseID, projectId);

            return RedirectToPage("/Projects/Index");
        }
    }
}
