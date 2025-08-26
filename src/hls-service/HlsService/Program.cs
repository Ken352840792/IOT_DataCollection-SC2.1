using System.Text.Json;
using HlsService.Models;
using HlsService.Services;

namespace HlsService
{
    /// <summary>
    /// HLS-Communication服务主程序
    /// 专业化的IPC通信服务器，支持与Node-RED的高性能数据交互
    /// </summary>
    class Program
    {
        private static IpcServer? _server;
        private static ServerConfiguration _config = new();

        static async Task Main(string[] args)
        {
            // 显示启动信息
            Console.WriteLine("=== HLS-Communication IPC 服务 ===");
            Console.WriteLine($".NET 版本: {Environment.Version}");
            Console.WriteLine($"进程 ID: {Environment.ProcessId}");
            Console.WriteLine($"启动时间: {DateTime.Now:yyyy-MM-dd HH:mm:ss}");
            Console.WriteLine();

            try
            {
                // 加载配置
                await LoadConfiguration();

                // 创建并启动IPC服务器
                _server = new IpcServer(_config);
                await _server.StartAsync();

                // 设置优雅关闭处理
                Console.CancelKeyPress += async (sender, e) =>
                {
                    e.Cancel = true;
                    Console.WriteLine("\n[系统] 收到关闭信号，正在优雅关闭服务器...");
                    
                    if (_server != null)
                    {
                        await _server.StopAsync();
                    }
                    
                    Environment.Exit(0);
                };

                // 启动状态监控
                _ = Task.Run(PeriodicStatusReport);

                // 检查运行模式
                if (Environment.UserInteractive && !Console.IsInputRedirected)
                {
                    Console.WriteLine("[系统] 服务已启动。按 'q' 退出，按 's' 查看状态，按 'h' 查看帮助");
                    await HandleInteractiveMode();
                }
                else
                {
                    Console.WriteLine("[系统] 服务已启动，使用 Ctrl+C 退出");
                    
                    // 等待关闭信号
                    await Task.Delay(-1);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[错误] 服务启动失败: {ex.Message}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[错误] 内部异常: {ex.InnerException.Message}");
                }
                Environment.Exit(1);
            }
            finally
            {
                if (_server != null)
                {
                    await _server.StopAsync();
                    _server.Dispose();
                }
                
                Console.WriteLine("[系统] 服务已完全停止");
            }
        }

        /// <summary>
        /// 加载服务器配置
        /// </summary>
        private static async Task LoadConfiguration()
        {
            try
            {
                var configPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "appsettings.json");
                
                if (File.Exists(configPath))
                {
                    var configJson = await File.ReadAllTextAsync(configPath);
                    var configRoot = JsonDocument.Parse(configJson);
                    
                    if (configRoot.RootElement.TryGetProperty("ServerConfiguration", out var serverConfigElement))
                    {
                        _config = JsonSerializer.Deserialize<ServerConfiguration>(serverConfigElement) ?? new ServerConfiguration();
                        Console.WriteLine("[配置] 配置文件加载成功");
                    }
                    else
                    {
                        Console.WriteLine("[配置] 配置文件中未找到 ServerConfiguration，使用默认配置");
                    }
                }
                else
                {
                    Console.WriteLine("[配置] 配置文件不存在，使用默认配置");
                }

                // 显示关键配置信息
                Console.WriteLine($"[配置] 监听地址: {_config.Host}:{_config.Port}");
                Console.WriteLine($"[配置] 最大连接数: {_config.MaxConnections}");
                Console.WriteLine($"[配置] 协议版本: {_config.ProtocolVersion}");
                Console.WriteLine($"[配置] 详细日志: {(_config.EnableVerboseLogging ? "启用" : "禁用")}");
                Console.WriteLine();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[配置] 加载配置时出错: {ex.Message}，使用默认配置");
            }
        }

