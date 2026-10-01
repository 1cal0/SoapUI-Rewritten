using SoapUI.Common;
using SoapUI.Models;

namespace SoapUI.Services.Soap
{
    /// <summary>
    /// Typed client for the legacy RBXGS (urn:Roblox) endpoint set.
    /// </summary>
    public sealed class RbxgsServiceClient
    {
        private readonly ServiceEndpoint _endpoint;
        private readonly SoapTransport _transport;

        public RbxgsServiceClient(ServiceEndpoint endpoint, SoapTransport transport)
        {
            Guard.AgainstNull(endpoint, nameof(endpoint));
            Guard.AgainstNull(transport, nameof(transport));

            _endpoint = endpoint;
            _transport = transport;
        }

        public ServiceEndpoint Endpoint
        {
            get { return _endpoint; }
        }

        public string HelloWorld()
        {
            return _transport.Send(_endpoint, "HelloWorld", "<roblox:HelloWorld/>");
        }

        public string GetVersion()
        {
            return _transport.Send(_endpoint, "GetVersion", "<roblox:GetVersion/>");
        }

        public string GetStatus()
        {
            return _transport.Send(_endpoint, "GetStatus", "<roblox:GetStatus/>");
        }

        public string Execute(Script script)
        {
            Guard.AgainstNull(script, nameof(script));

            string body = "<roblox:Execute>"
                + SoapEnvelopeBuilder.BuildScriptFragment(script, true)
                + "</roblox:Execute>";
            return _transport.Send(_endpoint, "Execute", body);
        }

        public string GetStandardOutMessages(int maxCount)
        {
            return _transport.Send(_endpoint, "GetStandardOutMessages",
                "<roblox:GetStandardOutMessages>\r\n\t<maxCount>" + maxCount + "</maxCount>\r\n</roblox:GetStandardOutMessages>");
        }

        public string GetAllEnvironments()
        {
            return _transport.Send(_endpoint, "GetAllEnvironments", "<roblox:GetAllEnvironments/>");
        }

        public string OpenEnvironment()
        {
            return _transport.Send(_endpoint, "OpenEnvironment", "<roblox:OpenEnvironment/>");
        }

        public string CloseEnvironment(string environmentId)
        {
            return _transport.Send(_endpoint, "CloseEnvironment",
                "<roblox:CloseEnvironment><environmentID>" + XmlHelper.Escape(environmentId ?? "RBX0") + "</environmentID></roblox:CloseEnvironment>");
        }

        public string CloseAllEnvironments()
        {
            return _transport.Send(_endpoint, "CloseAllEnvironments", "<roblox:CloseAllEnvironments/>");
        }

        public string CloseOrphanedEnvironments()
        {
            return _transport.Send(_endpoint, "CloseOrphanedEnvironments", "<roblox:CloseOrphanedEnvironments/>");
        }

        public string Update(string updateUrl)
        {
            return _transport.Send(_endpoint, "Update",
                "<roblox:Update><url>" + XmlHelper.Escape(updateUrl ?? string.Empty) + "</url></roblox:Update>");
        }
    }
}
