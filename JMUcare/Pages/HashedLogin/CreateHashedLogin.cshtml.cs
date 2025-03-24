using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JMUcare.Pages.Dataclasses.HashedLogin
{
    public class CreateHashedLoginModel : PageModel
    {
        [BindProperty]
        public string Username { get; set; }

        [BindProperty]
        public string Password { get; set; }

        [BindProperty]
        public string FirstName { get; set; }

        [BindProperty]
        public string LastName { get; set; }

        [BindProperty]
        public string Email { get; set; }

        [BindProperty]
        public int UserRoleID { get; set; } // User role ID

        [BindProperty]
        public DateTime UpdatedAt { get; set; } = DateTime.Now; // Default to now

        [BindProperty]
        public bool IsArchived { get; set; } = false; // Default to false

        public void OnGet()
        {
        }

        public IActionResult OnPost()
        {
            if (UserRoleID <= 0 || string.IsNullOrEmpty(Username) || string.IsNullOrEmpty(Password))
            {
                ViewData["UserCreate"] = "All required fields must be filled!";
                return Page();
            }

            DbUserModel newUser = new()
            {
                FirstName = FirstName,
                LastName = LastName,
                Email = Email,
                Password = Password,
                Username = Username,
                UserRoleID = UserRoleID,
                UpdatedAt = UpdatedAt,
                IsArchived = IsArchived
            };

            // Insert user into DBUser table
            DBClass.InsertDBUser(newUser);

            ViewData["UserCreate"] = "User Successfully Created!";
            return RedirectToPage("HashedLogin");
        }
    }
}
    

