// RunServer/Models/ResponseModel.cs
using System.Collections.Generic;

namespace RunServer.Models
{
    public class ResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; }
        public Dictionary<string, object> Data { get; set; }

        public ResponseModel()
        {
            Data = new Dictionary<string, object>();
        }
    }
}