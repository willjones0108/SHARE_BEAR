using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace JMUcare.Pages.Phases
{
    public class PhaseViewModel : PageModel  // Changed from ViewModel to PhaseViewModel to avoid confusion
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public PhaseModel Phase { get; set; }
        public List<ProjectModel> Projects { get; set; }
        public Dictionary<int, List<ProjectTaskModel>> ProjectTasks { get; set; }
        public bool CanAddProject { get; set; }

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

            // Get the phase details
            Phase = DBClass.GetPhaseById(Id);

            if (Phase == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this phase
            string accessLevel = DBClass.GetUserAccessLevelForPhase(CurrentUserID, Id);

            if (accessLevel == "None")
            {
                return RedirectToPage("/AccessDenied");
            }

            // Check if user can add a project (admin, grant editor, or phase editor)
            CanAddProject = DBClass.IsUserAdmin(CurrentUserID) ||
                            DBClass.IsGrantEditor(CurrentUserID) ||
                            accessLevel == "Edit";

            // Get projects associated with this phase
            Projects = DBClass.GetProjectsByPhaseId(Id);

            // Get tasks associated with each project
            ProjectTasks = new Dictionary<int, List<ProjectTaskModel>>();
            foreach (var project in Projects)
            {
                ProjectTasks[project.ProjectID] = DBClass.GetTasksByProjectId(project.ProjectID);
            }

            return Page();
        }
    }
}
