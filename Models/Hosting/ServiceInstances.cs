using System.Diagnostics;

namespace SoapUI.Models.Hosting
{
    /// <summary>
    /// A locally tracked RCC service process.
    /// </summary>
    public sealed class RccServiceInstance
    {
        public RccServiceInstance(Process serviceProcess, int servicePort, string jobId)
        {
            ServiceProcess = serviceProcess;
            ServicePort = servicePort;
            JobId = jobId;
        }

        public Process ServiceProcess { get; }

        public int ServicePort { get; }

        public string JobId { get; }
    }

    /// <summary>
    /// A locally tracked game-server process.
    /// </summary>
    public sealed class GameServerInstance
    {
        public GameServerInstance(Process gameProcess, int gamePort, int placeId, int servicePort, string jobId)
        {
            GameProcess = gameProcess;
            GamePort = gamePort;
            PlaceId = placeId;
            ServicePort = servicePort;
            JobId = jobId;
        }

        public Process GameProcess { get; }

        public int GamePort { get; }

        public int PlaceId { get; }

        public int ServicePort { get; }

        public string JobId { get; }
    }
}
