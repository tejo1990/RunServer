public class AppSettings
{
    public string IP { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 7775;
    
    public string ConnectionString { get; set; } = string.Empty;

    public string Server { get; set; } = string.Empty;
    public string Database { get; set; } = string.Empty;
    public string Uid { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public string Table { get; set; } = string.Empty;
}
