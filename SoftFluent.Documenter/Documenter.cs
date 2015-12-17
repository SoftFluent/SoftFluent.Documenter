using System;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using CodeFluent.Runtime.Utilities;

namespace SoftFluent.Documenter
{
    public class Documenter
    {
        private string renderingPath = "html";
        private string templateContent = "";
        private string summaryContent = "";

        private void Init()
        {
            if (Directory.Exists(renderingPath))
            {
                IOUtilities.DirectoryDelete(renderingPath, true);
            }

            Directory.CreateDirectory(renderingPath);

            if (!Directory.Exists(string.Concat(renderingPath, "/css")))
            {
                Directory.CreateDirectory(string.Concat(renderingPath, "/css"));
            }

            if (!Directory.Exists(string.Concat(renderingPath, "/js")))
            {
                Directory.CreateDirectory(string.Concat(renderingPath, "/js"));
            }

            if (!Directory.Exists(string.Concat(renderingPath, "/img")))
            {
                Directory.CreateDirectory(string.Concat(renderingPath, "/img"));
            }

            // copy template ressources
            foreach (string filePath in Directory.GetFiles("Template/css"))
            {
                File.Copy(filePath, string.Concat(renderingPath, "/css/", Path.GetFileName(filePath)));
            }

            foreach (string filePath in Directory.GetFiles("Template/js"))
            {
                File.Copy(filePath, string.Concat(renderingPath, "/js/", Path.GetFileName(filePath)));
            }

            foreach (string filePath in Directory.GetFiles("Template/img"))
            {
                File.Copy(filePath, string.Concat(renderingPath, "/img/", Path.GetFileName(filePath)));
            }
        }

        public bool RenderDirectory(string sourcePath)
        {
            // check source directory
            if (!Directory.Exists(sourcePath))
            {
                Console.WriteLine("Directory not found!");
                return false;
            }

            // init rendering directory
            Init();

            // init template
            templateContent = File.ReadAllText("Template/index.html");

            // init summary rendering
            summaryContent = InitSummaryContent(sourcePath);

            return RecursiveRendering(sourcePath, renderingPath);
        }

        private string InitSummaryContent(string sourcePath)
        {
            string result = Markdown.ToHtml(File.ReadAllText(string.Concat(sourcePath, "/", "SUMMARY.md")));
            result = result.Replace(".md", ".html");
            result = result.Replace("<ul>", "<ol>");
            result = result.Replace("</ul>", "</ol>");
            result = result.Replace("<ol>", "<ol class=\"nav nav-pills nav-stacked\">");
            result = result.Replace("<h1 id=\"summary\">Summary</h1>", "");
            return result;
        }

        private string LocateSummaryContent(string content, string fileName, string basePath, string pathLevelPath)
        {
            string finalBasePath = basePath.Replace("html", "").Trim('/');
            content = content.Replace("<li><a href=\"", "<li><a href=\"" + pathLevelPath);
            content = content.Replace("<li><a href=\"" + pathLevelPath + (finalBasePath.Length > 0 ? finalBasePath + "/" : "") + fileName, "<li class=\"active\"><a href=\"" + pathLevelPath + (finalBasePath.Length > 0 ? finalBasePath + "/" : "") + fileName);
            content = content.Replace("README.html", "index.html");
            return content;
        }

        private bool RecursiveRendering(string sourcePath, string basePath)
        {
            if (Path.GetFileName(sourcePath) == ".git" || Path.GetFileName(sourcePath) == ".gitignore")
                return false;

            if (!Directory.Exists(basePath))
            {
                Directory.CreateDirectory(basePath);
            }

            // render md files
            foreach (string filePath in Directory.GetFiles(sourcePath))
            {
                if (Path.GetExtension(filePath) == ".md" && Path.GetFileName(filePath).ToUpper() != "SUMMARY.md")
                {
                    string content = File.ReadAllText(filePath);

                    // set base path
                    int pathLevel = basePath.Count(c => c == '/');
                    string pathLevelPath = string.Concat(Enumerable.Repeat("../", (pathLevel > 0 ? pathLevel : 0)));
                    string fileContent = templateContent.Replace("{{BASE}}", pathLevelPath);

                    // set md transformed content
                    string htmlContent = Markdown.ToHtml(content);
                    htmlContent = htmlContent.Replace("<table>", "<table class=\"table table-bordered table-striped\">");
                    htmlContent = htmlContent.Replace(".md", ".html");
                    htmlContent = htmlContent.Replace("href=\"http", "target=\"_blank\" href=\"http");
                    htmlContent = new Regex("<h2 id=\"([^\"]*)\">").Replace(htmlContent, "<a href=\"#$1\"><h2 id=\"$1\">");
                    htmlContent = htmlContent.Replace("</h2>", "</h2></a>");
                    htmlContent = new Regex("<p><img src=\"([^\"]*)\" alt=\"([^\"]*)\"></p>").Replace(htmlContent, "<p class=\"text-center\"><a href=\"$1\"><img src=\"$1\" alt=\"$2\"></a></p>");
                    fileContent = fileContent.Replace("{{CONTENT}}", htmlContent);

                    // set md summary transformed content
                    fileContent = fileContent.Replace("{{SUMMARY}}", LocateSummaryContent(summaryContent,
                        Path.GetFileName(filePath).Replace("md", "html"),
                        basePath,
                        pathLevelPath));

                    fileContent = fileContent.Replace("{{DATE}}", DateTime.Now.ToString());
                    Match titleMatch = new Regex("<h1.*>(.*)</h1>").Match(htmlContent);
                    if (titleMatch.Groups.Count > 1)
                    {
                        fileContent = fileContent.Replace("{{TITLE}}", titleMatch.Groups[1].Value);
                    }
                    else
                    {
                        fileContent = fileContent.Replace("{{TITLE}} |", "");
                    }

                    string fileName = Path.GetFileNameWithoutExtension(filePath);
                    File.WriteAllText(string.Concat(basePath, "/", (fileName == "README" ? "index" : fileName), ".html"), fileContent);
                }
                else if (Path.GetExtension(filePath) == ".jpeg"
                        || Path.GetExtension(filePath) == ".jpg"
                        || Path.GetExtension(filePath) == ".gif"
                        || Path.GetExtension(filePath) == ".png"
                        || Path.GetExtension(filePath) == ".bmp"
                    )
                {
                    File.Copy(filePath, string.Concat(basePath, "/", Path.GetFileName(filePath)));
                }
            }

            // search in subdirectories
            foreach (string directoryPath in Directory.GetDirectories(sourcePath))
            {
                RecursiveRendering(directoryPath, string.Concat(basePath, "/", Path.GetFileName(directoryPath)));
            }

            return true;
        }
    }
}
