using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using System.Collections.Generic;

namespace JMUcare.Pages.Tasks
{
    public class CreateTaskModel : PageModel
    {
        [BindProperty]
        public ProjectTaskModel Task { get; set; }

        public List<ProjectModel> Projects { get; set; }

        public void OnGet()
        {
            Projects = DBClass.GetProjects();
        }

        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                Projects = DBClass.GetProjects();
                return Page();
            }

            DBClass.InsertProjectTask(Task);
            return RedirectToPage("/Tasks/Index", new { projectId = Task.ProjectID });
        }
    }
}