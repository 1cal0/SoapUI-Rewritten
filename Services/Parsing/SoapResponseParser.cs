using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;
using System.Xml.Linq;
using SoapUI.Common;
using SoapUI.Models;
using SoapUI.Models.Responses;

namespace SoapUI.Services.Parsing
{
    /// <summary>
    /// Parses raw SOAP XML into <see cref="ResponseOutcome"/> values.
    /// No WinForms dependencies - the UI decides how to render outcomes.
    /// </summary>
    public sealed class SoapResponseParser
    {
        private readonly ServiceMode _mode;

        public SoapResponseParser(ServiceMode mode)
        {
            _mode = mode;
        }

        public ResponseOutcome Parse(string xml, string soapAction)
        {
            Guard.AgainstNullOrWhiteSpace(xml, nameof(xml));
            Guard.AgainstNullOrWhiteSpace(soapAction, nameof(soapAction));

            XDocument doc;
            try
            {
                doc = XDocument.Parse(xml);
            }
            catch (XmlException ex)
            {
                throw new InvalidOperationException("Failed to parse SOAP XML response: " + ex.Message, ex);
            }

            switch (soapAction)
            {
                case "HelloWorld": return ParseHelloWorld(doc);
                case "GetVersion": return ParseGetVersion(doc);
                case "GetStatus": return ParseGetStatus(doc);
                case "OpenJobEx": return ResponseOutcome.Info("OpenJobEx Response", "Success!");
                case "OpenJob": return ResponseOutcome.Info("OpenJob Response", "Success!");
                case "RenewLease": return ParseRenewLease(doc);
                case "ExecuteEx": return ResponseOutcome.Info("ExecuteEx Response", "Success!");
                case "Execute": return ParseExecute(doc);
                case "CloseJob": return ResponseOutcome.Info("CloseJob Response", "Success!");
                case "DiagEx": return ParseSingleResult(doc, "DiagExResult", "DiagEx Response");
                case "Diag": return ParseSingleResult(doc, "DiagResult", "Diag Response");
                case "GetAllJobsEx": return ParseJobs(doc, "GetAllJobsExResult", "GetAllJobsEx Response");
                case "GetAllJobs": return ParseJobs(doc, "GetAllJobsResult", "GetAllJobs Response");
                case "CloseExpiredJobs": return ParseJobsClosed(doc, "CloseExpiredJobsResult", "CloseExpiredJobs Response");
                case "CloseAllJobs": return ParseJobsClosed(doc, "CloseAllJobsResult", "CloseAllJobs Response");
                case "OpenEnvironment": return ParseOpenEnvironment(doc);
                case "CloseEnvironment": return ResponseOutcome.Silent(new[] { "Closed environment" });
                case "CloseOrphanedEnvironments": return ParseClosedCount(doc, "CloseOrphanedEnvironments", "orphaned environments");
                case "CloseAllEnvironments": return ResponseOutcome.Silent(new[] { "Closed all environments" });
                case "GetAllEnvironments": return ParseGetAllEnvironments(doc);
                case "GetStandardOutMessages": return ParseStandardOutMessages(doc);
                case "Update": return ResponseOutcome.Silent(new[] { "Update request sent." });
                default:
                    throw new InvalidOperationException("Unsupported SOAP action '" + soapAction + "'.");
            }
        }

        private bool IsRbxgs
        {
            get { return _mode == ServiceMode.Rbxgs; }
        }

        private ResponseOutcome ParseHelloWorld(XDocument doc)
        {
            if (IsRbxgs)
            {
                var node = XmlHelper.FindByLocalName(doc, "HelloWorld");
                return ResponseOutcome.Info("HelloWorld Response", FirstNodeText(node));
            }

            var result = XmlHelper.FindByLocalName(doc, "HelloWorldResult");
            return ResponseOutcome.Info("HelloWorld Response", FirstNodeText(result));
        }

