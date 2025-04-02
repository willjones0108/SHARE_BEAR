using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using System.Collections.Generic;
using JMUcare.Pages.DBclass;
using System;

namespace JMUcare.Pages.Tasks
{
    public class CalendarModel : PageModel
    {
        public List<CalendarEvent> CalendarEvents { get; set; } = new List<CalendarEvent>();

        public void OnGet()
        {
            // Get all tasks from the database
            var tasks = DBClass.GetAllTasks();

            // Convert tasks to calendar events
            foreach (var task in tasks)
            {
                // Determine color based on status
                string color = GetStatusColor(task.Status);

                CalendarEvents.Add(new CalendarEvent
                {
                    Title = task.TaskContent,
                    Start = task.DueDate.ToString("yyyy-MM-dd"),
                    // Optional: End date if tasks have duration
                    // End = task.DueDate.AddDays(1).ToString("yyyy-MM-dd"),
                    AllDay = true,
                    Status = task.Status,
                    Color = color
                });
            }
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
    }
}
