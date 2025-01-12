using Microsoft.Extensions.Configuration;

public static class GlobalSettings
{
    public static string IP { get; private set; }
    public static int Port { get; private set; }
    public static string ConnectionString { get; private set; }
    public static string Server { get; private set; }
    public static string Database { get; private set; }
    public static string Uid { get; private set; }
    public static string Password { get; private set; }
    public static string Table { get; private set; }

    public static void Initialize(IConfiguration configuration)
    {
        IP = configuration["ServerSettings:IP"];
        Port = int.Parse(configuration["ServerSettings:Port"]);
        Server = configuration["ServerSettings:Server"];
        Database = configuration["ServerSettings:Database"];
        Uid = configuration["ServerSettings:UID"];
        Password = configuration["ServerSettings:Password"];
        Table = configuration["ServerSettings:Table"];
        ConnectionString = $"Server={Server};Database={Database};Uid={Uid};Pwd={Password};";
    }
} 