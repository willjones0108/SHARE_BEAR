// JMUcare/Pages/Documents/BlobStorageService.cs
using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas; // Add this line
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;

namespace JMUcare.Services
{
    public class BlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;
        private readonly IConfiguration _configuration; // Add this line

        public BlobStorageService(IConfiguration configuration)
        {
            _configuration = configuration; // Add this line
            var connectionString = configuration["AzureStorageConfig:ConnectionString"];
            _containerName = configuration["AzureStorageConfig:ContainerName"];

            _blobServiceClient = new BlobServiceClient(connectionString);

            // Ensure container exists
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            containerClient.CreateIfNotExists(PublicAccessType.None);
        }

        public async Task<string> UploadDocumentAsync(IFormFile file, string folderPath)
        {
            if (file == null) throw new ArgumentNullException(nameof(file));

            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            // Create a unique blob name
            string blobName = $"{folderPath}/{Guid.NewGuid()}-{file.FileName}";
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            // Upload the file
            using (var stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
            }

            return blobName;
        }

        public async Task<Stream> DownloadDocumentAsync(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            var memoryStream = new MemoryStream();
            await blobClient.DownloadToAsync(memoryStream);
            memoryStream.Position = 0;
            return memoryStream;
        }

        public async Task DeleteDocumentAsync(string blobName)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }

        public async Task<string> GenerateSasTokenAsync(string blobName, TimeSpan validFor)
        {
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(blobName);

            // Generate SAS token that's valid for the specified duration
            BlobSasBuilder sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = blobName,
                Resource = "b", // b for blob
                ExpiresOn = DateTimeOffset.UtcNow.Add(validFor)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Get storage account key to sign SAS token
            var storageSharedKeyCredential = new Azure.Storage.StorageSharedKeyCredential(
                _blobServiceClient.AccountName,
                _configuration["AzureStorageConfig:AccountKey"]); // Change this line

            string sasToken = sasBuilder.ToSasQueryParameters(storageSharedKeyCredential).ToString();
            return blobClient.Uri + "?" + sasToken;
        }

        public async Task<List<string>> ListBlobsInFolderAsync(string folderPath)
        {
            var result = new List<string>();
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            var blobs = containerClient.GetBlobsAsync(prefix: folderPath);
            await foreach (var blob in blobs)
            {
                result.Add(blob.Name);
            }

            return result;
        }

        public async Task CreateFolderAsync(string folderPath)
        {
            // In blob storage, folders don't actually exist as separate entities,
            // they're just part of the blob name. To "create" a folder, we upload
            // a zero-byte blob with the folder name as a prefix.
            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

            // Add a trailing slash if not present
            if (!folderPath.EndsWith("/"))
            {
                folderPath += "/";
            }

            // Create an empty blob with the folder name as a marker
            string markerBlobName = $"{folderPath}.folder";
            BlobClient blobClient = containerClient.GetBlobClient(markerBlobName);

            using (var emptyContent = new MemoryStream(new byte[0]))
            {
                await blobClient.UploadAsync(emptyContent, overwrite: true);
            }
        }
    }
}
