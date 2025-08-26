using System.Collections.Concurrent;
using HlsService.Interfaces;
using HlsService.Models;

namespace HlsService.Services
{
    /// <summary>
    /// 设备管理器
    /// 负责管理所有设备连接的生命周期和操作
    /// </summary>
    public class DeviceManager : IDisposable
    {
        private readonly ConcurrentDictionary<string, IDeviceConnection> _devices;
        private readonly object _lockObject = new object();
        private bool _disposed = false;

        public DeviceManager()
        {
            _devices = new ConcurrentDictionary<string, IDeviceConnection>();
        }

        /// <summary>
        /// 已连接的设备数量
        /// </summary>
        public int ConnectedDeviceCount => _devices.Values.Count(d => d.IsConnected);

        /// <summary>
        /// 总设备数量
        /// </summary>
        public int TotalDeviceCount => _devices.Count;

        /// <summary>
        /// 添加设备
        /// </summary>
        /// <param name="config">设备配置</param>
        /// <returns>添加结果</returns>
        public async Task<(bool Success, string Message)> AddDeviceAsync(DeviceConfiguration config)
        {
            try
            {
                if (config == null)
                    return (false, "Device configuration is null");

                // 验证设备配置
                var (isValid, errorMessage) = DeviceConnectionFactory.ValidateDeviceConfiguration(config);
                if (!isValid)
                    return (false, errorMessage);

                // 检查设备是否已存在
                if (_devices.ContainsKey(config.DeviceId))
                    return (false, $"Device {config.DeviceId} already exists");

                // 创建设备连接
                var deviceConnection = DeviceConnectionFactory.CreateDeviceConnection(config);
                
                // 订阅状态变化事件
                deviceConnection.StatusChanged += OnDeviceStatusChanged;

                // 添加到设备字典
                if (_devices.TryAdd(config.DeviceId, deviceConnection))
                {
                    Console.WriteLine($"[设备管理] 设备 {config.DeviceId} ({config.Type}) 已添加");
                    
                    // 如果设备配置为启用，自动尝试连接
                    if (config.Enabled)
                    {
                        var connectResult = await deviceConnection.ConnectAsync(config);
                        if (!connectResult)
                        {
                            Console.WriteLine($"[设备管理] 设备 {config.DeviceId} 自动连接失败");
                        }
                    }
                    
                    return (true, $"Device {config.DeviceId} added successfully");
                }
                else
                {
                    deviceConnection.Dispose();
                    return (false, $"Failed to add device {config.DeviceId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备管理] 添加设备失败: {ex.Message}");
                return (false, $"Error adding device: {ex.Message}");
            }
        }

        /// <summary>
        /// 移除设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>移除结果</returns>
        public async Task<(bool Success, string Message)> RemoveDeviceAsync(string deviceId)
        {
            try
            {
                if (string.IsNullOrEmpty(deviceId))
                    return (false, "Device ID is required");

                if (_devices.TryRemove(deviceId, out var deviceConnection))
                {
                    // 取消事件订阅
                    deviceConnection.StatusChanged -= OnDeviceStatusChanged;
                    
                    // 断开连接并释放资源
                    await deviceConnection.DisconnectAsync();
                    deviceConnection.Dispose();
                    
                    Console.WriteLine($"[设备管理] 设备 {deviceId} 已移除");
                    return (true, $"Device {deviceId} removed successfully");
                }
                else
                {
                    return (false, $"Device {deviceId} not found");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备管理] 移除设备失败: {ex.Message}");
                return (false, $"Error removing device: {ex.Message}");
            }
        }

        /// <summary>
        /// 连接设备
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>连接结果</returns>
        public async Task<(bool Success, string Message)> ConnectDeviceAsync(string deviceId)
        {
            try
            {
                if (!_devices.TryGetValue(deviceId, out var device))
                    return (false, $"Device {deviceId} not found");

                if (device.IsConnected)
                    return (true, $"Device {deviceId} is already connected");

                var success = await device.ConnectAsync(device.Configuration);
                return success 
                    ? (true, $"Device {deviceId} connected successfully") 
                    : (false, $"Failed to connect to device {deviceId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备管理] 连接设备失败: {ex.Message}");
                return (false, $"Error connecting device: {ex.Message}");
            }
        }

        /// <summary>
        /// 断开设备连接
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>断开结果</returns>
        public async Task<(bool Success, string Message)> DisconnectDeviceAsync(string deviceId)
        {
            try
            {
                if (!_devices.TryGetValue(deviceId, out var device))
                    return (false, $"Device {deviceId} not found");

                if (!device.IsConnected)
                    return (true, $"Device {deviceId} is already disconnected");

                await device.DisconnectAsync();
                return (true, $"Device {deviceId} disconnected successfully");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备管理] 断开设备失败: {ex.Message}");
                return (false, $"Error disconnecting device: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取设备数据
        /// </summary>
        /// <param name="request">读取请求</param>
        /// <returns>读取结果</returns>
        public async Task<ReadResult[]> ReadDeviceDataAsync(DataPointReadRequest request)
        {
            try
            {
                if (!_devices.TryGetValue(request.DeviceId, out var device))
                {
                    return request.Addresses.Select(addr => new ReadResult
                    {
                        Success = false,
                        Address = addr,
                        Error = $"Device {request.DeviceId} not found"
                    }).ToArray();
                }

                if (!device.IsConnected)
                {
                    return request.Addresses.Select(addr => new ReadResult
                    {
                        Success = false,
                        Address = addr,
                        Error = $"Device {request.DeviceId} is not connected"
                    }).ToArray();
                }

                return await device.ReadBatchAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备管理] 读取设备数据失败: {ex.Message}");
                return request.Addresses.Select(addr => new ReadResult
                {
                    Success = false,
                    Address = addr,
                    Error = $"Error reading data: {ex.Message}"
                }).ToArray();
            }
        }

        /// <summary>
        /// 写入设备数据
        /// </summary>
        /// <param name="request">写入请求</param>
        /// <returns>写入结果</returns>
        public async Task<WriteResult[]> WriteDeviceDataAsync(DataPointWriteRequest request)
        {
            try
            {
                if (!_devices.TryGetValue(request.DeviceId, out var device))
                {
                    return request.DataPoints.Select(dp => new WriteResult
                    {
                        Success = false,
                        Address = dp.Address,
                        Value = dp.Value,
                        Error = $"Device {request.DeviceId} not found"
                    }).ToArray();
                }

                if (!device.IsConnected)
                {
                    return request.DataPoints.Select(dp => new WriteResult
                    {
                        Success = false,
                        Address = dp.Address,
                        Value = dp.Value,
                        Error = $"Device {request.DeviceId} is not connected"
                    }).ToArray();
                }

                return await device.WriteBatchAsync(request);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备管理] 写入设备数据失败: {ex.Message}");
                return request.DataPoints.Select(dp => new WriteResult
                {
                    Success = false,
                    Address = dp.Address,
                    Value = dp.Value,
                    Error = $"Error writing data: {ex.Message}"
                }).ToArray();
            }
        }

        /// <summary>
        /// 获取设备状态
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>设备状态</returns>
        public async Task<DeviceStatusInfo?> GetDeviceStatusAsync(string deviceId)
        {
            try
            {
                if (!_devices.TryGetValue(deviceId, out var device))
                    return null;

                return await device.GetStatusAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备管理] 获取设备状态失败: {ex.Message}");
                return null;
            }
        }

