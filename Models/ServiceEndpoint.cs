using SoapUI.Common;

namespace SoapUI.Models
{
    /// <summary>
    /// Connection details for a target game-server instance.
    /// Immutable value object built from the settings panel.
    /// </summary>
    public sealed class ServiceEndpoint
    {
        public ServiceEndpoint(string ip, int port, string baseUrl, ServiceMode mode)
        {
            Guard.AgainstNullOrWhiteSpace(ip, nameof(ip));
            Guard.AgainstNullOrWhiteSpace(baseUrl, nameof(baseUrl));

            Ip = ip.Trim();
            Port = port;
            BaseUrl = baseUrl.Trim();
            Mode = mode;
        }

        public string Ip { get; }

        public int Port { get; }

        public string BaseUrl { get; }

        public ServiceMode Mode { get; }

        public bool IsRbxgs
        {
            get { return Mode == ServiceMode.Rbxgs; }
        }

        /// <summary>
        /// Builds the RCC HTTP endpoint, e.g. http://127.0.0.1:64989
        /// </summary>
        public string ToRccUrl()
        {
            return "http://" + Ip + ":" + Port;
        }

        /// <summary>
        /// Builds the legacy RBXGS endpoint.
        /// </summary>
        public string ToRbxgsUrl()
        {
            return "http://" + Ip + "/RBXGS/WebService.dll";
        }

        public string ToUrl()
        {
            return IsRbxgs ? ToRbxgsUrl() : ToRccUrl();
        }

        public override string ToString()
        {
            return ToUrl() + " (" + Mode + ")";
        }
    }
}
