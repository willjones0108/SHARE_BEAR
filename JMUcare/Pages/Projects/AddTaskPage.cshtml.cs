using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Linq;

namespace JMUcare.Pages.Projects
{
    public class AddTaskPageModel : PageModel
    {
        [BindProperty]
        public ProjectTaskModel Task { get; set; } = new ProjectTaskModel();

        public List<ProjectModel> Projects { get; set; } = new List<ProjectModel>();

        public bool IsProjectPreselected { get; set; } = false;

        public ProjectModel SelectedProject { get; set; }

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public IActionResult OnGet(int? projectId = null)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Set default due date to today
            Task.DueDate = DateTime.Today;

            // Load projects
            LoadProjects();

            if (projectId.HasValue)
            {
                Task.ProjectID = projectId.Value;
                IsProjectPreselected = true;

                // Get the selected project details for display
                SelectedProject = Projects.FirstOrDefault(p => p.ProjectID == projectId.Value);
                if (SelectedProject == null)
                {
                    // If project not found, redirect to projects list
                    TempData["ErrorMessage"] = "Selected project not found.";
                    return RedirectToPage("/Projects/Index");
                }
            }

            return Page();
        }

        public IActionResult OnPost()
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            if (!ModelState.IsValid)
            {
                LoadProjects();
                return Page();
            }

            try
            {
                // Add task
                int taskId = AddTask();

                TempData["SuccessMessage"] = "Task added successfully.";
                return RedirectToPage("/Projects/EditTask", new { id = taskId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"An error occurred: {ex.Message}";
                LoadProjects();
                return Page();
            }
        }

        private void LoadProjects()
        {
            Projects = DBClass.GetProjects().ToList();
        }

        private int AddTask()
        {
            using var connection = new System.Data.SqlClient.SqlConnection(DBClass.JMUcareDBConnString);
            var query = @"
                INSERT INTO Project_Task (ProjectID, TaskContent, DueDate, Status)
                OUTPUT INSERTED.TaskID
                VALUES (@ProjectID, @TaskContent, @DueDate, @Status)";

            using var cmd = new System.Data.SqlClient.SqlCommand(query, connection);
            cmd.Parameters.AddWithValue("@ProjectID", Task.ProjectID);
            cmd.Parameters.AddWithValue("@TaskContent", Task.TaskContent);
            cmd.Parameters.AddWithValue("@DueDate", Task.DueDate);
            cmd.Parameters.AddWithValue("@Status", Task.Status);

            connection.Open();
            int taskId = (int)cmd.ExecuteScalar();
            return taskId;
        }
    }
}
