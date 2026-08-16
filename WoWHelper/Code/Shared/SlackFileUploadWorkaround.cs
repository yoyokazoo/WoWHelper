using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using Newtonsoft.Json.Linq;
using WindowsGameAutomationTools.Images;
using WindowsGameAutomationTools.Slack;

namespace WoWHelper.Shared
{
    // TEMPORARY HACK -- delete this whole file once WindowsGameAutomationTools.Slack.SlackHelper
    // (github.com/yoyokazoo/WindowsGameAutomationTools) is fixed upstream and this project bumps
    // to that package version, then switch call sites back to SlackHelper.SendScreenshotToChannel/
    // UploadFile.
    //
    // Slack deprecated the old single-call files.upload endpoint (it now returns
    // "method_deprecated"), replacing it with a 3-step flow: files.getUploadURLExternal -> POST
    // the raw bytes to the URL that returns -> files.completeUploadExternal to attach/share it.
    // Confirmed via reflection that neither SlackHelper.UploadFile/SendScreenshotToChannel nor
    // the SlackAPI package it wraps implement this new flow (both only reference the deprecated
    // files.upload endpoint) -- see chat history for details. This reimplements just the new
    // flow directly against Slack's HTTP API, reusing SlackHelper.LoadSlackProfile() for
    // credentials since that part isn't broken.
    public static class SlackFileUploadWorkaround
    {
        private static readonly HttpClient httpClient = new HttpClient();

        // NOTE: SlackHelper.LoadSlackProfile() returns a (string, string) ValueTuple. Its
        // declared property order (ChannelId, then OauthToken) suggests this is
        // (channelId, oauthToken), but that wasn't confirmed by decompiling the method body. If
        // this fails with an auth-shaped error (invalid_auth, not_authed) rather than a
        // channel-shaped one (channel_not_found), swap this line.
        public static async Task<bool> UploadFileToChannelAsync(
            byte[] fileData,
            string fileName,
            string title = null,
            string initialComment = null,
            string threadTs = null)
        {
            var (channelId, oauthToken) = SlackHelper.LoadSlackProfile();

            // Step 1: ask Slack for a URL to upload the raw bytes to.
            var getUrlRequest = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/files.getUploadURLExternal")
            {
                Content = new FormUrlEncodedContent(new Dictionary<string, string>
                {
                    ["filename"] = fileName,
                    ["length"] = fileData.Length.ToString(),
                }),
            };
            getUrlRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);

            var getUrlResponse = await httpClient.SendAsync(getUrlRequest);
            string getUrlJson = await getUrlResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"SlackFileUploadWorkaround: files.getUploadURLExternal response: {getUrlJson}");

            var getUrlResult = JObject.Parse(getUrlJson);
            if (getUrlResult.Value<bool?>("ok") != true)
            {
                Console.WriteLine($"SlackFileUploadWorkaround: getUploadURLExternal failed, aborting.");
                return false;
            }

            string uploadUrl = getUrlResult.Value<string>("upload_url");
            string fileId = getUrlResult.Value<string>("file_id");

            // Step 2: upload the actual bytes to that URL. Pre-signed -- no auth header needed.
            using (var uploadContent = new MultipartFormDataContent())
            {
                uploadContent.Add(new ByteArrayContent(fileData), "file", fileName);

                var uploadResponse = await httpClient.PostAsync(uploadUrl, uploadContent);
                string uploadResponseText = await uploadResponse.Content.ReadAsStringAsync();
                Console.WriteLine($"SlackFileUploadWorkaround: raw upload status {uploadResponse.StatusCode}, body: {uploadResponseText}");

                if (!uploadResponse.IsSuccessStatusCode)
                {
                    Console.WriteLine($"SlackFileUploadWorkaround: raw upload failed, aborting.");
                    return false;
                }
            }

            // Step 3: finalize the upload and share it to the channel.
            var completePayload = new JObject
            {
                ["files"] = new JArray
                {
                    new JObject
                    {
                        ["id"] = fileId,
                        ["title"] = title ?? fileName,
                    },
                },
                ["channel_id"] = channelId,
            };

            if (!string.IsNullOrEmpty(initialComment))
            {
                completePayload["initial_comment"] = initialComment;
            }

            if (!string.IsNullOrEmpty(threadTs))
            {
                completePayload["thread_ts"] = threadTs;
            }

            var completeRequest = new HttpRequestMessage(HttpMethod.Post, "https://slack.com/api/files.completeUploadExternal")
            {
                Content = new StringContent(completePayload.ToString(), Encoding.UTF8, "application/json"),
            };
            completeRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", oauthToken);

            var completeResponse = await httpClient.SendAsync(completeRequest);
            string completeJson = await completeResponse.Content.ReadAsStringAsync();
            Console.WriteLine($"SlackFileUploadWorkaround: files.completeUploadExternal response: {completeJson}");

            var completeResult = JObject.Parse(completeJson);
            bool ok = completeResult.Value<bool?>("ok") == true;
            if (!ok)
            {
                Console.WriteLine($"SlackFileUploadWorkaround: completeUploadExternal failed.");
            }

            return ok;
        }

        // Convenience matching SlackHelper.SendScreenshotToChannel's ease of use: captures the
        // screen (or just cropRegion of it, if given) and uploads it. cropRegion is normally
        // WowScreenConfiguration.SlackScreenshotCropRegion -- null there (not configured for the
        // current resolution) falls back to the whole primary screen here.
        public static async Task<bool> UploadScreenshotToChannelAsync(
            string title = "Screenshot",
            string initialComment = null,
            string threadTs = null,
            Rectangle? cropRegion = null)
        {
            Rectangle captureRegion = cropRegion ?? new Rectangle(0, 0, Screen.PrimaryScreen.Bounds.Width, Screen.PrimaryScreen.Bounds.Height);

            using (Bitmap screenshot = ScreenCapture.CaptureBitmapFromDesktopAndRectangle(captureRegion))
            using (var stream = new MemoryStream())
            {
                screenshot.Save(stream, ImageFormat.Png);
                byte[] fileData = stream.ToArray();

                return await UploadFileToChannelAsync(fileData, "screenshot.png", title, initialComment, threadTs);
            }
        }
    }
}
