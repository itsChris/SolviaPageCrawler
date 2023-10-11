using HtmlAgilityPack;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;

namespace BrokenLinkChecker
{
    class Program
    {
        private static string RootUrl = "";
        private static readonly HashSet<string> VisitedUrls = new HashSet<string>();
        private static readonly List<string> ExternalUrls = new List<string>();
        private static readonly List<string> SpecialUrls = new List<string>();
        private static readonly List<(string BrokenLink, string Referrer)> BrokenUrls = new List<(string, string)>();
        private static readonly List<string> DownloadedGraphics = new List<string>();


        static async Task Main(string[] args)
        {
            if (args.Length == 0) {
                await Console.Out.WriteLineAsync("Please provide a valid URL as command line argument (i.e. https://www.solvia.ch)");
                return;
            }
            RootUrl = args[0];
            await CheckUrlForLinks(RootUrl);

            Console.WriteLine("\nExternal Links Found:");
            foreach (var url in ExternalUrls)
            {
                Console.WriteLine(url);
            }

            Console.WriteLine("\nSpecial URLs (tel, mailto, etc.):");
            foreach (var url in SpecialUrls)
            {
                Console.WriteLine(url);
            }

            Console.WriteLine("\nBroken URLs:");
            foreach (var (brokenLink, referrer) in BrokenUrls)
            {
                Console.WriteLine($"Broken link: {brokenLink} (Found on: {referrer})");
            }

            Console.WriteLine("\nVisited URLs:");
            foreach (var visited in VisitedUrls)
            {
                Console.WriteLine(visited);
            }

            Console.WriteLine("\nFinished checking.");
        }

        private static HttpClient CreateHttpClient()
        {
            var handler = new HttpClientHandler
            {
                ServerCertificateCustomValidationCallback = (sender, certificate, chain, sslPolicyErrors) =>
                {
                    if (sslPolicyErrors == System.Net.Security.SslPolicyErrors.None)
                    {
                        return true; // Valid certificate, no error.
                    }

                    // Print a red warning for an invalid certificate but proceed.
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.WriteLine($"Warning: SSL certificate error encountered for {certificate.Subject}: {sslPolicyErrors}");
                    Console.ResetColor();

                    return true; // Proceed even though there's an error.
                }
            };

            return new HttpClient(handler);
        }

        private static async Task DownloadGraphic(string graphicUrl)
        {
            using var client = CreateHttpClient();

            var bytes = await client.GetByteArrayAsync(graphicUrl);
            var filename = System.IO.Path.GetFileName(graphicUrl);
            var path = System.IO.Path.Combine("DownloadedGraphics", filename); // Save graphics to "DownloadedGraphics" folder.

            if (!System.IO.Directory.Exists("DownloadedGraphics"))
            {
                System.IO.Directory.CreateDirectory("DownloadedGraphics");
            }

            await System.IO.File.WriteAllBytesAsync(path, bytes);
            DownloadedGraphics.Add(path);
            Console.WriteLine($"Downloaded graphic: {graphicUrl} -> {path}");
        }

        private static async Task CheckUrlForLinks(string url, string referrer = null)
        {
            if (VisitedUrls.Contains(url))
                return;

            VisitedUrls.Add(url);
            Console.WriteLine($"Checking URL: {url}");

            using var client = CreateHttpClient();

            try
            {
                var response = await client.GetAsync(url);

                if ((int)response.StatusCode >= 300) // 3xx, 4xx, 5xx are considered as broken links.
                {
                    BrokenUrls.Add((url, referrer));
                    return; // If the link is broken, we don't need to parse its content.
                }

                var content = await response.Content.ReadAsStringAsync();
                var doc = new HtmlDocument();
                doc.LoadHtml(content);

                var links = doc.DocumentNode.SelectNodes("//a[@href]")?.Select(a => a.GetAttributeValue("href", null))
                    .Where(u => !string.IsNullOrEmpty(u)).ToList() ?? new List<string>();

                foreach (var link in links)
                {
                    if (link.StartsWith("tel:") || link.StartsWith("mailto:"))
                    {
                        if (!SpecialUrls.Contains(link))
                        {
                            SpecialUrls.Add(link);
                        }
                        continue;
                    }

                    var absoluteUrl = new Uri(new Uri(url), link).AbsoluteUri;

                    if (absoluteUrl.StartsWith(RootUrl))
                    {
                        await CheckUrlForLinks(absoluteUrl, url); // Passing current URL as referrer
                    }
                    else if (!ExternalUrls.Contains(absoluteUrl))
                    {
                        ExternalUrls.Add(absoluteUrl);
                    }
                }

                // Check for graphic elements
                var imgNodes = doc.DocumentNode.SelectNodes("//img[@src]");
                if (imgNodes != null)
                {
                    foreach (var imgNode in imgNodes)
                    {
                        var srcValue = imgNode.GetAttributeValue("src", null);
                        if (!string.IsNullOrEmpty(srcValue))
                        {
                            var graphicUrl = new Uri(new Uri(url), srcValue).AbsoluteUri;
                            var extension = System.IO.Path.GetExtension(graphicUrl).ToLower();
                            if (extension == ".png" || extension == ".jpeg" || extension == ".jpg" || extension == ".svg")
                            {
                                await DownloadGraphic(graphicUrl);
                            }
                        }
                    }
                }
            }
            catch (Exception e)
            {
                Console.WriteLine($"Error checking {url}: {e.Message}");
            }
        }
    }
}
