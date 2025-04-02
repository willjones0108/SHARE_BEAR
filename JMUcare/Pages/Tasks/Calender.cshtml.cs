using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using System.Collections.Generic;
using JMUcare.Pages.DBclass;
using System;
using Microsoft.AspNetCore.Http;

namespace JMUcare.Pages.Tasks
{
    public class CalendarModel : PageModel
    {
        public List<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

        public void OnGet()
        {
            // Get the current user ID from session
            int? currentUserId = HttpContext.Session.GetInt32("CurrentUserID");

            if (currentUserId.HasValue && currentUserId.Value > 0)
            {
                // Get authorized tasks for the current user
                var authorizedTasks = DBClass.GetAuthorizedTasksForUser(currentUserId.Value);

                // Convert authorized tasks to calendar events
                foreach (var task in authorizedTasks)
                {
                    // Determine color based on status
                    string color = GetStatusColor(task.Status);

                    CalendarEvents.Add(new CalendarEvent
                    {
                        Title = task.TaskContent,
                        Start = task.DueDate.ToString("yyyy-MM-dd"),
                        AllDay = true,
                        Status = task.Status,
                        Color = color,
                        TaskID = task.TaskID,
                        ProjectID = task.ProjectID
                    });
                }
            }
            // If user is not logged in, the calendar will be empty
        }

        private string GetStatusColor(string status)
        {
            return status.ToLower() switch
            {
                "completed" => "#28a745", // Green
                "in progress" => "#ffc107", // Yellow
                "pending" => "#17a2b8", // Blue
                _ => "#dc3545" // Red for overdue or other statuses
            };
        }
    }

    public class CalendarEvent
    {
        public string Title { get; set; }
        public string Start { get; set; }
        public string End { get; set; }
        public bool AllDay { get; set; }
        public string Status { get; set; }
        public string Color { get; set; }
        public int TaskID { get; set; }
        public int ProjectID { get; set; }
    }
}
