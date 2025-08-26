using System.Diagnostics;
using HlsService.Models;
using HslCommunication.ModBus;

namespace HlsService.Services.Devices
{
    /// <summary>
    /// 简化的Modbus TCP设备连接实现
    /// 用于测试和验证基本功能
    /// </summary>
    public class ModbusTcpConnectionSimple : DeviceConnectionBase
    {
        private ModbusTcpNet? _modbusClient;
        private readonly object _clientLock = new object();

        public ModbusTcpConnectionSimple(string deviceId) : base(deviceId, DeviceType.ModbusTcp)
        {
        }

        protected override async Task<bool> ConnectToDeviceAsync(DeviceConfiguration config, CancellationToken cancellationToken)
        {
            try
            {
                lock (_clientLock)
                {
                    // 如果已有连接，先关闭
                    if (_modbusClient != null)
                    {
                        _modbusClient.ConnectClose();
                        _modbusClient = null;
                    }

                    // 创建新的Modbus TCP客户端
                    _modbusClient = new ModbusTcpNet(config.Connection.Host, config.Connection.Port)
                    {
                        Station = config.Connection.Station,
                        ConnectTimeOut = config.Connection.TimeoutMs,
                        ReceiveTimeOut = config.Connection.TimeoutMs
                    };
                }

                // 尝试连接
                var connectResult = await Task.Run(() => _modbusClient.ConnectServer(), cancellationToken);
                
                if (connectResult.IsSuccess)
                {
                    Console.WriteLine($"[Modbus TCP] 设备 {DeviceId} 连接成功: {config.Connection.Host}:{config.Connection.Port}");
                    return true;
                }
                else
                {
                    Console.WriteLine($"[Modbus TCP] 设备 {DeviceId} 连接失败: {connectResult.Message}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Modbus TCP] 设备 {DeviceId} 连接异常: {ex.Message}");
                return false;
            }
        }

        protected override async Task DisconnectFromDeviceAsync(CancellationToken cancellationToken)
        {
            try
            {
                lock (_clientLock)
                {
                    if (_modbusClient != null)
                    {
                        _modbusClient.ConnectClose();
                        _modbusClient = null;
                    }
                }

                await Task.CompletedTask;
                Console.WriteLine($"[Modbus TCP] 设备 {DeviceId} 已断开连接");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Modbus TCP] 设备 {DeviceId} 断开连接异常: {ex.Message}");
            }
        }

        protected override async Task<ReadResult> ReadFromDeviceAsync<T>(string address, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (_modbusClient == null)
                {
                    throw new InvalidOperationException("Modbus client is not initialized");
                }

                // 简化实现：只读取Int16类型
                var readResult = await Task.Run(() => _modbusClient.ReadInt16(address), cancellationToken);

                stopwatch.Stop();

                if (readResult.IsSuccess)
                {
                    return new ReadResult
                    {
                        Success = true,
                        Address = address,
                        Value = readResult.Content,
                        DataType = "Int16",
                        ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds
                    };
                }
                else
                {
                    return new ReadResult
                    {
                        Success = false,
                        Address = address,
                        Error = readResult.Message,
                        DataType = "Int16",
                        ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds
                    };
                }
            }
            catch (Exception ex)
            {
                return new ReadResult
                {
                    Success = false,
                    Address = address,
                    Error = ex.Message,
                    DataType = "Int16",
                    ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }
        }

        protected override async Task<ReadResult[]> ReadBatchFromDeviceAsync(DataPointReadRequest request, CancellationToken cancellationToken)
        {
            var results = new List<ReadResult>();

            try
            {
                if (_modbusClient == null)
                {
                    return request.Addresses.Select(addr => new ReadResult
                    {
                        Success = false,
                        Address = addr,
                        Error = "Modbus client is not initialized"
                    }).ToArray();
                }

                // 逐个读取每个地址
                foreach (var address in request.Addresses)
                {
                    var result = await ReadFromDeviceAsync<object>(address, cancellationToken);
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Modbus TCP] 批量读取异常: {ex.Message}");
                
                return request.Addresses.Select(addr => new ReadResult
                {
                    Success = false,
                    Address = addr,
                    Error = ex.Message
                }).ToArray();
            }

            return results.ToArray();
        }

        protected override async Task<WriteResult> WriteToDeviceAsync<T>(string address, T value, CancellationToken cancellationToken)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                if (_modbusClient == null)
                {
                    throw new InvalidOperationException("Modbus client is not initialized");
                }

                // 简化实现：只写入Int16类型
                var writeResult = await Task.Run(() => _modbusClient.Write(address, Convert.ToInt16(value)), cancellationToken);

                stopwatch.Stop();

                return new WriteResult
                {
                    Success = writeResult.IsSuccess,
                    Address = address,
                    Value = value,
                    Error = writeResult.IsSuccess ? null : writeResult.Message,
                    ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }
            catch (Exception ex)
            {
                return new WriteResult
                {
                    Success = false,
                    Address = address,
                    Value = value,
                    Error = ex.Message,
                    ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }
        }

        protected override async Task<WriteResult[]> WriteBatchToDeviceAsync(DataPointWriteRequest request, CancellationToken cancellationToken)
        {
            var results = new List<WriteResult>();

            try
            {
                if (_modbusClient == null)
                {
                    return request.DataPoints.Select(dp => new WriteResult
                    {
                        Success = false,
                        Address = dp.Address,
                        Value = dp.Value,
                        Error = "Modbus client is not initialized"
                    }).ToArray();
                }

                // 逐个写入每个数据点
                foreach (var dataPoint in request.DataPoints)
                {
                    var result = await WriteToDeviceAsync<object>(dataPoint.Address, dataPoint.Value, cancellationToken);
                    results.Add(result);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Modbus TCP] 批量写入异常: {ex.Message}");
                
                return request.DataPoints.Select(dp => new WriteResult
                {
                    Success = false,
                    Address = dp.Address,
                    Value = dp.Value,
                    Error = ex.Message
                }).ToArray();
            }

            return results.ToArray();
        }

        protected override async Task<bool> PerformConnectionTestAsync(CancellationToken cancellationToken)
        {
            try
            {
                if (_modbusClient == null)
                    return false;

                // 尝试读取一个测试地址来验证连接
                var testResult = await Task.Run(() => _modbusClient.ReadCoil("0"), cancellationToken);
                
                // 即使读取失败，如果不是连接错误，也说明连接是正常的
                return testResult.IsSuccess || !IsConnectionError(testResult.Message);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Modbus TCP] 连接测试异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 判断是否是连接相关的错误
        /// </summary>
        private static bool IsConnectionError(string errorMessage)
        {
            if (string.IsNullOrEmpty(errorMessage))
                return false;

            var errorMsg = errorMessage.ToLower();
            return errorMsg.Contains("socket") || 
                   errorMsg.Contains("connect") || 
                   errorMsg.Contains("timeout") ||
                   errorMsg.Contains("网络");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                lock (_clientLock)
                {
                    if (_modbusClient != null)
                    {
                        try
                        {
                            _modbusClient.ConnectClose();
                        }
                        catch (Exception ex)
                        {
                            Console.WriteLine($"[Modbus TCP] 释放连接时出错: {ex.Message}");
                        }
                        finally
                        {
                            _modbusClient = null;
                        }
                    }
                }
            }

            base.Dispose(disposing);
        }
    }
}