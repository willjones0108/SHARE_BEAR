using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JMUcare.Pages.Phases
{
    public class ViewModel : PageModel
    {
        [BindProperty(SupportsGet = true)]
        public int Id { get; set; }

        public PhaseModel Phase { get; set; }
        //public List<TaskModel> Tasks { get; set; } // Placeholder for tasks

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

            //// Placeholder for getting tasks associated with the phase
           // Tasks = new List<TaskModel>(); // Replace with actual method to get tasks

            return Page();
        }
    }
}