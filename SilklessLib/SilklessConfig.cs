namespace SilklessLib;

public static class SilklessConfig
{
    public enum EConnectionType { SteamP2P, Standalone, Debug }

    // misc
    public static bool PrintDebugOutput;

    // connection
    public static EConnectionType ConnectionType;
    public static float ConnectionTimeout;

    // standalone
    public static string StandaloneIP;
    public static int StandalonePort;
    public static string StandaloneUsername;

    public static string Version;
}
