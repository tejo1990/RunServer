using System.Collections.Generic;

namespace RunServer.Models
{
    public class RequestModel
    {
        public string Type { get; set; }
        public Dictionary<string, object> Data { get; set; }
    }
}