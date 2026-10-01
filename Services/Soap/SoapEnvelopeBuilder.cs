using System;
using System.Text;
using SoapUI.Common;
using SoapUI.Models;

namespace SoapUI.Services.Soap
{
    /// <summary>
    /// Builds SOAP envelopes and typed body fragments for RCC / RBXGS.
    /// Pure string templating with XML escaping - no network I/O here.
    /// </summary>
    internal static class SoapEnvelopeBuilder
    {
        public static string BuildRccEnvelope(string baseUrl, string body)
        {
            Guard.AgainstNullOrWhiteSpace(baseUrl, nameof(baseUrl));

            var sb = new StringBuilder();
            sb.Append("<?xml version=\"1.0\" encoding=\"UTF-8\"?>\r\n");
            sb.Append("<SOAP-ENV:Envelope xmlns:SOAP-ENV=\"http://schemas.xmlsoap.org/soap/envelope/\" ");
            sb.Append("xmlns:SOAP-ENC=\"http://schemas.xmlsoap.org/soap/encoding/\" ");
            sb.Append("xmlns:xsi=\"http://www.w3.org/2001/XMLSchema-instance\" ");
            sb.Append("xmlns:xsd=\"http://www.w3.org/2001/XMLSchema\" ");
            sb.Append("xmlns:ns2=\"http://").Append(baseUrl).Append("/RCCServiceSoap\" ");
            sb.Append("xmlns:ns1=\"http://").Append(baseUrl).Append("/\" ");
            sb.Append("xmlns:ns3=\"http://").Append(baseUrl).Append("/RCCServiceSoap12\">\r\n");
            sb.Append("\t<SOAP-ENV:Body>").Append(body).Append("\t</SOAP-ENV:Body>\r\n");
            sb.Append("</SOAP-ENV:Envelope>");
            return sb.ToString();
        }

        public static string BuildRbxgsEnvelope(string body)
        {
            return "<soap:Envelope xmlns:soap=\"http://schemas.xmlsoap.org/soap/envelope/\" "
                + "xmlns:soapenc=\"http://schemas.xmlsoap.org/soap/encoding/\" "
                + "xmlns:roblox=\"urn:Roblox\">\r\n\t<soap:Body>\r\n\t\t"
                + body + "\r\n\t</soap:Body>\r\n</soap:Envelope>";
        }

        public static string BuildJobFragment(Job job)
        {
            Guard.AgainstNull(job, nameof(job));

            return "<ns1:job>\r\n"
                + "\t\t\t<ns1:id>" + XmlHelper.Escape(job.Id) + "</ns1:id>\r\n"
                + "\t\t\t<ns1:expirationInSeconds>" + job.ExpirationInSeconds + "</ns1:expirationInSeconds>\r\n"
                + "\t\t\t<ns1:category>" + job.Category + "</ns1:category>\r\n"
                + "\t\t\t<ns1:cores>" + job.Cores + "</ns1:cores></ns1:job>";
        }

        public static string BuildArgumentFragment(LuaArgument argument)
        {
            Guard.AgainstNull(argument, nameof(argument));

            string type = XmlHelper.Escape(argument.Type);
            string value = XmlHelper.Escape(argument.ValueAsString);

            return "<ns1:LuaValue><ns1:type>" + type + "</ns1:type><ns1:value>" + value + "</ns1:value></ns1:LuaValue>";
        }

        public static string BuildScriptFragment(Script script, bool rbxgs)
        {
            Guard.AgainstNull(script, nameof(script));

            var args = new StringBuilder();
            foreach (var argument in script.Arguments)
            {
                args.Append(BuildArgumentFragment(argument));
            }

            if (rbxgs)
            {
                return "<environmentID>" + XmlHelper.Escape(script.EnvironmentId) + "</environmentID>"
                    + "<script>" + XmlHelper.EscapeScript(script.Content) + "</script>"
                    + "<arguments><count>" + script.Arguments.Count + "</count>"
                    + "<items soapenc:arrayType=\"roblox:LuaValue1[0]\">" + args + "</items></arguments>"
                    + "<name>" + XmlHelper.Escape(script.Name) + "</name>";
            }

            var scriptElement = new StringBuilder();
            scriptElement.Append("<ns1:script>\r\n");
            scriptElement.Append("\t\t\t<ns1:name>").Append(XmlHelper.Escape(script.Name)).Append("</ns1:name>\r\n");
            scriptElement.Append("\t\t\t<ns1:script>").Append(XmlHelper.EscapeScript(script.Content)).Append("</ns1:script>\r\n");

            if (script.Arguments.Count > 0)
            {
                scriptElement.Append("\t\t\t<ns1:arguments>").Append(args).Append("</ns1:arguments>\r\n");
            }

            scriptElement.Append("\t\t</ns1:script>");
            return scriptElement.ToString();
        }
    }
}
