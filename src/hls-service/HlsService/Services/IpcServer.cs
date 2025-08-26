using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using HlsService.Models;

namespace HlsService.Services
{
    /// <summary>
    /// IPC通信服务器
    /// 负责TCP Socket服务器的完整功能实现
    /// </summary>
    public class IpcServer : IDisposable
    {
        private readonly ServerConfiguration _config;
        private readonly ClientConnectionManager _connectionManager;
        private readonly MessageProcessor _messageProcessor;
        private readonly ServerStatistics _statistics;
        private readonly DeviceManager _deviceManager;
        
        private TcpListener? _tcpListener;
        private CancellationTokenSource? _cancellationTokenSource;
        private Task? _serverTask;
        private Task? _cleanupTask;
        private bool _isRunning = false;
        private bool _disposed = false;

        public IpcServer(ServerConfiguration config)
        {
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _connectionManager = new ClientConnectionManager();
            _deviceManager = new DeviceManager();
            _statistics = new ServerStatistics(_connectionManager);
            _messageProcessor = new MessageProcessor(_config, _statistics, _deviceManager);
        }

        /// <summary>
        /// 服务器是否正在运行
        /// </summary>
        public bool IsRunning => _isRunning;

        /// <summary>
        /// 获取服务器统计信息
        /// </summary>
        public ServerStatistics Statistics => _statistics;

        /// <summary>
        /// 启动服务器
        /// </summary>
        public async Task StartAsync()
        {
            if (_isRunning)
            {
                throw new InvalidOperationException("Server is already running");
            }

            try
            {
                var ipAddress = IPAddress.Parse(_config.Host);
                _tcpListener = new TcpListener(ipAddress, _config.Port);
                _tcpListener.Start();

                _cancellationTokenSource = new CancellationTokenSource();
                _isRunning = true;

                Console.WriteLine($"[IPC服务器] 已启动监听: {_config.Host}:{_config.Port}");
                Console.WriteLine($"[IPC服务器] 最大连接数: {_config.MaxConnections}");
                Console.WriteLine($"[IPC服务器] 协议版本: {_config.ProtocolVersion}");

                // 启动主服务器任务
                _serverTask = Task.Run(async () => await HandleClientConnectionsAsync(_cancellationTokenSource.Token));

                // 启动清理任务
                _cleanupTask = Task.Run(async () => await PeriodicCleanupAsync(_cancellationTokenSource.Token));

                await Task.Delay(100); // 确保服务器启动完成
                Console.WriteLine("[IPC服务器] 服务器启动完成");
            }
            catch (Exception ex)
            {
                _isRunning = false;
                Console.WriteLine($"[IPC服务器] 启动失败: {ex.Message}");
                throw;
            }
        }

