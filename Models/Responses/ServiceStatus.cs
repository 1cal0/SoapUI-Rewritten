namespace SoapUI.Models.Responses
{
    /// <summary>
    /// Parsed GetStatus payload shared by RCC and RBXGS.
    /// </summary>
    public sealed class ServiceStatus
    {
        public ServiceStatus(string version, int environmentCount)
        {
            Version = version ?? string.Empty;
            EnvironmentCount = environmentCount;
        }

        public string Version { get; }

        public int EnvironmentCount { get; }

        public override string ToString()
        {
            return "Version: " + Version + "\r\nEnvironment count: " + EnvironmentCount;
        }
    }
}
