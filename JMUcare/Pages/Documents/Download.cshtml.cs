using System;
using System.IO;
using System.Threading.Tasks;
using JMUcare.Pages.DBclass;
using JMUcare.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace JMUcare.Pages.Documents
{
    public class DownloadModel : PageModel
    {
        private readonly BlobStorageService _blobStorageService;

        public DownloadModel(BlobStorageService blobStorageService)
        {
            _blobStorageService = blobStorageService;
        }

        public int CurrentUserID => HttpContext.Session.GetInt32("CurrentUserID") ?? 0;

        public async Task<IActionResult> OnGetAsync(int id)
        {
            if (CurrentUserID == 0)
            {
                return RedirectToPage("/HashedLogin/HashedLogin");
            }

            // Get document details from database
            var document = DBClass.GetDocumentById(id);
            if (document == null)
            {
                return NotFound();
            }

            // Check if user has access to this document
            if (!DBClass.CanAccessDocument(CurrentUserID, id))
            {
                return RedirectToPage("/Shared/AccessDenied");
            }

            try
            {
                // Download from blob storage
                Stream fileStream = await _blobStorageService.DownloadDocumentAsync(document.BlobName);

                // Set content type and suggested filename for download
                return File(fileStream, document.ContentType, document.FileName);
            }
            catch (Exception ex)
            {
                // Log the exception
                Console.WriteLine($"Error downloading document: {ex.Message}");
                return RedirectToPage("/Error");
            }
        }
    }
}
