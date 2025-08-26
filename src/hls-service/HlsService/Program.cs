using System;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using HslCommunication.ModBus;
using HslCommunication;

namespace HlsService
{
    /// <summary>
    /// HLS-Communication服务主程序
    /// 提供TCP Socket服务器，支持与Node-RED的IPC通信
    /// </summary>
    class Program
    {
        private static TcpListener? _server;
        private static bool _isRunning = false;
        
        static async Task Main(string[] args)
        {
            Console.WriteLine("HLS-Communication服务启动中...");
            
            // 显示版本信息
            Console.WriteLine($".NET版本: {Environment.Version}");
            Console.WriteLine($"HslCommunication库已加载");
            
            // 启动TCP服务器
            await StartServer();
            
            // 检查是否是后台运行模式
            if (Environment.UserInteractive && !Console.IsInputRedirected)
            {
                Console.WriteLine("按任意键退出服务...");
                Console.ReadKey();
            }
            else
            {
                Console.WriteLine("服务已启动，使用Ctrl+C退出...");
                // 等待取消信号
                var cancellationToken = new CancellationTokenSource();
                Console.CancelKeyPress += (s, e) => {
                    e.Cancel = true;
                    cancellationToken.Cancel();
                };
                
                try
                {
                    await Task.Delay(-1, cancellationToken.Token);
                }
                catch (TaskCanceledException)
                {
                    Console.WriteLine("收到退出信号...");
                }
            }
            
            await StopServer();
        }
        
        /// <summary>
        /// 启动TCP服务器
        /// </summary>
        static async Task StartServer()
        {
            try
            {
                var port = 8888;
                var ipAddress = IPAddress.Loopback; // 127.0.0.1
                
                _server = new TcpListener(ipAddress, port);
                _server.Start();
                _isRunning = true;
                
                Console.WriteLine($"TCP服务器已启动: {ipAddress}:{port}");
                
                // 异步处理客户端连接
                _ = Task.Run(HandleClientConnections);
                
                await Task.Delay(100); // 确保服务器启动
            }
            catch (Exception ex)
            {
                Console.WriteLine($"服务器启动失败: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 停止TCP服务器
        /// </summary>
        static async Task StopServer()
        {
            try
            {
                _isRunning = false;
                _server?.Stop();
                Console.WriteLine("TCP服务器已停止");
                await Task.Delay(100);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"服务器停止时出错: {ex.Message}");
            }
        }
        
        /// <summary>
        /// 处理客户端连接
        /// </summary>
        static async Task HandleClientConnections()
        {
            while (_isRunning && _server != null)
            {
                try
                {
                    var client = await _server.AcceptTcpClientAsync();
                    Console.WriteLine($"客户端已连接: {client.Client.RemoteEndPoint}");
                    
                    // 为每个客户端创建独立的处理任务
                    _ = Task.Run(() => HandleClient(client));
                }
                catch (ObjectDisposedException)
                {
                    // 服务器已停止，正常退出
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"接受客户端连接时出错: {ex.Message}");
                }
            }
        }
        
        /// <summary>
        /// 处理单个客户端
        /// </summary>
        static async Task HandleClient(TcpClient client)
        {
            var clientId = client.Client.RemoteEndPoint?.ToString() ?? "未知";
            
            try
            {
                using (client)
                {
                    var stream = client.GetStream();
                    var buffer = new byte[4096];
                    
                    while (client.Connected)
                    {
                        var bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);
                        if (bytesRead == 0) break; // 客户端断开连接
                        
                        var message = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        Console.WriteLine($"收到来自 {clientId} 的消息: {message}");
                        
                        // 处理消息并发送响应
                        var response = await ProcessMessage(message);
                        var responseBytes = Encoding.UTF8.GetBytes(response);
                        
                        await stream.WriteAsync(responseBytes, 0, responseBytes.Length);
                        Console.WriteLine($"向 {clientId} 发送响应: {response}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理客户端 {clientId} 时出错: {ex.Message}");
            }
            finally
            {
                Console.WriteLine($"客户端 {clientId} 已断开连接");
            }
        }
        
        /// <summary>
        /// 处理JSON消息
        /// </summary>
        static async Task<string> ProcessMessage(string jsonMessage)
        {
            try
            {
                // 解析JSON请求
                var request = JsonDocument.Parse(jsonMessage);
                var root = request.RootElement;
                
                var messageId = root.TryGetProperty("messageId", out var msgId) ? msgId.GetString() : Guid.NewGuid().ToString();
                var command = root.TryGetProperty("command", out var cmd) ? cmd.GetString() : "";
                
                // 创建响应对象
                var response = new
                {
                    messageId = messageId,
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    success = true,
                    command = command,
                    data = await ExecuteCommand(command, root),
                    error = (string?)null
                };
                
                return JsonSerializer.Serialize(response, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
            catch (Exception ex)
            {
                // 错误响应
                var errorResponse = new
                {
                    messageId = Guid.NewGuid().ToString(),
                    timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                    success = false,
                    data = (object?)null,
                    error = ex.Message
                };
                
                return JsonSerializer.Serialize(errorResponse, new JsonSerializerOptions 
                { 
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    WriteIndented = true
                });
            }
        }
        
        /// <summary>
        /// 执行具体命令
        /// </summary>
        static async Task<object> ExecuteCommand(string command, JsonElement requestData)
        {
            return command?.ToLower() switch
            {
                "ping" => new { message = "pong", version = "0.1.0" },
                "status" => new { 
                    status = "running", 
                    uptime = DateTime.Now,
                    hslCommunicationVersion = "12.3.3",
                    dotnetVersion = Environment.Version.ToString()
                },
                "test_modbus" => await TestModbusConnection(),
                _ => new { error = $"未知命令: {command}" }
            };
        }
        
        /// <summary>
        /// 测试Modbus连接（原型）
        /// </summary>
        static async Task<object> TestModbusConnection()
        {
            try
            {
                // 这是一个测试原型，实际使用时需要真实的设备配置
                var modbusTcp = new ModbusTcpNet("127.0.0.1", 502);
                
                // 注意：这里只是创建对象，不实际连接
                // 因为没有真实的Modbus设备
                
                return new
                {
                    message = "Modbus连接测试原型",
                    status = "prototype_ready",
                    supportedProtocols = new[]
                    {
                        "Modbus TCP",
                        "Modbus RTU",
                        "Siemens S7",
                        "Omron FINS",
                        "Mitsubishi MC"
                    }
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message };
            }
        }
    }
}