        /// <summary>
        /// 停止服务器
        /// </summary>
        public async Task StopAsync()
        {
            if (!_isRunning)
            {
                return;
            }

            Console.WriteLine("[IPC服务器] 正在停止服务器...");
            _isRunning = false;

            try
            {
                // 取消所有任务
                _cancellationTokenSource?.Cancel();

                // 停止监听器
                _tcpListener?.Stop();

                // 等待服务器任务完成
                if (_serverTask != null)
                {
                    await _serverTask.WaitAsync(TimeSpan.FromSeconds(5));
                }

                if (_cleanupTask != null)
                {
                    await _cleanupTask.WaitAsync(TimeSpan.FromSeconds(2));
                }

                // 关闭所有客户端连接
                await _connectionManager.CloseAllConnections();

                Console.WriteLine("[IPC服务器] 服务器已停止");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IPC服务器] 停止服务器时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 处理客户端连接的主循环
        /// </summary>
        private async Task HandleClientConnectionsAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && _tcpListener != null && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    // 检查连接数限制
                    if (_statistics.ActiveConnections >= _config.MaxConnections)
                    {
                        Console.WriteLine($"[IPC服务器] 达到最大连接数限制: {_config.MaxConnections}");
                        await Task.Delay(1000, cancellationToken);
                        continue;
                    }

                    var tcpClient = await _tcpListener.AcceptTcpClientAsync();
                    
                    // 配置客户端参数
                    tcpClient.ReceiveTimeout = _config.ReadTimeoutMs;
                    tcpClient.SendTimeout = _config.WriteTimeoutMs;

                    // 添加到连接管理器
                    var clientId = _connectionManager.AddConnection(tcpClient);

                    // 为每个客户端创建独立的处理任务
                    _ = Task.Run(async () => await HandleClientAsync(clientId, cancellationToken));

                    if (_config.EnableVerboseLogging)
                    {
                        Console.WriteLine($"[IPC服务器] 新连接已接受: {clientId} (总连接数: {_statistics.ActiveConnections})");
                    }
                }
                catch (ObjectDisposedException)
                {
                    // 服务器正在关闭，正常退出
                    break;
                }
                catch (Exception ex) when (!cancellationToken.IsCancellationRequested)
                {
                    Console.WriteLine($"[IPC服务器] 接受连接时出错: {ex.Message}");
                    await Task.Delay(1000, cancellationToken);
                }
            }
        }

        /// <summary>
        /// 处理单个客户端连接
        /// </summary>
        private async Task HandleClientAsync(string clientId, CancellationToken cancellationToken)
        {
            var connection = _connectionManager.GetConnection(clientId);
            if (connection == null)
            {
                Console.WriteLine($"[IPC服务器] 找不到连接: {clientId}");
                return;
            }

            try
            {
                var stream = connection.GetStream();
                var buffer = new byte[_config.BufferSize];

                while (connection.IsConnected && !cancellationToken.IsCancellationRequested)
                {
                    var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);
                    if (bytesRead == 0)
                    {
                        // 客户端正常关闭连接
                        break;
                    }

                    var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    
                    if (_config.EnableVerboseLogging)
                    {
                        Console.WriteLine($"[IPC服务器] 收到消息 {clientId}: {message}");
                    }

                    // 处理消息
                    var response = await _messageProcessor.ProcessMessageAsync(message, connection);
                    
                    // 发送响应
                    var responseJson = JsonSerializer.Serialize(response, new JsonSerializerOptions
                    {
                        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                        WriteIndented = !_config.EnableVerboseLogging // 生产环境使用紧凑格式
                    });

                    var responseBytes = Encoding.UTF8.GetBytes(responseJson);
                    await stream.WriteAsync(responseBytes, 0, responseBytes.Length, cancellationToken);

                    if (_config.EnableVerboseLogging)
                    {
                        Console.WriteLine($"[IPC服务器] 发送响应 {clientId}: {responseJson}");
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // 服务器正在关闭
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[IPC服务器] 处理客户端 {clientId} 时出错: {ex.Message}");
            }
            finally
            {
                // 清理连接
                _connectionManager.RemoveConnection(clientId);
                
                if (_config.EnableVerboseLogging)
                {
                    Console.WriteLine($"[IPC服务器] 客户端连接已关闭: {clientId}");
                }
            }
        }

        /// <summary>
        /// 定期清理断开的连接
        /// </summary>
        private async Task PeriodicCleanupAsync(CancellationToken cancellationToken)
        {
            while (_isRunning && !cancellationToken.IsCancellationRequested)
            {
                try
                {
                    var cleanedCount = _connectionManager.CleanupDisconnectedClients();
                    
                    if (cleanedCount > 0 && _config.EnableVerboseLogging)
                    {
                        Console.WriteLine($"[IPC服务器] 定期清理完成，清理了 {cleanedCount} 个断开的连接");
                    }

                    await Task.Delay(TimeSpan.FromSeconds(30), cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    // 正常退出
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[IPC服务器] 定期清理时出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 获取所有连接信息
        /// </summary>
        public IEnumerable<ClientInfo> GetConnectionInfo()
        {
            return _connectionManager.GetAllConnectionInfo();
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                StopAsync().Wait(5000);
                _deviceManager?.Dispose();
                _connectionManager?.Dispose();
                _tcpListener?.Stop();
                _cancellationTokenSource?.Cancel();
                _cancellationTokenSource?.Dispose();
                _disposed = true;
            }
        }
    }
}