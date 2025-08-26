using System.Collections.Concurrent;
using System.Net.Sockets;
using HlsService.Models;

namespace HlsService.Services
{
    /// <summary>
    /// 客户端连接管理器
    /// 负责管理所有客户端连接的生命周期和状态
    /// </summary>
    public class ClientConnectionManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, ClientConnection> _connections;
        private readonly object _lockObject = new object();
        private long _totalConnections = 0;
        private bool _disposed = false;

        public ClientConnectionManager()
        {
            _connections = new ConcurrentDictionary<string, ClientConnection>();
        }

        /// <summary>
        /// 活跃连接数
        /// </summary>
        public int ActiveConnections => _connections.Count;

        /// <summary>
        /// 总连接数（历史累计）
        /// </summary>
        public long TotalConnections => _totalConnections;

        /// <summary>
        /// 添加新的客户端连接
        /// </summary>
        public string AddConnection(TcpClient tcpClient)
        {
            var clientId = Guid.NewGuid().ToString("N")[..8];
            var connection = new ClientConnection(clientId, tcpClient);

            if (_connections.TryAdd(clientId, connection))
            {
                Interlocked.Increment(ref _totalConnections);
                Console.WriteLine($"[连接管理] 新连接已添加: {clientId} ({tcpClient.Client.RemoteEndPoint})");
                return clientId;
            }

            throw new InvalidOperationException($"Failed to add connection: {clientId}");
        }

        /// <summary>
        /// 移除客户端连接
        /// </summary>
        public bool RemoveConnection(string clientId)
        {
            if (_connections.TryRemove(clientId, out var connection))
            {
                connection.Dispose();
                Console.WriteLine($"[连接管理] 连接已移除: {clientId}");
                return true;
            }
            return false;
        }

        /// <summary>
        /// 获取客户端连接
        /// </summary>
        public ClientConnection? GetConnection(string clientId)
        {
            _connections.TryGetValue(clientId, out var connection);
            return connection;
        }

        /// <summary>
        /// 获取所有活跃连接信息
        /// </summary>
        public IEnumerable<ClientInfo> GetAllConnectionInfo()
        {
            return _connections.Values
                .Where(c => c.IsConnected)
                .Select(c => new ClientInfo
                {
                    ClientId = c.ClientId,
                    RemoteEndpoint = c.RemoteEndpoint,
                    ConnectedTime = c.ConnectedTime,
                    LastActivity = c.LastActivity,
                    MessageCount = c.MessageCount,
                    IsActive = c.IsConnected
                })
                .ToList();
        }

        /// <summary>
        /// 清理断开的连接
        /// </summary>
        public int CleanupDisconnectedClients()
        {
            var disconnectedClients = _connections.Values
                .Where(c => !c.IsConnected)
                .ToList();

            int cleanedCount = 0;
            foreach (var client in disconnectedClients)
            {
                if (RemoveConnection(client.ClientId))
                {
                    cleanedCount++;
                }
            }

            if (cleanedCount > 0)
            {
                Console.WriteLine($"[连接管理] 清理了 {cleanedCount} 个断开的连接");
            }

            return cleanedCount;
        }

        /// <summary>
        /// 关闭所有连接
        /// </summary>
        public async Task CloseAllConnections()
        {
            var connections = _connections.Values.ToList();
            
            foreach (var connection in connections)
            {
                try
                {
                    await connection.CloseAsync();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[连接管理] 关闭连接 {connection.ClientId} 时出错: {ex.Message}");
                }
            }

            _connections.Clear();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                CloseAllConnections().Wait(5000); // 5秒超时
                _disposed = true;
            }
        }
    }

    /// <summary>
    /// 单个客户端连接封装
    /// </summary>
    public class ClientConnection : IDisposable
    {
        private readonly TcpClient _tcpClient;
        private readonly NetworkStream _stream;
        private long _messageCount = 0;

        public ClientConnection(string clientId, TcpClient tcpClient)
        {
            ClientId = clientId;
            _tcpClient = tcpClient;
            _stream = tcpClient.GetStream();
            ConnectedTime = DateTime.UtcNow;
            LastActivity = DateTime.UtcNow;
            RemoteEndpoint = tcpClient.Client.RemoteEndPoint?.ToString() ?? "Unknown";
        }

        public string ClientId { get; }
        public DateTime ConnectedTime { get; }
        public DateTime LastActivity { get; private set; }
        public string RemoteEndpoint { get; }
        public long MessageCount => _messageCount;
        public bool IsConnected => _tcpClient.Connected;

        /// <summary>
        /// 更新最后活动时间和消息计数
        /// </summary>
        public void UpdateActivity()
        {
            LastActivity = DateTime.UtcNow;
            Interlocked.Increment(ref _messageCount);
        }

        /// <summary>
        /// 获取网络流
        /// </summary>
        public NetworkStream GetStream() => _stream;

        /// <summary>
        /// 异步关闭连接
        /// </summary>
        public async Task CloseAsync()
        {
            try
            {
                if (_tcpClient.Connected)
                {
                    _stream?.Close();
                    _tcpClient?.Close();
                }
                await Task.CompletedTask;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[连接] 关闭连接 {ClientId} 时出错: {ex.Message}");
            }
        }

        public void Dispose()
        {
            CloseAsync().Wait(1000);
            _stream?.Dispose();
            _tcpClient?.Dispose();
        }
    }
}