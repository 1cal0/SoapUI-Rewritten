using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Xml.Linq;
using Newtonsoft.Json;
using SoapUI.Common;
using SoapUI.Models;

namespace SoapUI.Services.IO
{
    /// <summary>
    /// Loads/saves script argument lists. JSON is the native format;
    /// XML files are accepted on import as well. Format is detected
    /// from the file extension with a content-sniffing fallback, so
    /// callers never have to care which one they got.
    /// JSON shape: [{"type":"...","value":"..."}]
    /// XML shape:
    ///   &lt;arguments&gt;
    ///     &lt;argument&gt;&lt;type&gt;...&lt;/type&gt;&lt;value&gt;...&lt;/value&gt;&lt;/argument&gt;
    ///   &lt;/arguments&gt;
    /// Keeps file dialogs in the UI but file format knowledge here.
    /// </summary>
    public sealed class ArgumentFileStore
    {
        public IList<LuaArgument> Load(string path)
        {
            Guard.AgainstNullOrWhiteSpace(path, nameof(path));

            string content = File.ReadAllText(path, Encoding.UTF8);

            if (LooksLikeXml(path, content))
            {
                return LoadXml(content);
            }

            return LoadJson(content);
        }

        public string Save(IList<LuaArgument> arguments, string directory, string fileName)
        {
            Guard.AgainstNull(arguments, nameof(arguments));
            Guard.AgainstNullOrWhiteSpace(directory, nameof(directory));
            Guard.AgainstNullOrWhiteSpace(fileName, nameof(fileName));

            Directory.CreateDirectory(directory);

            string json = JsonConvert.SerializeObject(arguments, Formatting.Indented);
            string fullPath = Path.Combine(directory, fileName);
            File.WriteAllText(fullPath, json, Encoding.UTF8);
            return fullPath;
        }

        private static bool LooksLikeXml(string path, string content)
        {
            if (path.EndsWith(".xml", StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }

            if (path.EndsWith(".json", StringComparison.OrdinalIgnoreCase) ||
                path.EndsWith(".txt", StringComparison.OrdinalIgnoreCase))
            {
                return false;
            }

            return content.TrimStart().StartsWith("<", StringComparison.Ordinal);
        }

        private static IList<LuaArgument> LoadJson(string content)
        {
            var args = JsonConvert.DeserializeObject<List<LuaArgument>>(content);
            return args ?? new List<LuaArgument>();
        }

        private static IList<LuaArgument> LoadXml(string content)
        {
            var list = new List<LuaArgument>();
            var doc = XDocument.Parse(content);

            if (doc.Root == null)
            {
                return list;
            }

            foreach (var node in doc.Root.Elements("argument"))
            {
                var typeNode = node.Element("type");
                var valueNode = node.Element("value");

                string type = typeNode != null ? typeNode.Value : LuaType.Nil;
                string value = valueNode != null ? valueNode.Value : string.Empty;

                list.Add(new LuaArgument(type, value));
            }

            return list;
        }
    }
}
