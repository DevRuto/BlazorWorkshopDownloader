using BlazorWorkshopDownloader.Shared;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

namespace BlazorWorkshopDownloader.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class SteamController : ControllerBase
    {
        private readonly IConfiguration _config;

        public SteamController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("proxy")]
        public async Task<IActionResult> Map(string steamurl, string filename)
        {
            var currentDirectory = Directory.GetCurrentDirectory();
            var tempPath = Path.Combine(currentDirectory, "Temp");
            if (!Directory.Exists(tempPath))
                Directory.CreateDirectory(tempPath);

            using var webClient = new WebClient();
            var baseTemp = Path.Combine(tempPath, Path.GetFileNameWithoutExtension(filename));
            var tempFile = baseTemp + ".zip";
            if (System.IO.File.Exists(tempFile))
                System.IO.File.Delete(tempFile);
            if (System.IO.File.Exists(baseTemp + ".bsp"))
                System.IO.File.Delete(baseTemp + ".bsp");
            await webClient.DownloadFileTaskAsync(new Uri(steamurl), tempFile);
            ZipFile.ExtractToDirectory(tempFile, tempPath);
            return File(new FileStream(baseTemp + ".bsp", FileMode.Open), "application/octet-stream", Path.GetFileNameWithoutExtension(filename) + ".bsp");
        }

        [HttpGet("details")]
        public async Task<IActionResult> Details([FromQuery] int[] workshopIds)
        {
            var urlParams = new Dictionary<string, string>();
            urlParams.Add("itemcount", workshopIds.Length.ToString());
            int i = 0;
            foreach (var id in workshopIds)
            {
                urlParams.Add($"publishedfileids[{i++}]", id.ToString());
            }
            var req = new HttpRequestMessage(HttpMethod.Post, "https://api.steampowered.com/ISteamRemoteStorage/GetPublishedFileDetails/v1/")
            {
                Content = new FormUrlEncodedContent(urlParams)
            };
            var httpClient = new HttpClient();
            var res = await httpClient.SendAsync(req);
            var data = await res.Content.ReadAsStringAsync();
            return new JsonResult(JsonConvert.DeserializeObject<SteamResponse>(data).response.publishedfiledetails);
        }
    }
}
