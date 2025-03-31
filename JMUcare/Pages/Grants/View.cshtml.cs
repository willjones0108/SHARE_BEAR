using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace JMUcare.Pages.Grants
{
    public class ViewModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public GrantModel Grant { get; set; }
        public List<PhaseModel> Phases { get; set; }
        public Dictionary<int, List<ProjectModel>> PhaseProjects { get; set; }
        public Dictionary<int, List<ProjectTaskModel>> ProjectTasks { get; set; }

        public int CurrentUserID
        {
            get
            {
                return HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
            }
        }

        public bool CanAddPhase { get; set; }
        public bool CanAddProject { get; set; } // Added property for adding projects

        public IActionResult OnGet()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/Account/Login");
            }

            // Get the grant details
            Grant = DBClass.GetGrantById(Id);

            if (Grant == null)
            {
                return NotFound();
            }

            // Check if user has permission to view this grant
            string accessLevel = DBClass.GetUserAccessLevelForGrant(CurrentUserID, Id);

            if (accessLevel == "None")
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Check if the user can add a phase
            CanAddPhase = accessLevel == "Edit" || DBClass.IsUserAdmin(CurrentUserID);

            // Check if the user can add a project
            // Same permission level as adding a phase - admins, grant editors with "Edit" access
            CanAddProject = CanAddPhase || DBClass.IsGrantEditor(CurrentUserID);

            // Get the phases associated with the grant
            Phases = DBClass.GetPhasesByGrantId(Id);

            // Get the projects associated with each phase
            PhaseProjects = new Dictionary<int, List<ProjectModel>>();
            ProjectTasks = new Dictionary<int, List<ProjectTaskModel>>();
            foreach (var phase in Phases)
            {
                var projects = DBClass.GetProjectsByPhaseId(phase.PhaseID);
                PhaseProjects[phase.PhaseID] = projects;

                // Get the tasks associated with each project
                foreach (var project in projects)
                {
                    ProjectTasks[project.ProjectID] = DBClass.GetTasksByProjectId(project.ProjectID, CurrentUserID);
                }
            }

            return Page();
        }
        public IActionResult OnPostDeleteTask(int taskId)
        {
            if (DBClass.DeleteTask(taskId))
            {
                TempData["SuccessMessage"] = "Task deleted successfully.";
            }
            else
            {
                TempData["ErrorMessage"] = "An error occurred while deleting the task.";
            }

            return RedirectToPage(new { id = Id });
        }
        public IActionResult OnPostArchivePhase(int phaseId)
{
    if (DBClass.ArchivePhase(phaseId))
    {
        TempData["SuccessMessage"] = "Phase and its associated projects and tasks archived successfully.";
    }
    else
    {
        TempData["ErrorMessage"] = "An error occurred while archiving the phase.";
    }

    return RedirectToPage(new { id = Id });
}


    }
}
