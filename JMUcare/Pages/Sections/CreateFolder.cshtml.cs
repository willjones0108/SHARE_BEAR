using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using System;
using System.Collections.Generic;

namespace JMUcare.Pages.Projects
{
    public class CreateFolderModel : PageModel
    {
        [BindProperty]
        public ProjectModel Folder { get; set; }

        public List<SelectListItem> PhaseList { get; set; }
        public List<GrantModel> Grants { get; set; }

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        [BindProperty(SupportsGet = true)]
        public int GrantId { get; set; }

        [BindProperty(SupportsGet = true)]
        public int PreSelectedPhaseId { get; set; }

        public void OnGet()
        {
            Folder = new ProjectModel
            {
                ProjectType = "folder",
                TrackingStatus = "N/A",
                StartDate = DateTime.Today,
                DueDate = DateTime.Today.AddMonths(1),
                IsArchived = false,
                GrantID = GrantId > 0 ? GrantId : null
            };

            PhaseList = new SelectList(DBClass.GetPhasesForUser(CurrentUserID), "PhaseID", "PhaseName").ToList();
            Grants = DBClass.GetGrantsForUser(CurrentUserID);

            // Set the pre-selected phase if provided
            if (PreSelectedPhaseId > 0)
            {
                Folder.PhaseID = PreSelectedPhaseId;
            }
        }



        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                PhaseList = new SelectList(DBClass.GetPhasesForUser(CurrentUserID), "PhaseID", "PhaseName").ToList();
                Grants = DBClass.GetGrantsForUser(CurrentUserID);
                return Page();
            }

            // Set the current user as the creator
            Folder.CreatedBy = CurrentUserID;

            // Set StartDate and DueDate to an arbitrary date in the past
            Folder.StartDate = new DateTime(1900, 1, 1);
            Folder.DueDate = new DateTime(1900, 1, 1);

            try
            {
                int id = DBClass.InsertProject(Folder);

                // Insert Phase_Project record if a phase is selected
                if (Folder.PhaseID > 0)
                {
                    DBClass.InsertPhaseProject(Folder.PhaseID, id);
                }

                TempData["SuccessMessage"] = "Folder created successfully.";
                return RedirectToPage("/Projects/View", new { id });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error: {ex.Message}";
                PhaseList = new SelectList(DBClass.GetPhasesForUser(CurrentUserID), "PhaseID", "PhaseName").ToList();
                Grants = DBClass.GetGrantsForUser(CurrentUserID);
                return Page();
            }
        }


    }
}
