using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Bluewire.Stash.Remote;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace Bluewire.Stash.Service.Controllers
{
    [ApiController]
    public class ApiController : ControllerBase
    {
        private readonly RemoteStashRepositoryService service;
        private readonly AzureBlobsStashService azureService;
        private readonly ILogger<ApiController> logger;

        public ApiController(RemoteStashRepositoryService service, AzureBlobsStashService azureService, ILogger<ApiController> logger)
        {
            this.service = service;
            this.azureService = azureService;
            this.logger = logger;
        }

        private static VersionMarker ParseEntry(string entry)
        {
            if (VersionMarkerStringConverter.ForIdentifierRoundtrip().TryParse(entry, out var marker)) return marker;
            throw new ArgumentException($"Not a valid entry name: {entry}");
        }

        [HttpPost]
        [Authorize(Roles = "Stash.Add, Stash.Admin")]
        [Route("push/{name}/{txId}/{*relativePath}")]
        [Produces("application/json")]
        public async Task<string> Push(string name, Guid txId, string relativePath)
        {
            var stash = service.GetNamed(name);
            var blobName = $"{txId:D}.{Guid.NewGuid():D}";

            await stash.Push(txId, relativePath, new BlobNameAdapter().ToStream(blobName));

            return azureService.CreateUploadUri(blobName).AbsoluteUri;
        }

        [HttpPut]
        [Authorize(Roles = "Stash.Add, Stash.Admin")]
        [Route("commit/{name}/{entry}/{txId}")]
        public IActionResult Commit(string name, string entry, Guid txId)
        {
            var stash = service.GetNamed(name);
            // Revoke SAP.

            var marker = ParseEntry(entry);
            stash.Commit(marker, txId);
            Task.Run(BackgroundCleanUp);
            return Ok();
        }

        private async void BackgroundCleanUp()
        {
            await foreach (var item in service.CleanUpTemporaryObjects(new BlobCleaner(azureService, logger), default))
            {
                logger.LogInformation($"Cleaned up {item}");
            }
        }

        [HttpGet]
        [Authorize(Roles = "Stash.Get, Stash.Add, Stash.Admin")]
        [Route("pull/{name}/{entry}/{*relativePath}")]
        [Produces("application/json")]
        public async Task<string> Pull(string name, string entry, string relativePath)
        {
            var stash = service.GetNamed(name);
            var marker = ParseEntry(entry);
            await using (var stream = await stash.Pull(marker, relativePath))
            {
                var blobName = new BlobNameAdapter().FromStream(stream);
                return azureService.GetDownloadUri(blobName).AbsoluteUri;
            }
        }

        [HttpGet]
        [Authorize(Roles = "Stash.Get, Stash.Add, Stash.Admin")]
        [Route("pull/{name}")]
        [Produces("application/json")]
        public async Task<string[]> List(string name)
        {
            var stash = service.GetNamed(name);
            var list = new List<string>();
            await foreach (var entry in stash.List())
            {
                list.Add(VersionMarkerStringConverter.ForIdentifierRoundtrip().ToString(entry));
            }
            return list.ToArray();
        }

        [HttpGet]
        [Authorize(Roles = "Stash.Get, Stash.Add, Stash.Admin")]
        [Route("pull/{name}/{entry}")]
        [Produces("application/json")]
        public async Task<string[]> ListFiles(string name, string entry)
        {
            var stash = service.GetNamed(name);
            var marker = ParseEntry(entry);
            var list = new List<string>();
            await foreach (var relativePath in stash.ListFiles(marker))
            {
                list.Add(relativePath);
            }
            return list.ToArray();
        }

        [HttpHead]
        [Authorize(Roles = "Stash.Get, Stash.Add, Stash.Admin")]
        [Route("pull/{name}/{entry}")]
        public async Task<IActionResult> Exists(string name, string entry)
        {
            var stash = service.GetNamed(name);
            var marker = ParseEntry(entry);
            if (await stash.Exists(marker))
            {
                return Ok();
            }
            return NotFound();
        }

        [HttpDelete]
        [Authorize(Roles = "Stash.Admin")]
        [Route("delete/{name}/{entry}")]
        public async Task<IActionResult> Delete(string name, string entry)
        {
            var stash = service.GetNamed(name);
            var marker = ParseEntry(entry);
            if (await stash.Exists(marker))
            {
                await stash.Delete(marker);
                return Ok();
            }
            return NotFound();
        }

        [HttpPost]
        [Authorize]
        [Route("ping")]
        public string Ping()
        {
            var roles = User.Identities
                .SelectMany(i => i.Claims
                    .Where(c => StringComparer.OrdinalIgnoreCase.Equals(c.Type, i.RoleClaimType)).Select(c => c.Value))
                .Distinct()
                .ToArray();

            return $@"
User: {User.Identity.Name}
Roles: {string.Join(", ", roles)}
".TrimStart();
        }

        class BlobNameAdapter
        {
            public Stream ToStream(string blobName) => new MemoryStream(Encoding.UTF8.GetBytes(blobName));
            public string FromStream(Stream stream)
            {
                using (var reader = new StreamReader(stream, Encoding.UTF8))
                {
                    return reader.ReadToEnd();
                }
            }
        }

        class BlobCleaner : IBlobCleaner
        {
            private readonly AzureBlobsStashService azureService;
            private readonly ILogger<ApiController> logger;

            public BlobCleaner(AzureBlobsStashService azureService, ILogger<ApiController> logger)
            {
                this.azureService = azureService;
                this.logger = logger;
            }

            public async Task<bool> TryCleanUp(LocalFileSystem fileSystem, string absolutePath)
            {
                try
                {
                    await using (var stream = fileSystem.OpenForRead(absolutePath))
                    {
                        var blobName = new BlobNameAdapter().FromStream(stream);
                        await azureService.DeleteBlob(blobName);
                    }
                    return true;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, $"Unable to delete blob referenced by {absolutePath}");
                    return false;
                }
            }
        }
    }
}
