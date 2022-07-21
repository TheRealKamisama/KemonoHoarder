using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using GammaLibrary.Extensions;

namespace KemonoHoarder
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("请输入作者平台(patreon, fanbox, discord):");
            var platform = Console.ReadLine()?.ToLower();
            Console.WriteLine("请输入作者id:");
            var id = Console.ReadLine();
            var i = 0;
            var posts = new List<Post>();
            var user = $"https://kemono.party/api/{platform}/user/{id}";
            Console.WriteLine("正在获取作者数据···");
            var hc = GetProxyHC();
            var creators = new List<Creator>();
            if (System.IO.File.Exists("creators.json"))
            {
                creators = System.IO.File.ReadAllText("creators.json").JsonDeserialize<List<Creator>>();
            }
            else
            {
                creators = await hc.GetJsonAsync<List<Creator>>("https://kemono.party/api/creators");
                await System.IO.File.WriteAllTextAsync("creators.json", creators.ToJsonString());
            }
            var creator = creators.First(c => c.Id == id && c.Service.ToLower() == platform?.ToLower());
            Console.WriteLine($"作者: {creator.Name}");
            var path = Path.GetFullPath(Path.Combine("Downloads", platform, $"{creator.Name} [{creator.Id}]"));
            if (!Directory.Exists(path))
            {
                Directory.CreateDirectory(path);
            }
            Console.WriteLine("正在获取作品数据···");
            while (true)
            {
                var result = await hc.GetJsonAsync<List<Post>>($"{user}?o={i}");
                if (!result.Any())
                {
                    break;   
                }
                posts.AddRange(result);
                i += 25;
            }
            Console.WriteLine($"已找到作品: {posts.Count} 个");
            const string data = "https://kemono.party/data";
            var downloads = new List<FileToDownload>();
            foreach (var post in posts)
            {
                var folder =
                    $"[{post.Published.Year}-{post.Published.Month}-{post.Published.Day}] [{post.Id}] {post.Title.RemoveIllegalChars()}".Trim();
                var urls = "";
                Directory.CreateDirectory(Path.Combine(path, folder));
                foreach (var attachment in post.Attachments)
                {
                    downloads.Add(new FileToDownload{ path = Path.Combine(path, folder), name = attachment.Name, url = data + attachment.Path});
                    urls += $"{Environment.NewLine}{attachment.Name},{data}{attachment.Path}";
                }
                await System.IO.File.WriteAllTextAsync(Path.Combine(path, folder, "urls.txt"), urls);
            }
            Console.WriteLine($"已找到附件: {downloads.Count} 个");
            Console.WriteLine($"正在下载附件···");
            DownloadPics(downloads).Wait();
        }

        internal class FileToDownload
        {
            public string url { get; set; }
            public string path { get; set; }
            public string name { get; set; }
        }
        public static HttpClient GetProxyHC()
        {
            var proxy = new WebProxy
            {
                Address = new Uri($"http://localhost:7890"),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = true
            };
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
            };
            return  new HttpClient(handler: httpClientHandler, disposeHandler: true);
        }
        private static volatile int count = 1;
        static SemaphoreSlim semaphoreSlim = new SemaphoreSlim(50);
        public static async Task DownloadPics(List<FileToDownload> downloads)
        {
            var hc = GetProxyHC();
            var tasks = downloads.Select(async f =>
            {
                try
                {
                    await semaphoreSlim.WaitAsync();
                    var current = count;
                    Interlocked.Increment(ref count);
                    Console.WriteLine($"第{current}/{downloads.Count}个文件的下载开始.");
                    await hc.DownloadAsync(f.url, Path.Combine(f.path, f.name));
                    Console.WriteLine($"第{current}/{downloads .Count}个文件的下载结束.");
                }
                finally
                {
                    semaphoreSlim.Release();
                }

            });


            await Task.WhenAll(tasks);
        }
    }

    public static class StringExtensions
    {
        public static string RemoveIllegalChars(this string input)
        {
            var result = input;
            string invalid = new string(Path.GetInvalidFileNameChars()) + new string(Path.GetInvalidPathChars());

            foreach (char c in invalid)
            {
                result = result.Replace(c.ToString(), ""); 
            }

            return result;
        }
    }
}
