using System;
using System.IO;
using System.Reflection;
using System.Text;
using CodeFluent.Runtime.Utilities;

namespace SoftFluent.Documenter
{
    public static class Markdown
    {
        // https://github.com/chjj/marked

        private static readonly string _javaScriptCode = LoadJavaScriptCode();

        private static string LoadJavaScriptCode()
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("function Main(md) {");
            sb.AppendLine(@"
                if(typeof window === 'undefined') {
                    var window = {};
                }");

            // load stmd.js from resources
            var assembly = Assembly.GetExecutingAssembly();
            var stmdResourceName = assembly.GetName().Name + ".marked.js";
            using (Stream stream = assembly.GetManifestResourceStream(stmdResourceName))
            using (StreamReader reader = new StreamReader(stream))
            {
                sb.Append(reader.ReadToEnd());
            }

            sb.AppendLine(@"
                return marked(md);");
            sb.AppendLine("}");
            sb.AppendLine("Main");
            return sb.ToString();
        }

        /// <summary>
        /// Parses a markdown document and convert it to HTML.
        /// </summary>
        /// <param name="commonMarkText">The Markdown text to parse.</param>
        /// <returns>The HTML.</returns>
        public static string ToHtml(string commonMarkText)
        {
            if (string.IsNullOrWhiteSpace(commonMarkText))
                return commonMarkText;

            // NOTE: we could re-use the engine to cache the parsed script, etc. but beware to threading-issues
            using (JsRuntime jsRuntime = new JsRuntime())
            {
                JsRuntime.JsContext.Current = jsRuntime.CreateContext();
                JsRuntime.JsValue jsValue = jsRuntime.ParseScript(_javaScriptCode);

                Exception exception;
                JsRuntime.JsValue mainFunction;
                if (jsValue.TryCall(out exception, out mainFunction)) // Get the Main function
                {
                    JsRuntime.JsValue result;
                    if (mainFunction.TryCall(out exception, out result, null, commonMarkText))
                    {
                        return result.Value as string;
                    }
                }
                return null;
            }
        }
    }
}