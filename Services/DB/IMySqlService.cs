using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace RunServer.Services.Database
{
    public interface IMySqlService : IDisposable
    {
        Task<bool> ConnectAsync();
        Task<List<Dictionary<string, object>>> GetAllItemsAsync(string tableName);
        Task<Dictionary<string, object>> GetItemByIdAsync(string tableName, string id);
        Task<bool> SaveUserDataAsync(string tableName, Dictionary<string, object> userData);
        Task<bool> DeleteItemAsync(string tableName, string id);
        Task<string> GetContentIdByClientIdAsync(string? clientId, string? tableName);
        Task<string> GetcontentIdByIdAsync(string id, string table);
    }
}