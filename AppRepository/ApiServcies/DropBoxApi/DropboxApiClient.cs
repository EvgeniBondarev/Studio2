using Dropbox.Api.Files;
using Dropbox.Api;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Dropbox.Api.Sharing;
using System.Security.Policy;

namespace Servcies.ApiServcies.DropBoxApi
{
    public class DropboxApiClient
    {
        private string _accessToken;
        private readonly string _refreshToken;
        private readonly string _appKey;
        private readonly string _appSecret;

        public DropboxApiClient(string refreshToken, string appKey, string appSecret)
        {
            _refreshToken = refreshToken;
            _appKey = appKey;
            _appSecret = appSecret;
        }

        private async Task<string> RefreshAccessTokenAsync()
        {
            var client = new HttpClient();
            var requestContent = new FormUrlEncodedContent(new[]
            {
                new KeyValuePair<string, string>("grant_type", "refresh_token"),
                new KeyValuePair<string, string>("refresh_token", _refreshToken),
                new KeyValuePair<string, string>("client_id", _appKey),
                new KeyValuePair<string, string>("client_secret", _appSecret)
            });

            var response = await client.PostAsync("https://api.dropbox.com/oauth2/token", requestContent);
            response.EnsureSuccessStatusCode();

            var responseContent = await response.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<Dictionary<string, string>>(responseContent);

            _accessToken = tokenResponse["access_token"];
            return _accessToken;
        }

        public async Task<string> GetTemporaryLinkAsync(string dropboxFilePath)
        {
            await RefreshAccessTokenAsync();
            using (var dropboxClient = new DropboxClient(_accessToken))
            {
                var result = await dropboxClient.Files.GetTemporaryLinkAsync(dropboxFilePath);
                return result.Link;
            }
        }

        public async Task<string> GetPreviewLinkAsync(string dropboxFilePath)
        {
            await RefreshAccessTokenAsync();
            using (var dropboxClient = new DropboxClient(_accessToken))
            {
                var metadata = await dropboxClient.Files.GetMetadataAsync(dropboxFilePath);
                string previewLink = $"https://www.dropbox.com/preview{metadata.PathDisplay}";
                return previewLink;
            }
        }

        public async Task<string> GetViewLinkAsync(string dropboxFilePath)
        {
            await RefreshAccessTokenAsync();
            using (var dropboxClient = new DropboxClient(_accessToken))
            {
                var link = dropboxClient.Sharing.ListSharedLinksAsync(dropboxFilePath);
                string url;
                if (link.Result.Links.Count == 0)
                {
                    var result =
                           dropboxClient.Sharing.CreateSharedLinkWithSettingsAsync(dropboxFilePath);
                    url = result.Result.Url;
                }
                else
                {
                    url = link.Result.Links[0].Url;
                }
                return url;
            }
        }

        public async Task UploadFileAsync(string localFilePath, string dropboxFolderPath, string dropboxFileName)
        {
            await RefreshAccessTokenAsync();

            using (var dropboxClient = new DropboxClient(_accessToken))
            {
                using (var fileStream = new FileStream(localFilePath, FileMode.Open, FileAccess.Read))
                {
                    await dropboxClient.Files.UploadAsync(
                        path: $"{dropboxFolderPath}/{dropboxFileName}",
                        WriteMode.Overwrite.Instance,
                        body: fileStream
                    );
                }
            }
        }
        public async Task DeleteFileAsync(string dropboxFilePath)
        {
            using (var dropboxClient = new DropboxClient(_accessToken))
            {
                await dropboxClient.Files.DeleteV2Async(dropboxFilePath);
            }
        }
    }
}
