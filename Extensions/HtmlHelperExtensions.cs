using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;
using MarkdownSharp;

namespace Server.Extensions
{
    public static class HtmlHelperExtensions
    {
        static readonly Markdown MarkdownTransformer = new Markdown();

        public static IHtmlString Markdown(this HtmlHelper helper, string text)
        {
            string html = MarkdownTransformer.Transform(text);
            return new MvcHtmlString(html);
        }

    }
}
