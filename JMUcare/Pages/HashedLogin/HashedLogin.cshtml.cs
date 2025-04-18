using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using System.ComponentModel.DataAnnotations;

namespace JMUcare.Pages.HashedLogin
{
    public class HashedLoginModel : PageModel
    {
        [BindProperty]
        [Required(ErrorMessage = "Username is required")]
        public string Username { get; set; }

        [BindProperty]
        [Required(ErrorMessage = "Password is required")]
        public string Password { get; set; }

        public void OnGet()
        {
        }
        public IActionResult OnPost()
        {
            if (!ModelState.IsValid)
            {
                return Page();
            }

            if (DBClass.HashedParameterLogin(Username, Password))
            {
                HttpContext.Session.SetString("username", Username);

                // Also retrieve and store the user ID
                int userId = DBClass.GetUserIdByUsername(Username);
                HttpContext.Session.SetInt32("CurrentUserID", userId);

                ViewData["LoginMessage"] = "Login Successful!";


                // Redirect to the dashboard page (Index page)
                return RedirectToPage("/Index");
            }
            else
            {
                ViewData["LoginMessage"] = "Username and/or Password Incorrect";

                // Remove the invalid reference to JMUcareDBConnection.Close()
                return Page();
            }

        }

    }
}
