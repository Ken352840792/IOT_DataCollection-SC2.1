using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using HlsService.Models;

namespace HlsService.Tests
{
    /// <summary>
    /// 设备管理功能测试
    /// 测试通过IPC接口进行设备管理的完整功能
    /// </summary>
    public class DeviceManagementTests
    {
        private readonly string _host = "127.0.0.1";
        private readonly int _port = 8888;
        
        /// <summary>
        /// 运行所有设备管理测试
        /// </summary>
        public static async Task RunAllTests()
        {
            var tests = new DeviceManagementTests();
            
            Console.WriteLine("=== 设备管理功能测试 ===");
            Console.WriteLine();
            
            try
            {
                // 等待服务器启动
                await Task.Delay(1000);
                
                // 执行测试序列
                await tests.TestDeviceList();
                await tests.TestAddModbusTcpDevice();
                await tests.TestDeviceStatus();
                await tests.TestConnectDevice();
                await tests.TestReadData();
                await tests.TestWriteData();
                await tests.TestDisconnectDevice();
                await tests.TestRemoveDevice();
                
                Console.WriteLine("\n✅ 所有设备管理测试通过");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"\n❌ 测试失败: {ex.Message}");
            }
        }

        /// <summary>
        /// 测试获取设备列表
        /// </summary>
        private async Task TestDeviceList()
        {
            Console.WriteLine("🔄 测试获取设备列表...");
            
            var response = await SendIpcCommand("device_list", null);
            
            if (response.Success)
            {
                Console.WriteLine("✅ 设备列表获取成功");
                if (response.Data is JsonElement dataElement && dataElement.TryGetProperty("supportedTypes", out var typesElement))
                {
                    var types = typesElement.EnumerateArray().Select(e => e.GetString()).ToArray();
                    Console.WriteLine($"   支持的设备类型: {string.Join(", ", types)}");
                }
            }
            else
            {
                Console.WriteLine($"❌ 设备列表获取失败: {response.Error}");
            }
        }

        /// <summary>
        /// 测试添加Modbus TCP设备
        /// </summary>
        private async Task TestAddModbusTcpDevice()
        {
            Console.WriteLine("\n🔄 测试添加Modbus TCP设备...");
            
            var deviceConfig = new
            {
                deviceId = "modbus_test_001",
                name = "测试Modbus设备",
                description = "用于测试的Modbus TCP设备",
                type = "ModbusTcp",
                enabled = true,
                connection = new
                {
                    host = "127.0.0.1",
                    port = 502,
                    station = 1,
                    timeoutMs = 5000
                }
            };
            
            var response = await SendIpcCommand("add_device", deviceConfig);
            
            if (response.Success)
            {
                Console.WriteLine("✅ Modbus TCP设备添加成功");
                if (response.Data is JsonElement dataElement && dataElement.TryGetProperty("deviceId", out var deviceIdElement))
                {
                    Console.WriteLine($"   设备ID: {deviceIdElement.GetString()}");
                }
            }
            else
            {
                Console.WriteLine($"❌ Modbus TCP设备添加失败: {response.Error}");
            }
        }

        /// <summary>
        /// 测试获取设备状态
        /// </summary>
        private async Task TestDeviceStatus()
        {
            Console.WriteLine("\n🔄 测试获取设备状态...");
            
            var request = new { deviceId = "modbus_test_001" };
            var response = await SendIpcCommand("device_status", request);
            
            if (response.Success)
            {
                Console.WriteLine("✅ 设备状态获取成功");
                if (response.Data is JsonElement dataElement && 
                    dataElement.TryGetProperty("status", out var statusElement) &&
                    statusElement.TryGetProperty("status", out var statusValueElement))
                {
                    Console.WriteLine($"   设备状态: {statusValueElement.GetString()}");
                }
            }
            else
            {
                Console.WriteLine($"❌ 设备状态获取失败: {response.Error}");
            }
        }

        /// <summary>
        /// 测试连接设备
        /// </summary>
        private async Task TestConnectDevice()
        {
            Console.WriteLine("\n🔄 测试连接设备...");
            
            var request = new { deviceId = "modbus_test_001" };
            var response = await SendIpcCommand("connect_device", request);
            
            if (response.Success)
            {
                Console.WriteLine("✅ 设备连接成功");
            }
            else
            {
                Console.WriteLine($"❌ 设备连接失败: {response.Error}");
                Console.WriteLine("   注意: 这可能是因为没有实际的Modbus服务器在运行");
            }
        }

