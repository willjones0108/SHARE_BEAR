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
        public List<PhaseModel> Phases { get; set; }
        public List<GrantModel> Grants { get; set; } // Added property for grants dropdown

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
            // Initialize project model with default values
            Project = new ProjectModel
            {
                StartDate = DateTime.Today,
                DueDate = DateTime.Today.AddMonths(1),
                TrackingStatus = "Not Started",
                IsArchived = false
            };

            // Initialize status options with common project milestones
            StatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Not Started", Text = "Not Started" },
                new SelectListItem { Value = "In Progress", Text = "In Progress" },
                new SelectListItem { Value = "Completed", Text = "Completed" },
                new SelectListItem { Value = "Blocked", Text = "Blocked" }
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

            // Get lists for dropdowns
            Users = DBClass.GetUsers();
            Phases = DBClass.GetPhasesForUser(CurrentUserID);
            Grants = DBClass.GetGrantsForUser(CurrentUserID);

            // If PhaseId was provided, store it for use in the form
            if (PhaseId > 0)
            {
                PreSelectedPhaseId = PhaseId;

                // Pre-select the phase in the model
                Project.PhaseID = PhaseId;

                // Try to get the associated GrantID for the phase
                int? grantId = DBClass.GetGrantIdByPhaseId(PhaseId);
                if (grantId.HasValue)
                {
                    Project.GrantID = grantId.Value;
                }
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
                Users = DBClass.GetUsers();
                Phases = DBClass.GetPhasesForUser(CurrentUserID);
                Grants = DBClass.GetGrantsForUser(CurrentUserID);
                PreSelectedPhaseId = Project.PhaseID;
                return Page();
            }

            // Set the current user as the creator
            Project.CreatedBy = CurrentUserID;

            try
            {
                int projectId = DBClass.InsertProject(Project);

                // Insert Phase_Project record if a phase is selected
                if (Project.PhaseID > 0)
                {
                    DBClass.InsertPhaseProject(Project.PhaseID, projectId);
                }

                TempData["SuccessMessage"] = "Project created successfully.";
                return RedirectToPage("/Projects/View", new { id = projectId });
            }
            catch (Exception ex)
            {
                Users = DBClass.GetUsers();
                Phases = DBClass.GetPhasesForUser(CurrentUserID);
                Grants = DBClass.GetGrantsForUser(CurrentUserID);
                PreSelectedPhaseId = Project.PhaseID;
                TempData["ErrorMessage"] = $"Error creating project: {ex.Message}";
                return Page();
            }
        }
    }
}
