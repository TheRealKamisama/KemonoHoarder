using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Text.RegularExpressions;
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
            var linkParser = new Regex(@"http(s)?://([\w-]+\.)+[\w-]+(/[\w- ./?%&=]*)?", RegexOptions.Compiled | RegexOptions.IgnoreCase);
            var contenturls = new List<string>();
            var embededs = new List<Embed>();
            foreach (var post in posts)
            {
                var folder =
                    $"[{post.Published.Year}-{post.Published.Month}-{post.Published.Day}] [{post.Id}] {post.Title.RemoveIllegalChars()}".Trim();
                var urls = new StringBuilder();
                Directory.CreateDirectory(Path.Combine(path, folder));
                foreach (var attachment in post.Attachments)
                {
                    downloads.Add(new FileToDownload{ path = Path.Combine(path, folder), name = attachment.Name, url = data + attachment.Path});
                    urls.AppendLine($"{attachment.Name},{data}{attachment.Path}");
                }

                if (post.File != default)
                {
                    downloads.Add(new FileToDownload{path = Path.Combine(path, folder), name = post.File.Name, url = data + post.File.Path});
                    urls.AppendLine($"{post.File.Name},{data}{post.File.Path}");
                }

                var contenturl = linkParser.Matches(post.Content);
                contenturls.AddRange(contenturl.Select(e => e.Value));
                await System.IO.File.WriteAllTextAsync(Path.Combine(path, folder, "urls.txt"), urls.ToString());
                await System.IO.File.WriteAllTextAsync(Path.Combine(path, folder, "content.txt"), post.Content);
                if (contenturl.Any())
                {
                    await System.IO.File.WriteAllLinesAsync(Path.Combine(path, folder, "contenturls.txt"),
                        contenturl.Select(e => e.Value));
                }
                if (post.Embed.Url != null)
                {
                    embededs.Add(post.Embed);
                    await System.IO.File.WriteAllTextAsync(Path.Combine(path, folder, "embeded.txt"),
                        $"[{post.Embed.Subject}][{post.Embed.Description}],{post.Embed.Url}");
                }
                Console.WriteLine($"作品: {post.Title} 附件: {post.Attachments.Count} 件 内含链接: {contenturl.Count} 个");
            }
            Console.WriteLine("附件链接已写入 urls.txt, 内容已写入: content.txt, 内容内的链接已写入 contenturls.txt, 嵌入内容已写入: embeded.txt.");
            contenturls = contenturls.Distinct().ToList();
            await System.IO.File.WriteAllLinesAsync(Path.Combine(path, "all_contenturls.txt"), contenturls);
            Console.WriteLine("所有内容里的链接已写入: all_contenturls.txt");
            await System.IO.File.WriteAllTextAsync(Path.Combine(path, "all_embededs.txt"), embededs.ToJsonString());
            Console.WriteLine("所有嵌入内容已写入: all_embededs.txt");
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
                Address = new Uri($"http://192.168.0.104:7890"),
                BypassProxyOnLocal = false,
                UseDefaultCredentials = true
            };
            var httpClientHandler = new HttpClientHandler
            {
                Proxy = proxy,
            };
            return  new HttpClient(handler: new RetryHandler(httpClientHandler), disposeHandler: true);
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
                    var failed = true;
                    while (failed)
                    {
                        await hc.DownloadAsync(f.url, Path.Combine(f.path, f.name));
                        failed = false;
                    }
                    Console.WriteLine($"第{current}/{downloads .Count}个文件的下载结束.");
                }
                catch (Exception)
                {
                    // ignored
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

            result = result.Replace(".", "");
            return result;
        }
    }
    public class RetryHandler : DelegatingHandler
    {
        // Strongly consider limiting the number of retries - "retry forever" is
        // probably not the most user friendly way you could respond to "the
        // network cable got pulled out."
        private const int MaxRetries = 5;

        public RetryHandler(HttpMessageHandler innerHandler) : base(innerHandler)
        {

        }

        protected override async Task<HttpResponseMessage> SendAsync(
            HttpRequestMessage request,
            CancellationToken cancellationToken)
        {
            HttpResponseMessage response = null;
            for (int i = 0; i < MaxRetries; i++)
            {
                response = await base.SendAsync(request, cancellationToken);
                if (response.IsSuccessStatusCode) {
                    return response;
                }
            }

            return response;
        }
    }
}
