using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Bluewire.Stash.Remote;
using Microsoft.Identity.Client;

namespace Bluewire.Stash.Tool
{
    public class HttpRemoteStashRepository : IRemoteStashRepository
    {
        private readonly Uri rootUri;
        private readonly string name;
        private readonly AuthenticationResult authResult;
        private static readonly HttpClient httpClient = new HttpClient();

        public HttpRemoteStashRepository(Uri rootUri, string name, AuthenticationResult authResult)
        {
            this.rootUri = rootUri;
            this.name = name;
            this.authResult = authResult;
        }

        private string ConvertRelativePathToUriSegments(string relativePath)
        {
            if (Path.IsPathRooted(relativePath)) throw new ArgumentException($"Not a relative path: {relativePath}", nameof(relativePath));
            return string.Join("/", relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar).Select(Uri.EscapeDataString));
        }

        private string ConvertVersionMarkerToUriSegment(VersionMarker entry)
        {
            return Uri.EscapeDataString(VersionMarkerStringConverter.ForIdentifierRoundtrip().ToString(entry));
        }

        private static HttpRequestMessage CreateMessage(HttpMethod method, Uri uri, AuthenticationResult authResult)
        {
            var message = new HttpRequestMessage(method, uri);
            message.Headers.Authorization = new AuthenticationHeaderValue("Bearer", authResult.AccessToken);
            return message;
        }

        public async Task Push(Guid txId, string relativePath, Stream stream, CancellationToken token = default)
        {
            var uri = new Uri(rootUri, $"push/{Uri.EscapeDataString(name)}/{Uri.EscapeDataString(txId.ToString("D"))}/{ConvertRelativePathToUriSegments(relativePath)}");
            var message = CreateMessage(HttpMethod.Post, uri, authResult);

            using (var response = await httpClient.SendAsync(message, token))
            {
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();

                var pushTargetUri = JsonSerializer.Deserialize<Uri>(result);
                var client = new BlobClient(pushTargetUri);
                await client.UploadAsync(stream, token);
            }
        }

        public async Task Commit(VersionMarker entry, Guid txId, CancellationToken token = default)
        {
            var uri = new Uri(rootUri, $"commit/{Uri.EscapeDataString(name)}/{ConvertVersionMarkerToUriSegment(entry)}/{Uri.EscapeDataString(txId.ToString("D"))}");
            var message = CreateMessage(HttpMethod.Put, uri, authResult);

            using (var response = await httpClient.SendAsync(message, token))
            {
                response.EnsureSuccessStatusCode();
            }
        }

        public async Task<Stream> Pull(VersionMarker entry, string relativePath, CancellationToken token = default)
        {
            var uri = new Uri(rootUri, $"pull/{Uri.EscapeDataString(name)}/{ConvertVersionMarkerToUriSegment(entry)}/{ConvertRelativePathToUriSegments(relativePath)}");
            var message = CreateMessage(HttpMethod.Get, uri, authResult);

            var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token);
            response.EnsureSuccessStatusCode();
            var result = await response.Content.ReadAsStringAsync();

            var pullSourceUri = JsonSerializer.Deserialize<Uri>(result);
            var client = new BlobClient(pullSourceUri);
            var blob = await client.DownloadAsync(token);
            return blob.Value.Content;
        }

        public async IAsyncEnumerable<VersionMarker> List([EnumeratorCancellation] CancellationToken token = default)
        {
            var uri = new Uri(rootUri, $"pull/{Uri.EscapeDataString(name)}");
            var message = CreateMessage(HttpMethod.Get, uri, authResult);

            using (var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token))
            {
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                foreach (var entry in JsonSerializer.Deserialize<string[]>(result))
                {
                    if (VersionMarkerStringConverter.ForIdentifierRoundtrip().TryParse(entry, out var marker))
                    {
                        yield return marker;
                    }
                }
            }
        }

        public async IAsyncEnumerable<string> ListFiles(VersionMarker entry, [EnumeratorCancellation] CancellationToken token = default)
        {
            var uri = new Uri(rootUri, $"pull/{Uri.EscapeDataString(name)}/{ConvertVersionMarkerToUriSegment(entry)}");
            var message = CreateMessage(HttpMethod.Get, uri, authResult);

            using (var response = await httpClient.SendAsync(message, HttpCompletionOption.ResponseHeadersRead, token))
            {
                response.EnsureSuccessStatusCode();
                var result = await response.Content.ReadAsStringAsync();
                foreach (var relativePath in JsonSerializer.Deserialize<string[]>(result))
                {
                    yield return relativePath;
                }
            }
        }

        public async Task<bool> Exists(VersionMarker entry, CancellationToken token = default)
        {
            var uri = new Uri(rootUri, $"pull/{Uri.EscapeDataString(name)}/{ConvertVersionMarkerToUriSegment(entry)}");
            var message = CreateMessage(HttpMethod.Head, uri, authResult);

            var response = await httpClient.SendAsync(message, token);
            if (response.StatusCode == HttpStatusCode.NotFound) return false;
            response.EnsureSuccessStatusCode();
            return true;
        }

        public static async Task<string> Ping(Uri rootUri, AuthenticationResult authResult, CancellationToken token = default)
        {
            var uri = new Uri(rootUri, "ping");
            var message = CreateMessage(HttpMethod.Post, uri, authResult);

            using (var response = await httpClient.SendAsync(message, token))
            {
                response.EnsureSuccessStatusCode();
                return await response.Content.ReadAsStringAsync();
            }
        }
    }
}
