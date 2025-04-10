using System;
using System.Collections.Generic;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using UserRoleModel = JMUcare.Pages.Dataclasses.UserRoleModel;

namespace JMUcare.Pages.Users
{
    public class EditUserModel : PageModel
    {
        [BindProperty]
        public DbUserModel User { get; set; }

        public List<UserRoleModel> Roles { get; set; } = new List<UserRoleModel>();

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public IActionResult OnGet(int id)
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

            // Load the user to edit
            User = DBClass.GetUserById(id);
            if (User == null)
            {
                TempData["ErrorMessage"] = "User not found.";
                return RedirectToPage("/Users/DBUsers");
            }

            // Load user roles for dropdown
            Roles = DBClass.GetUserRoles();

            return Page();
        }

        public IActionResult OnPost()
        {
            // Check if user is logged in and is admin
            if (CurrentUserID == 0 || !DBClass.IsUserAdmin(CurrentUserID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            if (!ModelState.IsValid)
            {
                Roles = DBClass.GetUserRoles();
                return Page();
            }

            // Check if trying to edit yourself
            if (User.UserID == CurrentUserID && User.IsArchived)
            {
                TempData["ErrorMessage"] = "You cannot archive your own account.";
                Roles = DBClass.GetUserRoles();
                return Page();
            }

            // Update the user
            try
            {
                DBClass.UpdateUser(User);
                TempData["Message"] = "User updated successfully.";
                return RedirectToPage("/Users/DBUsers");
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error updating user: {ex.Message}";
                Roles = DBClass.GetUserRoles();
                return Page();
            }
        }

        public IActionResult OnPostChangePassword(int userId, string newPassword, string confirmPassword)
        {
            // Check if user is logged in and is admin
            if (CurrentUserID == 0 || !DBClass.IsUserAdmin(CurrentUserID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            if (string.IsNullOrEmpty(newPassword) || newPassword != confirmPassword)
            {
                TempData["ErrorMessage"] = "Passwords do not match or are empty.";
                return RedirectToPage(new { id = userId });
            }

            try
            {
                DBClass.ChangeUserPassword(userId, newPassword);
                TempData["Message"] = "Password changed successfully.";
                return RedirectToPage(new { id = userId });
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error changing password: {ex.Message}";
                return RedirectToPage(new { id = userId });
            }
        }

        public IActionResult OnPostDeleteUser(int userId)
        {
            // Check if user is logged in and is admin
            if (CurrentUserID == 0 || !DBClass.IsUserAdmin(CurrentUserID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Prevent deleting yourself
            if (userId == CurrentUserID)
            {
                TempData["ErrorMessage"] = "You cannot delete your own account.";
                return RedirectToPage(new { id = userId });
            }

            try
            {
                bool success = DBClass.DeleteUser(userId);
                if (success)
                {
                    TempData["Message"] = "User deleted successfully.";
                    return RedirectToPage("/Users/DBUsers");
                }
                else
                {
                    TempData["ErrorMessage"] = "Failed to delete user. Try archiving instead.";
                    return RedirectToPage(new { id = userId });
                }
            }
            catch (Exception ex)
            {
                TempData["ErrorMessage"] = $"Error deleting user: {ex.Message}";
                return RedirectToPage(new { id = userId });
            }
        }
    }


}
