namespace SoapUI.Common
{
    /// <summary>
    /// Application-wide defaults and well-known values.
    /// Keeps magic strings/numbers out of the UI and service clients.
    /// </summary>
    internal static class AppConstants
    {
        public const string DefaultBaseUrl = "roblox.com";
        public const string DefaultIp = "127.0.0.1";
        public const int DefaultPort = 64989;

        public const string RepositoryUrl = "https://github.com/p0s0/SoapUI";
        public const string DefaultScriptText = @"print('Hello, world!')";

        public static readonly string[] RccSoapActions =
        {
            "HelloWorld",
            "GetVersion",
            "GetStatus",
            "OpenJobEx",
            "OpenJob",
            "RenewLease",
            "ExecuteEx",
            "Execute",
            "CloseJob",
            "DiagEx",
            "Diag",
            "GetAllJobsEx",
            "GetAllJobs",
            "CloseExpiredJobs",
            "CloseAllJobs"
        };

        public static readonly string[] RbxgsSoapActions =
        {
            "GetStandardOutMessages",
            "GetAllEnvironments",
            "Execute",
            "CloseAllEnvironments",
            "CloseOrphanedEnvironments",
            "CloseEnvironment",
            "OpenEnvironment",
            "Update",
            "GetStatus",
            "GetVersion",
            "HelloWorld"
        };

        public static readonly string[] SupportedLuaTypes =
        {
            "LUA_TNIL",
            "LUA_TBOOLEAN",
            "LUA_TNUMBER",
            "LUA_TSTRING",
            "LUA_TTABLE"
        };
    }
}