        private ResponseOutcome ParseGetVersion(XDocument doc)
        {
            if (IsRbxgs)
            {
                var node = XmlHelper.FindByLocalName(doc, "GetVersion");
                string version = node != null && node.Element("return") != null
                    ? node.Element("return").Value
                    : FirstNodeText(node);
                return new ResponseOutcome("GetVersion Response", version, new[] { "Version: " + version });
            }

            var result = XmlHelper.FindByLocalName(doc, "GetVersionResult");
            return ResponseOutcome.Info("GetVersion Response", FirstNodeText(result));
        }

        private ResponseOutcome ParseGetStatus(XDocument doc)
        {
            if (IsRbxgs)
            {
                var node = XmlHelper.FindByLocalName(doc, "GetStatus");
                var ret = node != null ? node.Element("return") : null;
                string version = ret != null ? XmlHelper.ChildValue(ret, "version") : string.Empty;
                string count = ret != null ? XmlHelper.ChildValue(ret, "environmentCount") : string.Empty;

                var lines = new List<string> { "Version: " + version, "Environment count: " + count };
                return new ResponseOutcome("GetStatus Response", "Version: " + version + "\r\nEnvironment count: " + count, lines);
            }

            var result = XmlHelper.FindByLocalName(doc, "GetStatusResult");
            if (result == null)
            {
                return ResponseOutcome.Info("GetStatus Response", string.Empty);
            }

            XNamespace ns = result.Name.Namespace;
            string rccVersion = XmlHelper.ChildValue(result, ns, "version");
            int envCount = 0;
            int.TryParse(XmlHelper.ChildValue(result, ns, "environmentCount", "0"), out envCount);

            var status = new ServiceStatus(rccVersion, envCount);
            return ResponseOutcome.Info("GetStatus Response", status.ToString());
        }

        private ResponseOutcome ParseExecute(XDocument doc)
        {
            if (!IsRbxgs)
            {
                return ResponseOutcome.Info("Execute Response", "Success!");
            }

            var node = XmlHelper.FindByLocalName(doc, "Execute");
            var ret = node != null ? node.Element("return") : null;
            if (ret == null)
            {
                return ResponseOutcome.Silent(new[] { "Execute returned an empty payload." });
            }

            var lines = new List<string>();
            lines.Add("Return value count: " + XmlHelper.ChildValue(ret, "count", "0"));

            var items = ret.Element("items");
            if (items != null)
            {
                foreach (var item in items.Elements())
                {
                    if (item.Name.LocalName == "count")
                    {
                        continue;
                    }

                    string type = XmlHelper.ChildValue(item, "type");
                    string value = XmlHelper.ChildValue(item, "value");
                    lines.Add(LuaType.FriendlyPrefix(type) + value);
                }
            }

            return ResponseOutcome.Silent(lines);
        }

        private ResponseOutcome ParseOpenEnvironment(XDocument doc)
        {
            var node = XmlHelper.FindByLocalName(doc, "OpenEnvironment");
            var ret = node != null ? node.Element("return") : null;
            string id = ret != null ? ret.Value : string.Empty;

            return new ResponseOutcome("Open Environment", "Success",
                new[] { "Opened Environment " + id });
        }

        private ResponseOutcome ParseClosedCount(XDocument doc, string localName, string noun)
        {
            var node = XmlHelper.FindByLocalName(doc, localName);
            var ret = node != null ? node.Element("return") : null;
            string count = ret != null ? ret.Value : "0";

            return ResponseOutcome.Silent(new[] { "Closed " + count + " " + noun });
        }

        private ResponseOutcome ParseGetAllEnvironments(XDocument doc)
        {
            var node = XmlHelper.FindByLocalName(doc, "GetAllEnvironments");
            var ret = node != null ? node.Element("return") : null;
            if (ret == null)
            {
                return ResponseOutcome.Silent(new[] { "Environment count: 0" });
            }

            var lines = new List<string>
            {
                "Environment count: " + XmlHelper.ChildValue(ret, "count", "0")
            };

            var items = ret.Element("items");
            if (items != null)
            {
                foreach (var item in items.Elements())
                {
                    lines.Add(item.Value);
                }
            }

            return ResponseOutcome.Silent(lines);
        }

