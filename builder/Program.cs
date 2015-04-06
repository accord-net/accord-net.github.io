using HtmlAgilityPack;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System;
using System.Text.RegularExpressions;

namespace builder
{
    class Program
    {
        static void Main(string[] args)
        {
            var list = GetSampleApplications();

            string categories = GetCategories(list);
            string thumb = GetThumbnails(list);
            string text = GetText(list);
            string js = GetJavascript(list);

            string file = @"..\..\..\samples.html";

            var html = File.ReadAllText(file);

            html = replace(categories, html, "<!-- BEGIN CATEGORIES -->", "<!-- END CATEGORIES -->");
            html = replace(thumb, html, "<!-- BEGIN THUMBNAILS -->", "<!-- END THUMBNAILS -->");
            html = replace(text, html, "<!-- BEGIN TEXT -->", "<!-- END TEXT -->");
            html = replace(js, html, "// BEGIN JAVASCRIPT", "// END JAVASCRIPT");

            html = Regex.Replace(html, @"\r\n|\n\r|\n|\r", "\r\n");

            File.WriteAllText(file, html);
        }

        private static string replace(string categories, string html, string startStr, string endStr)
        {
            int begin = html.IndexOf(startStr);
            int end = html.LastIndexOf(endStr);
            html = html.Remove(begin, end - begin);
            html = html.Insert(begin, startStr + categories);
            return html;
        }

        private static string GetCategories(List<Sample> list)
        {
            string template = @"            <li data-value=""{0}""><a href=""#"">{1}</a></li>";

            var builder = new StringBuilder();

            var categories = from Sample s in list
                             group s by s.Category into g
                             select g.Key;

            builder.AppendLine();
            foreach (var category in categories)
            {
                builder.AppendFormat(template, category.Id, category.Name);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string GetThumbnails(List<Sample> list)
        {
            string template = @"
            <li data-type=""{0}"" data-id=""id-{1}"" class=""sample-app"">
              <a href=""#"" id=""thumb-{1}""><h2>{3}</h2><img src=""{2}"" alt=""{3}""></a>
            </li>";

            var builder = new StringBuilder();

            builder.AppendLine();
            foreach (var sample in list)
            {
                builder.AppendFormat(template, sample.Category.Id, sample.Index, sample.Image, sample.Title);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string GetText(List<Sample> list)
        {
            string template = @"            <div id=""text-{0}"" class=""hidden"">{1}</div>";

            var builder = new StringBuilder();
            builder.AppendLine();

            foreach (var sample in list)
            {
                builder.AppendFormat(template, sample.Index, sample.Text);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static string GetJavascript(List<Sample> list)
        {
            string template = @"
            $('#thumb-{0}').off().on('click', function () {{
                var content = $('#text-{0}').clone(false);
                $(content).removeClass('hidden');
                bootbox.dialog({{ message: content, title: '{1}', buttons: {{
                        ""Download"": function() {{ window.location.assign('{2}'); }}
                    }}}});
            }});";

            var builder = new StringBuilder();
            builder.AppendLine();
            foreach (var sample in list)
            {
                builder.AppendFormat(template, sample.Index, sample.Title, sample.Zip);
                builder.AppendLine();
            }

            return builder.ToString();
        }

        private static List<Sample> GetSampleApplications()
        {
            var web = new HtmlWeb();
            var wiki = web.Load(@"https://github.com/accord-net/framework/wiki/Sample-applications");

            var list = new List<Sample>();

            var sections = wiki.DocumentNode.SelectNodes("//*[@id='wiki-body']//h1");

            int index = 0;

            var current = sections.FirstOrDefault();

            Category category = null;
            Sample sample = null;

            StringBuilder builder = new StringBuilder();

            while (current != null)
            {
                if (current.Name == "h1")
                {
                    if (sample != null)
                        sample.Text = builder.ToString();
                    builder.Length = 0;

                    category = new Category
                    {
                        Id = identify(current.InnerText),
                        Name = trim(current.InnerText)
                    };

                    sample = null;
                }
                else if (current.Name == "h2")
                {
                    if (sample != null)
                        sample.Text = builder.ToString();
                    builder.Length = 0;

                    sample = new Sample
                    {
                        Category = category,
                        Title = trim(current.InnerText),
                        TitleId = identify(current.InnerText),
                        Index = index++
                    };

                    list.Add(sample);
                }
                else
                {
                    if (sample != null)
                    {
                        if (String.IsNullOrEmpty(sample.Image))
                        {
                            var img = current.SelectSingleNode(".//img");
                            if (img != null)
                                sample.Image = img.Attributes["src"].Value;
                        }

                        if (String.IsNullOrEmpty(sample.Zip))
                        {
                            var a = current.SelectSingleNode(".//a");
                            if (a != null)
                            {
                                var url = a.Attributes["href"].Value;
                                if (url.EndsWith(".zip"))
                                    sample.Zip = url;
                            }
                        }

                        builder.Append(current.OuterHtml);
                    }
                }

                current = current.NextSibling;
            }

            if (sample != null)
                sample.Text = builder.ToString();


            return list;
        }

        private static string identify(string str)
        {
            return Regex.Replace(str.ToLower(), @"[^0-9a-zA-Z]+", "-").Trim(' ', '-');
        }

        private static string trim(string str)
        {
            return str.Trim('\n', '\r');
        }
    }
}
