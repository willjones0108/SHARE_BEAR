using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.Collections.Generic;
using System.Linq;

namespace JMUcare.Pages.Messages
{
    public class IndexModel : PageModel
    {
        public List<MessageModel> ReceivedMessages { get; set; }
        public List<MessageModel> SentMessages { get; set; }
        public List<DbUserModel> UserList { get; set; }

        [BindProperty]
        public int SelectedRecipientID { get; set; }

        [BindProperty]
        public string MessageText { get; set; }

        [BindProperty]
        public int MessageID { get; set; } // For marking messages as read

        public int LoggedInUserID { get; set; } // Store UserID from session

        public void OnGet()
        {
            string username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                ViewData["LoginMessage"] = "You must be logged in to view messages.";
                return;
            }

            // Retrieve the logged-in user's ID from the database
            LoggedInUserID = DBClass.GetUserIdByUsername(username);

            if (LoggedInUserID == 0)
            {
                ViewData["LoginMessage"] = "User not found.";
                return;
            }

            ReceivedMessages = DBClass.GetReceivedMessages(LoggedInUserID);
            SentMessages = DBClass.GetSentMessages(LoggedInUserID);
            UserList = DBClass.GetUsers(); // Load all users for dropdown selection
        }

        public IActionResult OnPost()
        {
            string username = HttpContext.Session.GetString("username");
            if (string.IsNullOrEmpty(username))
            {
                ViewData["LoginMessage"] = "You must be logged in to send messages.";
                return Page();
            }

            int senderID = DBClass.GetUserIdByUsername(username);
            if (senderID == 0)
            {
                ViewData["LoginMessage"] = "User not found.";
                return Page();
            }

            DBClass.SendMessage(senderID, SelectedRecipientID, MessageText);
            return RedirectToPage("Index"); // Refresh messages after sending
        }

        public IActionResult OnPostMarkAsRead(int messageId)
        {
            DBClass.MarkMessageAsRead(messageId);
            return RedirectToPage("Index");
        }

    }
}