        private ResponseOutcome ParseStandardOutMessages(XDocument doc)
        {
            var node = XmlHelper.FindByLocalName(doc, "GetStandardOutMessages");
            var ret = node != null ? node.Element("return") : null;
            if (ret == null)
            {
                return ResponseOutcome.Silent(new[] { "Message count: 0" });
            }

            var lines = new List<string>
            {
                "Message count: " + XmlHelper.ChildValue(ret, "count", "0")
            };

            var items = ret.Element("items");
            if (items != null)
            {
                foreach (var item in items.Elements())
                {
                    if (item.Name.LocalName == "count")
                    {
                        continue;
                    }

                    string prefix = MessagePrefix(XmlHelper.ChildValue(item, "type"));
                    string text = XmlHelper.ChildValue(item, "text");
                    string timestamp = FormatTimestamp(XmlHelper.ChildValue(item, "time"));
                    lines.Add(timestamp + " - " + prefix + text);
                }
            }

            return ResponseOutcome.Silent(lines);
        }

        private ResponseOutcome ParseRenewLease(XDocument doc)
        {
            var result = XmlHelper.FindByLocalName(doc, "RenewLeaseResult");
            string value = result != null ? result.Value : string.Empty;

            if (string.IsNullOrWhiteSpace(value))
            {
                return ResponseOutcome.Info("RenewLease Response", "No renewal result was returned.");
            }

            return ResponseOutcome.Info("RenewLease Response", "RenewLease result: " + value);
        }

        private ResponseOutcome ParseJobs(XDocument doc, string localName, string title)
        {
            var resultNodes = doc.Descendants()
                .Where(x => x.Name.LocalName == localName)
                .ToList();

            if (resultNodes.Count == 0)
            {
                return ResponseOutcome.Info(title, "Jobs are empty.");
            }

            int jobCount = localName == "GetAllJobsExResult"
                ? resultNodes.Sum(x => x.Descendants().Count(y => y.Name.LocalName == "Job"))
                : resultNodes.Count;

            if (jobCount == 0)
            {
                return ResponseOutcome.Info(title, "Jobs are empty.");
            }

            return ResponseOutcome.Info(title, "Jobs found: " + jobCount + ".");
        }

        private ResponseOutcome ParseSingleResult(XDocument doc, string localName, string title)
        {
            var node = XmlHelper.FindByLocalName(doc, localName);
            return ResponseOutcome.Info(title, node != null ? node.ToString() : string.Empty);
        }

        private ResponseOutcome ParseJobsClosed(XDocument doc, string localName, string title)
        {
            var node = XmlHelper.FindByLocalName(doc, localName);
            string count = node != null ? FirstNodeText(node) : "0";
            return ResponseOutcome.Info(title, "Jobs closed: " + count);
        }

        private static string MessagePrefix(string type)
        {
            switch (type)
            {
                case "MESSAGE_OUTPUT": return "[OUTPUT] ";
                case "MESSAGE_INFO": return "[INFO] ";
                case "MESSAGE_WARNING": return "[WARNING] ";
                case "MESSAGE_ERROR": return "[ERROR] ";
                default: return "[UNKNOWN] ";
            }
        }

        private static string FormatTimestamp(string epochSeconds)
        {
            long seconds;
            if (UnixTime.TryParse(epochSeconds, out seconds))
            {
                return UnixTime.FromSeconds(seconds).ToString();
            }

            return epochSeconds;
        }

        private static string FirstNodeText(XElement element)
        {
            if (element == null || element.FirstNode == null)
            {
                return element != null ? element.Value : string.Empty;
            }

            return element.FirstNode.ToString();
        }
    }
}
