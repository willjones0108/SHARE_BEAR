using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Linq;

namespace JMUcare.Pages
{
    public class IndexModel : PageModel
    {
        public int CurrentUserID
        {
            get
            {
                return HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
            }
        }

        public string CurrentUsername
        {
            get
            {
                return HttpContext.Session.GetString("username") ?? string.Empty;
            }
        }

        public bool IsAdmin { get; set; }
        public List<ProjectTaskModel> EditableTasks { get; set; } = new List<ProjectTaskModel>();

        public List<GrantModel> Grants { get; set; } = new List<GrantModel>();
        public List<ProjectModel> RecentProjects { get; set; } = new List<ProjectModel>();
        public List<PhaseModel> ActivePhases { get; set; } = new List<PhaseModel>();
        public List<ProjectTaskModel> UpcomingTasks { get; set; } = new List<ProjectTaskModel>();

        // Dashboard statistics
        public int TotalGrants { get; set; }
        public int InProgressPhases { get; set; }
        public int PendingTasks { get; set; }

        public IActionResult OnGet()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check if user is admin
            IsAdmin = DBClass.IsUserAdmin(CurrentUserID);

            // Get grants that the user has access to
            Grants = DBClass.GetGrantsForUser(CurrentUserID) ?? new List<GrantModel>();
            TotalGrants = Grants.Count;

            // Get active phases across all grants
            ActivePhases = new List<PhaseModel>();
            foreach (var grant in Grants)
            {
                var phases = DBClass.GetPhasesByGrantId(grant.GrantID);
                if (phases != null)
                {
                    ActivePhases.AddRange(phases.Where(p => p.Status == "In Progress"));
                }
            }
            InProgressPhases = ActivePhases.Count;

            // Select most relevant phases if there are too many
            if (ActivePhases.Count > 5)
            {
                ActivePhases = ActivePhases.Take(5).ToList();
            }

            // Get recent projects
            var allProjects = new List<ProjectModel>();
            foreach (var phase in ActivePhases)
            {
                var projects = DBClass.GetProjectsByPhaseId(phase.PhaseID);
                if (projects != null)
                {
                    allProjects.AddRange(projects);
                }
            }
            RecentProjects = allProjects.OrderByDescending(p => p.ProjectID).Take(5).ToList();

            // Get upcoming tasks
            var allTasks = new List<ProjectTaskModel>();
            foreach (var project in allProjects)
            {
                var tasks = DBClass.GetTasksByProjectId(project.ProjectID, CurrentUserID);
                if (tasks != null)
                {
                    allTasks.AddRange(tasks);
                }
            }

            // Get pending/in-progress tasks with the earliest due dates
            EditableTasks = allTasks
                 .Where(t => t.AssignedUsers.Any(u => u.UserID == CurrentUserID) && t.Status == "Edit")
                 .OrderBy(t => t.DueDate)
                 .ToList();

            return Page();
        }

        public string GetProjectPhase(int projectId)
        {
            foreach (var phase in ActivePhases)
            {
                var projects = DBClass.GetProjectsByPhaseId(phase.PhaseID);
                if (projects != null && projects.Any(p => p.ProjectID == projectId))
                {
                    return phase.PhaseName;
                }
            }
            return "N/A";
        }

        public string GetGrantName(int? grantId)
        {
            if (!grantId.HasValue) return "N/A";

            var grant = Grants.FirstOrDefault(g => g.GrantID == grantId.Value);
            return grant?.GrantTitle ?? "N/A";
        }
    }
}