        /// <summary>
        /// 获取所有设备状态
        /// </summary>
        /// <returns>所有设备状态</returns>
        public async Task<DeviceStatusInfo[]> GetAllDeviceStatusAsync()
        {
            var statusList = new List<DeviceStatusInfo>();

            try
            {
                var tasks = _devices.Values.Select(async device =>
                {
                    try
                    {
                        return await device.GetStatusAsync();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[设备管理] 获取设备 {device.DeviceId} 状态失败: {ex.Message}");
                        return null;
                    }
                });

                var results = await Task.WhenAll(tasks);
                statusList.AddRange(results.Where(r => r != null)!);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备管理] 获取所有设备状态失败: {ex.Message}");
            }

            return statusList.ToArray();
        }

        /// <summary>
        /// 测试设备连接
        /// </summary>
        /// <param name="deviceId">设备ID</param>
        /// <returns>测试结果</returns>
        public async Task<bool> TestDeviceConnectionAsync(string deviceId)
        {
            try
            {
                if (!_devices.TryGetValue(deviceId, out var device))
                    return false;

                return await device.TestConnectionAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备管理] 测试设备连接失败: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 获取设备列表
        /// </summary>
        /// <returns>设备配置列表</returns>
        public DeviceConfiguration[] GetDeviceList()
        {
            return _devices.Values.Select(d => d.Configuration).ToArray();
        }

        /// <summary>
        /// 获取支持的设备类型
        /// </summary>
        /// <returns>支持的设备类型</returns>
        public DeviceType[] GetSupportedDeviceTypes()
        {
            return DeviceConnectionFactory.GetSupportedDeviceTypes();
        }

        /// <summary>
        /// 处理设备状态变化事件
        /// </summary>
        private void OnDeviceStatusChanged(object? sender, ConnectionStatusChangedEventArgs e)
        {
            Console.WriteLine($"[设备管理] 设备 {e.DeviceId} 状态变化: {e.OldStatus} → {e.NewStatus}");
            
            if (!string.IsNullOrEmpty(e.ErrorMessage))
            {
                Console.WriteLine($"[设备管理] 设备 {e.DeviceId} 错误信息: {e.ErrorMessage}");
            }
        }

        /// <summary>
        /// 关闭所有设备连接
        /// </summary>
        public async Task CloseAllDevicesAsync()
        {
            try
            {
                var disconnectTasks = _devices.Values.Select(async device =>
                {
                    try
                    {
                        if (device.IsConnected)
                        {
                            await device.DisconnectAsync();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[设备管理] 关闭设备 {device.DeviceId} 连接失败: {ex.Message}");
                    }
                });

                await Task.WhenAll(disconnectTasks);
                Console.WriteLine($"[设备管理] 所有设备连接已关闭");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备管理] 关闭所有设备连接失败: {ex.Message}");
            }
        }

        public void Dispose()
        {
            if (!_disposed)
            {
                CloseAllDevicesAsync().Wait(5000);

                foreach (var device in _devices.Values)
                {
                    try
                    {
                        device.StatusChanged -= OnDeviceStatusChanged;
                        device.Dispose();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[设备管理] 释放设备 {device.DeviceId} 资源失败: {ex.Message}");
                    }
                }

                _devices.Clear();
                _disposed = true;
            }
        }
    }
}