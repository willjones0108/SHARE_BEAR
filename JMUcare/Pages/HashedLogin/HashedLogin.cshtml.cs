using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JMUcare.Pages.HashedLogin
{
        public class HashedLoginModel : PageModel
        {
            [BindProperty]
            public string Username { get; set; }
            [BindProperty]
            public string Password { get; set; }

            public void OnGet()
            {
            }

            public IActionResult OnPost()
            {
                if (DBClass.HashedParameterLogin(Username, Password))
                {
                    HttpContext.Session.SetString("username", Username);
                    ViewData["LoginMessage"] = "Login Successful!";
                    DBClass.JMUcareDBConnection.Close();

                    return RedirectToPage("/Index");
                }
                else
                {
                    ViewData["LoginMessage"] = "Username and/or Password Incorrect";
                    DBClass.JMUcareDBConnection.Close();
                    return Page();
                }
            }
        }
    }


