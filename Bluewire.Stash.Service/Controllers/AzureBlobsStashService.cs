using System;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;

namespace Bluewire.Stash.Service.Controllers
{
    public class AzureBlobsStashService
    {
        private readonly string connectionString;
        private readonly string containerName;

        public Func<DateTimeOffset> Now = () => DateTimeOffset.Now;

        public AzureBlobsStashService(string connectionString, string containerName)
        {
            this.connectionString = connectionString;
            this.containerName = containerName;
        }

        private BlobContainerClient GetContainer()
        {
            return new BlobContainerClient(connectionString, containerName);
        }

        private TimeSpan DefaultTransactionTimeout { get; } = TimeSpan.FromHours(4);
        private TimeSpan DefaultReadTimeout { get; } = TimeSpan.FromHours(12);

        public Uri CreateUploadUri(string blobName)
        {
            var client = GetContainer().GetBlobClient(blobName);
            if (!client.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Not authorised to create service SAS.");
            }
            var builder = CreateExpiringSasBuilder(blobName, DefaultTransactionTimeout);
            builder.SetPermissions(BlobSasPermissions.Read | BlobSasPermissions.Write);
            return client.GenerateSasUri(builder);
        }

        public Uri GetDownloadUri(string blobName)
        {
            var client = GetContainer().GetBlobClient(blobName);
            if (!client.CanGenerateSasUri)
            {
                throw new InvalidOperationException("Not authorised to create service SAS.");
            }
            var builder = CreateExpiringSasBuilder(blobName, DefaultReadTimeout);
            builder.SetPermissions(BlobSasPermissions.Read);
            return client.GenerateSasUri(builder);
        }

        private BlobSasBuilder CreateExpiringSasBuilder(string blobName, TimeSpan expiresAfter)
        {
            return new BlobSasBuilder()
            {
                BlobContainerName = containerName,
                BlobName = blobName,
                Resource = "b",
                ExpiresOn = Now() + expiresAfter,
            };
        }

        public async Task DeleteBlob(string blobName)
        {
            var client = GetContainer().GetBlobClient(blobName);
            await client.DeleteIfExistsAsync(DeleteSnapshotsOption.IncludeSnapshots);
        }
    }
}