        /// <summary>
        /// 处理交互模式
        /// </summary>
        private static async Task HandleInteractiveMode()
        {
            while (true)
            {
                var key = Console.ReadKey(true);
                
                switch (key.KeyChar)
                {
                    case 'q':
                    case 'Q':
                        Console.WriteLine("\n[系统] 用户请求退出");
                        return;
                    
                    case 's':
                    case 'S':
                        await ShowDetailedStatus();
                        break;
                    
                    case 'c':
                    case 'C':
                        ShowConnectionInfo();
                        break;
                    
                    case 'h':
                    case 'H':
                        ShowHelp();
                        break;
                    
                    case 'l':
                    case 'L':
                        ToggleVerboseLogging();
                        break;
                    
                    case 't':
                    case 'T':
                        _ = Task.Run(async () => 
                        {
                            try
                            {
                                await HlsService.Tests.DeviceManagementTests.RunAllTests();
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"[测试] 运行设备管理测试时出错: {ex.Message}");
                            }
                        });
                        break;
                }
            }
        }

        /// <summary>
        /// 定期状态报告
        /// </summary>
        private static async Task PeriodicStatusReport()
        {
            while (_server?.IsRunning == true)
            {
                try
                {
                    await Task.Delay(TimeSpan.FromMinutes(5));
                    
                    if (_server?.IsRunning == true)
                    {
                        var stats = _server.Statistics;
                        Console.WriteLine($"[状态] 活跃连接: {stats.ActiveConnections}, 总消息: {stats.MessagesProcessed}");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[状态] 状态报告出错: {ex.Message}");
                }
            }
        }

        /// <summary>
        /// 显示详细状态
        /// </summary>
        private static async Task ShowDetailedStatus()
        {
            if (_server == null)
            {
                Console.WriteLine("[状态] 服务器未运行");
                return;
            }

            var stats = _server.Statistics;
            var process = System.Diagnostics.Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / (1024.0 * 1024.0);

            Console.WriteLine("\n=== 服务器状态 ===");
            Console.WriteLine($"运行状态: {(_server.IsRunning ? "运行中" : "已停止")}");
            Console.WriteLine($"活跃连接: {stats.ActiveConnections}");
            Console.WriteLine($"总连接数: {stats.TotalConnections}");
            Console.WriteLine($"已处理消息: {stats.MessagesProcessed}");
            Console.WriteLine($"内存使用: {memoryMB:F1} MB");
            Console.WriteLine($"进程ID: {Environment.ProcessId}");
            Console.WriteLine("==================\n");
        }

        /// <summary>
        /// 显示连接信息
        /// </summary>
        private static void ShowConnectionInfo()
        {
            if (_server == null)
            {
                Console.WriteLine("[连接] 服务器未运行");
                return;
            }

            var connections = _server.GetConnectionInfo().ToList();
            
            Console.WriteLine($"\n=== 连接信息 ({connections.Count}) ===");
            
            if (connections.Any())
            {
                foreach (var conn in connections)
                {
                    var uptime = DateTime.UtcNow - conn.ConnectedTime;
                    Console.WriteLine($"ID: {conn.ClientId}, 端点: {conn.RemoteEndpoint}");
                    Console.WriteLine($"  连接时间: {uptime.TotalMinutes:F1} 分钟, 消息数: {conn.MessageCount}");
                }
            }
            else
            {
                Console.WriteLine("当前无活跃连接");
            }
            
            Console.WriteLine("========================\n");
        }

        /// <summary>
        /// 显示帮助信息
        /// </summary>
        private static void ShowHelp()
        {
            Console.WriteLine("\n=== 可用命令 ===");
            Console.WriteLine("q - 退出服务");
            Console.WriteLine("s - 显示详细状态");
            Console.WriteLine("c - 显示连接信息");
            Console.WriteLine("l - 切换详细日志");
            Console.WriteLine("t - 运行设备管理测试");
            Console.WriteLine("h - 显示此帮助");
            Console.WriteLine("================\n");
        }

        /// <summary>
        /// 切换详细日志模式
        /// </summary>
        private static void ToggleVerboseLogging()
        {
            _config.EnableVerboseLogging = !_config.EnableVerboseLogging;
            Console.WriteLine($"\n[日志] 详细日志已{(_config.EnableVerboseLogging ? "启用" : "禁用")}\n");
        }
    }
}
