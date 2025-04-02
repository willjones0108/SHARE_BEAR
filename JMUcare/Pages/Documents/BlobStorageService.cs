//using Azure;
//using Azure.Storage.Blobs;
//using Azure.Storage.Blobs.Models;
//using Microsoft.AspNetCore.Http;
//using Microsoft.Extensions.Configuration;
//using System;
//using System.IO;
//using System.Threading.Tasks;

//namespace JMUcare.Services
//{
//    public class BlobStorageService
//    {
//        private readonly BlobServiceClient _blobServiceClient;
//        private readonly string _containerName;

//        public BlobStorageService(IConfiguration configuration)
//        {
//            var connectionString = configuration["AzureStorageConfig:ConnectionString"];
//            _containerName = configuration["AzureStorageConfig:ContainerName"];

//            _blobServiceClient = new BlobServiceClient(connectionString);

//            // Ensure container exists
//            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
//            containerClient.CreateIfNotExists(PublicAccessType.None);
//        }

//        public async Task<string> UploadDocumentAsync(IFormFile file, string folderPath)
//        {
//            if (file == null) throw new ArgumentNullException(nameof(file));

//            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);

//            // Create a unique blob name
//            string blobName = $"{folderPath}/{Guid.NewGuid()}-{file.FileName}";
//            BlobClient blobClient = containerClient.GetBlobClient(blobName);

//            // Upload the file
//            using (var stream = file.OpenReadStream())
//            {
//                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType });
//            }

//            return blobName;
//        }

//        public async Task<Stream> DownloadDocumentAsync(string blobName)
//        {
//            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
//            BlobClient blobClient = containerClient.GetBlobClient(blobName);

//            var memoryStream = new MemoryStream();
//            await blobClient.DownloadToAsync(memoryStream);
//            memoryStream.Position = 0;
//            return memoryStream;
//        }

//        public async Task DeleteDocumentAsync(string blobName)
//        {
//            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
//            BlobClient blobClient = containerClient.GetBlobClient(blobName);
//            await blobClient.DeleteIfExistsAsync();
//        }

//        public async Task<string> GenerateSasTokenAsync(string blobName, TimeSpan validFor)
//        {
//            var containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
//            BlobClient blobClient = containerClient.GetBlobClient(blobName);

//            // Generate SAS token that's valid for the specified duration
//            BlobSasBuilder sasBuilder = new BlobSasBuilder
//            {
//                BlobContainerName = _containerName,
//                BlobName = blobName,
//                Resource = "b", // "b" for blob
//                StartsOn = DateTimeOffset.UtcNow,
//                ExpiresOn = DateTimeOffset.UtcNow.Add(validFor)
//            };

//            sasBuilder.SetPermissions(BlobSasPermissions.Read);

//            // Generate the SAS token
//            var sasToken = blobClient.GenerateSasUri(sasBuilder);
//            return sasToken.ToString();
//        }
//    }
//}