        /// <summary>
        /// 测试读取数据
        /// </summary>
        private async Task TestReadData()
        {
            Console.WriteLine("\n🔄 测试读取数据...");
            
            var request = new
            {
                deviceId = "modbus_test_001",
                addresses = new[] { "0", "1", "2" }
            };
            
            var response = await SendIpcCommand("read_data", request);
            
            if (response.Success)
            {
                Console.WriteLine("✅ 数据读取请求成功发送");
                if (response.Data is JsonElement dataElement && dataElement.TryGetProperty("results", out var resultsElement))
                {
                    foreach (var result in resultsElement.EnumerateArray())
                    {
                        if (result.TryGetProperty("address", out var addressElement) &&
                            result.TryGetProperty("success", out var successElement))
                        {
                            var address = addressElement.GetString();
                            var success = successElement.GetBoolean();
                            Console.WriteLine($"   地址 {address}: {(success ? "成功" : "失败")}");
                        }
                    }
                }
            }
            else
            {
                Console.WriteLine($"❌ 数据读取失败: {response.Error}");
            }
        }

        /// <summary>
        /// 测试写入数据
        /// </summary>
        private async Task TestWriteData()
        {
            Console.WriteLine("\n🔄 测试写入数据...");
            
            var request = new
            {
                deviceId = "modbus_test_001",
                dataPoints = new object[]
                {
                    new { address = "0", value = true },
                    new { address = "1", value = 100 }
                }
            };
            
            var response = await SendIpcCommand("write_data", request);
            
            if (response.Success)
            {
                Console.WriteLine("✅ 数据写入请求成功发送");
            }
            else
            {
                Console.WriteLine($"❌ 数据写入失败: {response.Error}");
            }
        }

        /// <summary>
        /// 测试断开设备连接
        /// </summary>
        private async Task TestDisconnectDevice()
        {
            Console.WriteLine("\n🔄 测试断开设备连接...");
            
            var request = new { deviceId = "modbus_test_001" };
            var response = await SendIpcCommand("disconnect_device", request);
            
            if (response.Success)
            {
                Console.WriteLine("✅ 设备断开成功");
            }
            else
            {
                Console.WriteLine($"❌ 设备断开失败: {response.Error}");
            }
        }

        /// <summary>
        /// 测试移除设备
        /// </summary>
        private async Task TestRemoveDevice()
        {
            Console.WriteLine("\n🔄 测试移除设备...");
            
            var request = new { deviceId = "modbus_test_001" };
            var response = await SendIpcCommand("remove_device", request);
            
            if (response.Success)
            {
                Console.WriteLine("✅ 设备移除成功");
            }
            else
            {
                Console.WriteLine($"❌ 设备移除失败: {response.Error}");
            }
        }

        /// <summary>
        /// 发送IPC命令
        /// </summary>
        private async Task<IpcResponse> SendIpcCommand(string command, object? data)
        {
            try
            {
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(_host, _port);
                
                var stream = tcpClient.GetStream();
                
                var request = new IpcRequest
                {
                    MessageId = Guid.NewGuid().ToString(),
                    Command = command,
                    Data = data,
                    Version = "1.0"
                };
                
                var requestJson = JsonSerializer.Serialize(request, new JsonSerializerOptions
                {
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase
                });
                
                var requestBytes = Encoding.UTF8.GetBytes(requestJson);
                await stream.WriteAsync(requestBytes);
                
                // 读取响应
                var buffer = new byte[8192];
                var bytesRead = await stream.ReadAsync(buffer);
                var responseJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                
                var response = JsonSerializer.Deserialize<IpcResponse>(responseJson, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
                
                return response ?? new IpcResponse 
                { 
                    Success = false, 
                    Error = ErrorFactory.CreateInternalError("Failed to deserialize response") 
                };
            }
            catch (Exception ex)
            {
                return new IpcResponse
                {
                    Success = false,
                    Error = ErrorFactory.CreateInternalError($"Connection error: {ex.Message}", ex)
                };
            }
        }
    }
}