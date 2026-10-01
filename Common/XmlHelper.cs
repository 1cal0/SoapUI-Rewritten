using System;
using System.Linq;
using System.Security;
using System.Text;
using System.Xml.Linq;

namespace SoapUI.Common
{
    /// <summary>
    /// Small XML helpers shared by the SOAP envelope builder and the
    /// response parser. Centralises escaping and namespace-agnostic lookup
    /// so call sites don't repeat LINQ-to-XML boilerplate.
    /// </summary>
    internal static class XmlHelper
    {
        /// <summary>
        /// Escapes a string for inclusion in an XML element body.
        /// NOTE: ampersand must be replaced first.
        /// </summary>
        public static string Escape(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            // SecurityElement.Escape handles &, <, >, ", ' in the right order.
            return SecurityElement.Escape(value);
        }

        /// <summary>
        /// Escapes a Lua script for inclusion in a SOAP XML element body.
        /// Mirrors PHP's htmlspecialchars($lua, ENT_XML1, 'UTF-8', false):
        /// encodes &amp;, &lt;, &gt; per XML 1.0, leaves quotes untouched,
        /// and does NOT double-encode existing entities.
        /// Example: "a &lt; b &amp;&amp; c &gt; d" stays exactly as-is,
        /// while raw "a &lt; b &amp;&amp; c &gt; d" input becomes it.
        /// </summary>
        public static string EscapeScript(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            var sb = new StringBuilder(value.Length);
            for (int i = 0; i < value.Length; i++)
            {
                char c = value[i];
                switch (c)
                {
                    case '<':
                        sb.Append("&lt;");
                        break;
                    case '>':
                        sb.Append("&gt;");
                        break;
                    case '&':
                        int entityLength;
                        if (IsEntityAt(value, i, out entityLength))
                        {
                            sb.Append(value, i, entityLength);
                            i += entityLength - 1;
                        }
                        else
                        {
                            sb.Append("&amp;");
                        }

                        break;
                    default:
                        sb.Append(c);
                        break;
                }
            }

            return sb.ToString();
        }

        /// <summary>
        /// Checks whether the '&amp;' at <paramref name="index"/> starts an
        /// already-encoded entity (&amp;name;, &amp;#123;, &amp;#x1F;).
        /// </summary>
        private static bool IsEntityAt(string text, int index, out int length)
        {
            length = 0;

            int i = index + 1;
            if (i >= text.Length)
            {
                return false;
            }

            if (text[i] == '#')
            {
                i++;
                bool hex = i < text.Length && (text[i] == 'x' || text[i] == 'X');
                if (hex)
                {
                    i++;
                }

                int digits = 0;
                while (i < text.Length && (hex ? IsHexDigit(text[i]) : char.IsDigit(text[i])))
                {
                    i++;
                    digits++;
                }

                if (digits == 0)
                {
                    return false;
                }
            }
            else
            {
                if (!IsAsciiLetter(text[i]))
                {
                    return false;
                }

                while (i < text.Length && IsAsciiLetterOrDigit(text[i]))
                {
                    i++;
                }
            }

            if (i >= text.Length || text[i] != ';')
            {
                return false;
            }

            length = i - index + 1;
            return true;
        }

        private static bool IsAsciiLetter(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z');
        }

        private static bool IsAsciiLetterOrDigit(char c)
        {
            return IsAsciiLetter(c) || char.IsDigit(c);
        }

        private static bool IsHexDigit(char c)
        {
            return char.IsDigit(c) || (c >= 'A' && c <= 'F') || (c >= 'a' && c <= 'f');
        }

        /// <summary>
        /// Finds the first descendant element with the given local name,
        /// ignoring namespaces (RCC and RBXGS use different namespaces).
        /// </summary>
        public static XElement FindByLocalName(XDocument doc, string localName)
        {
            if (doc == null || string.IsNullOrEmpty(localName))
            {
                return null;
            }

            return doc.Descendants().FirstOrDefault(x => x.Name.LocalName == localName);
        }

        /// <summary>
        /// Reads a child element value safely; returns <paramref name="fallback"/>
        /// when the parent or child is missing.
        /// </summary>
        public static string ChildValue(XElement parent, string childLocalName, string fallback = "")
        {
            if (parent == null)
            {
                return fallback;
            }

            var child = parent.Elements().FirstOrDefault(x => x.Name.LocalName == childLocalName);
            return child != null ? child.Value : fallback;
        }

        public static string ChildValue(XElement parent, XNamespace ns, string localName, string fallback = "")
        {
            if (parent == null)
            {
                return fallback;
            }

            var child = parent.Element(ns + localName);
            return child != null ? child.Value : fallback;
        }
    }
}
