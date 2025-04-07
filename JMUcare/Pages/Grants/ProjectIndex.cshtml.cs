using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.DBclass;
using JMUcare.Pages.Dataclasses;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Http;
using System;

namespace JMUcare.Pages.Grants
{
    public class ProjectIndexModel : PageModel
    {
        public List<GrantModel> Grants { get; set; } = new List<GrantModel>();
        public Dictionary<int, List<PhaseModel>> GrantPhases { get; set; } = new Dictionary<int, List<PhaseModel>>();
        public Dictionary<int, List<ProjectModel>> PhaseProjects { get; set; } = new Dictionary<int, List<ProjectModel>>();

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
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Get all grants that the user has access to
            var allGrants = DBClass.GetGrantsForUser(CurrentUserID);

            // Filter to only show active grants
            Grants = allGrants?.Where(g => g.Status == "Active" && !g.IsArchived).ToList() ?? new List<GrantModel>();

            // For each active grant, get associated phases
            foreach (var grant in Grants)
            {
                var phases = DBClass.GetPhasesByGrantId(grant.GrantID);
                if (phases != null && phases.Any())
                {
                    // Store non-archived phases
                    var nonArchivedPhases = phases.Where(p => !p.IsArchived).ToList();
                    GrantPhases[grant.GrantID] = nonArchivedPhases;

                    // For each phase, get associated projects
                    foreach (var phase in nonArchivedPhases)
                    {
                        var projects = DBClass.GetProjectsByPhaseId(phase.PhaseID);
                        if (projects != null)
                        {
                            PhaseProjects[phase.PhaseID] = projects.Where(p => !p.IsArchived).ToList();
                        }
                        else
                        {
                            PhaseProjects[phase.PhaseID] = new List<ProjectModel>();
                        }
                    }
                }
                else
                {
                    GrantPhases[grant.GrantID] = new List<PhaseModel>();
                }
            }

            return Page();
        }

        public bool IsUserAdmin()
        {
            return DBClass.IsUserAdmin(CurrentUserID);
        }

        public string GetUserAccessLevel(int grantId)
        {
            return DBClass.GetUserAccessLevelForGrant(CurrentUserID, grantId);
        }

        public List<ProjectModel> GetAllProjectsForGrant(int grantId)
        {
            var allProjects = new List<ProjectModel>();

            if (GrantPhases.ContainsKey(grantId))
            {
                foreach (var phase in GrantPhases[grantId])
                {
                    if (PhaseProjects.ContainsKey(phase.PhaseID))
                    {
                        allProjects.AddRange(PhaseProjects[phase.PhaseID]);
                    }
                }
            }

            return allProjects;
        }

        public int GetPhaseCount(int grantId)
        {
            return GrantPhases.ContainsKey(grantId) ? GrantPhases[grantId].Count : 0;
        }

        public int GetProjectCount(int grantId)
        {
            return GetAllProjectsForGrant(grantId).Count;
        }

        public int GetCompletedProjectCount(int grantId)
        {
            var allProjects = GetAllProjectsForGrant(grantId);
            return allProjects.Count(p => p.TrackingStatus?.ToLower() == "completed");
        }

        public double GetCompletionPercentage(int grantId)
        {
            int total = GetProjectCount(grantId);
            if (total == 0)
                return 0;

            int completed = GetCompletedProjectCount(grantId);
            return (double)completed / total * 100;
        }

        public string GetProgressClass(string status)
        {
            return status?.ToLower() switch
            {
                "not started" => "bg-secondary",
                "planning" => "bg-info",
                "in progress" => "jmu-progress-bar",
                "advanced" => "bg-primary",
                "completed" => "bg-success",
                _ => "bg-secondary"
            };
        }

        public string GetPhasesProgressSummary(int grantId)
        {
            if (!GrantPhases.ContainsKey(grantId) || !GrantPhases[grantId].Any())
                return "No phases";

            var phases = GrantPhases[grantId];
            int completedPhases = phases.Count(p => p.Status?.ToLower() == "completed");

            return $"{completedPhases} of {phases.Count} phases complete";
        }
    }
}

