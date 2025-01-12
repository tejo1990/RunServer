using MySql.Data.MySqlClient;
using Microsoft.Extensions.Logging;
using System.Data;

namespace RunServer.Services.Database
{
    public class MySqlService : IMySqlService
    {
        private readonly MySqlConnection _connection;
        private readonly ILogger<MySqlService> _logger;

        public MySqlService(string connectionString, ILogger<MySqlService> logger)
        {
            _connection = new MySqlConnection(connectionString);
            _logger = logger;
            _logger.LogInformation(connectionString);       
        }

        public async Task<bool> ConnectAsync()
        {
            try
            {
                if (_connection == null)
                {
                    _logger?.LogError("MySqlConnection 객체가 초기화되지 않았습니다.");
                    return false;
                }

                await _connection.OpenAsync();
                _logger?.LogInformation("데이터베이스에 성공적으로 연결되었습니다.");
                return true;
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "데이터베이스 연결 실패");
                return false;
            }
        }

        public async Task<List<Dictionary<string, object>>> GetAllItemsAsync(string tableName)
        {
            var result = new List<Dictionary<string, object>>();
            
            using var cmd = new MySqlCommand($"SELECT * FROM {tableName}", _connection);
            using var reader = await cmd.ExecuteReaderAsync();
            
            while (await reader.ReadAsync())
            {
                var row = new Dictionary<string, object>();
                for (int i = 0; i < reader.FieldCount; i++)
                {
                    row[reader.GetName(i)] = reader.GetValue(i);
                }
                result.Add(row);
            }
            
            return result;
        }

        // ... 나머지 메서드 구현
        
        public void Dispose()
        {
            _connection?.Dispose();
        }

        public async Task<Dictionary<string, object>> GetItemByIdAsync(string tableName, string id)
        {
            var item = new Dictionary<string, object>();
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    await _connection.OpenAsync();
                }

                _logger.LogInformation($"SELECT * FROM {tableName} WHERE ID = {id}");

                using (var cmd = new MySqlCommand($"SELECT * FROM {tableName} WHERE ID = @id", _connection))
                {
                    cmd.Parameters.AddWithValue("@id", id);
                    using (var reader = await cmd.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            for (int i = 0; i < reader.FieldCount; i++)
                            {
                                item[reader.GetName(i)] = reader[i];
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "데이터 가져오기 중 오류 발생");
            }
            return item;
        }

        public async Task<bool> SaveUserDataAsync(string tableName, Dictionary<string, object> userData)
        {
            try
            {
                if (!userData.ContainsKey("id"))
                {
                    _logger?.LogError("데이터에 'id' 키가 없습니다.");
                    return false;
                }

                _logger.LogInformation($"id : {userData["id"]}");

                if (_connection.State != ConnectionState.Open)
                {
                    await _connection.OpenAsync();
                }

                var id = userData["id"].ToString();
                var existingData = await GetItemByIdAsync(tableName, id);

                if (existingData.Count > 0)
                {
                    // UPDATE 쿼리 생성
                    string updates = string.Join(", ", userData.Keys.Select(k => $"{k} = @{k}"));
                    string uquery = $"UPDATE {tableName} SET {updates} WHERE ID = @id";

                    using (var cmd = new MySqlCommand(uquery, _connection))
                    {
                        foreach (var item in userData)
                        {
                            cmd.Parameters.AddWithValue("@" + item.Key, item.Value);
                        }
                        int result = await cmd.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
                else
                {
                    // INSERT 쿼리 실행
                    string columns = string.Join(", ", userData.Keys);
                    string values = string.Join(", ", userData.Keys.Select(k => "@" + k));
                    string query = $"INSERT INTO {tableName} ({columns}) VALUES ({values})";

                    using (var cmd = new MySqlCommand(query, _connection))
                    {
                        foreach (var item in userData)
                        {
                            cmd.Parameters.AddWithValue("@" + item.Key, item.Value);
                        }
                        int result = await cmd.ExecuteNonQueryAsync();
                        return result > 0;
                    }
                }
            }
            catch (MySqlException ex)
            {
                _logger?.LogError(ex, "데이터 저장 중 오류 발생");
                return false;
            }
        }

        public Task<bool> DeleteItemAsync(string tableName, string id)
        {
            throw new NotImplementedException();
        }

        public async Task<string?> GetContentIdByClientIdAsync(string clientId, string tableName)
        {
            try
            {
                if (_connection.State != ConnectionState.Open)
                {
                    await _connection.OpenAsync();
                }

                using var cmd = new MySqlCommand($"SELECT contentId FROM {tableName} WHERE ID = @id", _connection);
                cmd.Parameters.AddWithValue("@id", clientId);

                _logger.LogInformation(cmd.CommandText);

                using var reader = await cmd.ExecuteReaderAsync();
                if (await reader.ReadAsync())
                {
                    var contentId = reader["contentId"]?.ToString();
                    _logger.LogInformation($"{tableName} 내부에 해당 clientId에 대한 contentId: {contentId}");
                    return contentId;
                }
                else
                {
                    _logger.LogInformation($"{tableName} 내부에 해당 clientId가 존재하지 않습니다.");
                    return null;
                }
            }
            catch (Exception ex)
            {
                _logger?.LogError(ex, "clientId로 contentId를 가져오는 중 오류 발생");
                return null;
            }
        }
    }
}