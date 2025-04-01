using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.AspNetCore.Mvc.Rendering;
using JMUcare.Pages.Dataclasses;
using JMUcare.Pages.DBclass;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace JMUcare.Pages.Grants
{
    public class CreateGrantModel : PageModel
    {
        [BindProperty]
        public GrantModel Grant { get; set; }

        public List<DbUserModel> Users { get; set; }

        public List<SelectListItem> StatusOptions { get; private set; }

        public int CurrentUserID
        {
            get
            {
                // Retrieve the user ID from session state
                return HttpContext.Session.GetInt32("CurrentUserID") ?? 0;
            }
        }

        public CreateGrantModel()
        {
            // Initialize status options with common grant milestones
            StatusOptions = new List<SelectListItem>
            {
                new SelectListItem { Value = "Concept Development", Text = "Concept Development" },
                new SelectListItem { Value = "Proposal Writing", Text = "Proposal Writing" },
                new SelectListItem { Value = "Internal Review", Text = "Internal Review" },
                new SelectListItem { Value = "Submitted", Text = "Submitted" },
                new SelectListItem { Value = "Under Review", Text = "Under Review" },
                new SelectListItem { Value = "Revisions Requested", Text = "Revisions Requested" },
                new SelectListItem { Value = "Approved", Text = "Approved" },
                new SelectListItem { Value = "Funded", Text = "Funded" },
                new SelectListItem { Value = "Active", Text = "Active" },
                new SelectListItem { Value = "Reporting", Text = "Reporting" },
                new SelectListItem { Value = "Completed", Text = "Completed" },
                new SelectListItem { Value = "Rejected", Text = "Rejected" },
                new SelectListItem { Value = "On Hold", Text = "On Hold" }
            };
        }

        public IActionResult OnGet()
        {
            // Check if user is logged in
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/Account/Login");
            }


            // Check if user is an admin
            if (!DBClass.IsUserAdmin(CurrentUserID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            // Get list of users for dropdowns
            Users = DBClass.GetUsers(); // this gets non-archived users
            return Page();
        }

        public IActionResult OnPost()
        {
            // Re-check admin permissions on post as well for security
            if (CurrentUserID == 0 || !DBClass.IsUserAdmin(CurrentUserID))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            if (!ModelState.IsValid)
            {
                Users = DBClass.GetUsers(); // repopulate dropdown
                return Page();
            }

            // Set the current user as the creator if not specified
            if (Grant.CreatedBy == 0)
            {
                Grant.CreatedBy = CurrentUserID;
            }

            int grantId = DBClass.InsertGrant(Grant);

            // Add Grant Lead as editor
            DBClass.InsertGrantPermission(grantId, Grant.GrantLeadID, "Edit");

            // Optionally also give CreatedBy some permission
            DBClass.InsertGrantPermission(grantId, Grant.CreatedBy, "Edit");

            return RedirectToPage("/Grants/Index");
        }
    }
}
