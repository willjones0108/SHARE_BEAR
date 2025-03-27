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

        [BindProperty(SupportsGet = true)]
        public int PhaseId { get; set; }

        public int PreSelectedPhaseId { get; set; }

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

            // Check permissions (allow admins, grant editors, and phase editors)
            bool hasPermission = DBClass.IsUserAdmin(CurrentUserID) || DBClass.IsGrantEditor(CurrentUserID);

            // If not an admin or grant editor, check if user is a phase editor for the specified phase
            if (!hasPermission && PhaseId > 0)
            {
                string accessLevel = DBClass.GetUserAccessLevelForPhase(CurrentUserID, PhaseId);
                hasPermission = (accessLevel == "Edit");
            }

            if (!hasPermission)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Get list of users and phases for dropdowns
            Users = DBClass.GetUsers(); // this gets non-archived users
            Phases = DBClass.GetPhasesForUser(CurrentUserID); // this gets phases for the current user

            // If PhaseId was provided, store it for use in the form
            if (PhaseId > 0)
            {
                PreSelectedPhaseId = PhaseId;
                // Pre-select the phase in the model
                if (Project == null)
                {
                    Project = new ProjectModel();
                }
                Project.PhaseID = PhaseId;
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            // Re-check permissions on post
            bool hasPermission = DBClass.IsUserAdmin(CurrentUserID) || DBClass.IsGrantEditor(CurrentUserID);

            // If not an admin or grant editor, check if user is a phase editor
            if (!hasPermission && Project.PhaseID > 0)
            {
                string accessLevel = DBClass.GetUserAccessLevelForPhase(CurrentUserID, Project.PhaseID);
                hasPermission = (accessLevel == "Edit");
            }

            if (!hasPermission)
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            if (!ModelState.IsValid)
            {
                Users = DBClass.GetUsers(); // repopulate dropdown
                Phases = DBClass.GetPhasesForUser(CurrentUserID); // repopulate dropdown
                PreSelectedPhaseId = Project.PhaseID; // Maintain the pre-selected phase
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

            return RedirectToPage("/phases/index");
        }
    }
}

