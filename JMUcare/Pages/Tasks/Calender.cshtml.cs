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
                // Get authorized projects for the current user
                var authorizedProjects = DBClass.GetProjectsByUserId(currentUserId.Value);

                // Convert authorized projects to calendar events
                foreach (var project in authorizedProjects)
                {
                    // Determine color based on status
                    string color = GetStatusColor(project.TrackingStatus);

                    CalendarEvents.Add(new CalendarEvent
                    {
                        Title = project.Title,
                        Start = project.DueDate.ToString("yyyy-MM-dd"),
                        AllDay = true,
                        Status = project.TrackingStatus,
                        Color = color,
                        ProjectID = project.ProjectID
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
        public int ProjectID { get; set; }
    }
}
