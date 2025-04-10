using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;

namespace JMUcare.Pages.Users
{
    public class DBUsersModel : PageModel
    {
        public List<DbUserModel> Users { get; set; } = new List<DbUserModel>();
        public List<AuthCredentialModel> AuthCredentials { get; set; } = new List<AuthCredentialModel>();

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public IActionResult OnGet()
        {
            // Check if user is logged in
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Check if user is an admin
            if (!DBClass.IsUserAdmin(CurrentUserID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Get all users with role information
            Users = DBClass.GetAllUsersWithRoles();

            // Get auth credentials (usernames and password hashes)
            AuthCredentials = DBClass.GetAuthCredentials();

            return Page();
        }

        public class AuthCredentialModel
        {
            public int UserID { get; set; }
            public string Username { get; set; }
            public string PasswordHash { get; set; }
        }
    }
}
