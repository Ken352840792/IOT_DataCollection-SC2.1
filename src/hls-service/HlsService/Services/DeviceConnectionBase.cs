using System.Diagnostics;
using HlsService.Interfaces;
using HlsService.Models;

namespace HlsService.Services
{
    /// <summary>
    /// 设备连接基类
    /// 提供了设备连接的通用功能实现
    /// </summary>
    public abstract class DeviceConnectionBase : IDeviceConnection
    {
        protected readonly object _lockObject = new object();
        protected bool _disposed = false;
        
        private ConnectionStatus _status = ConnectionStatus.Disconnected;
        private DateTime? _lastCommunicationTime;
        private readonly CommunicationStatistics _statistics = new();

        protected DeviceConnectionBase(string deviceId, DeviceType deviceType)
        {
            DeviceId = deviceId ?? throw new ArgumentNullException(nameof(deviceId));
            DeviceType = deviceType;
            Configuration = new DeviceConfiguration { DeviceId = deviceId, Type = deviceType };
        }

        public string DeviceId { get; }
        public DeviceType DeviceType { get; }
        public DeviceConfiguration Configuration { get; protected set; }
        public DateTime? LastCommunicationTime => _lastCommunicationTime;
        public bool IsConnected => _status == ConnectionStatus.Connected;

        public ConnectionStatus Status
        {
            get => _status;
            protected set
            {
                if (_status != value)
                {
                    var oldStatus = _status;
                    _status = value;
                    OnStatusChanged(oldStatus, value);
                }
            }
        }

        public event EventHandler<ConnectionStatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// 连接到设备（子类实现具体逻辑）
        /// </summary>
        public async Task<bool> ConnectAsync(DeviceConfiguration config, CancellationToken cancellationToken = default)
        {
            try
            {
                Configuration = config ?? throw new ArgumentNullException(nameof(config));
                Status = ConnectionStatus.Connecting;

                var success = await ConnectToDeviceAsync(config, cancellationToken);
                
                Status = success ? ConnectionStatus.Connected : ConnectionStatus.Error;
                
                if (success)
                {
                    Console.WriteLine($"[设备连接] 设备 {DeviceId} ({DeviceType}) 连接成功");
                }
                else
                {
                    Console.WriteLine($"[设备连接] 设备 {DeviceId} ({DeviceType}) 连接失败");
                }

                return success;
            }
            catch (Exception ex)
            {
                Status = ConnectionStatus.Error;
                Console.WriteLine($"[设备连接] 设备 {DeviceId} 连接异常: {ex.Message}");
                return false;
            }
        }

        /// <summary>
        /// 断开设备连接
        /// </summary>
        public async Task DisconnectAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                if (Status == ConnectionStatus.Disconnected)
                    return;

                await DisconnectFromDeviceAsync(cancellationToken);
                Status = ConnectionStatus.Disconnected;
                
                Console.WriteLine($"[设备连接] 设备 {DeviceId} ({DeviceType}) 已断开连接");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[设备连接] 设备 {DeviceId} 断开连接异常: {ex.Message}");
            }
        }

        /// <summary>
        /// 读取单个数据点
        /// </summary>
        public async Task<ReadResult> ReadAsync<T>(string address, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                return new ReadResult
                {
                    Success = false,
                    Address = address,
                    Error = "Device is not connected"
                };
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var result = await ReadFromDeviceAsync<T>(address, cancellationToken);
                UpdateStatistics(true, false, stopwatch.Elapsed.TotalMilliseconds);
                UpdateLastCommunicationTime();
                
                return result;
            }
            catch (Exception ex)
            {
                UpdateStatistics(false, false, stopwatch.Elapsed.TotalMilliseconds);
                
                return new ReadResult
                {
                    Success = false,
                    Address = address,
                    Error = ex.Message,
                    ResponseTimeMs = stopwatch.Elapsed.TotalMilliseconds
                };
            }
        }

        /// <summary>
        /// 批量读取数据点
        /// </summary>
        public async Task<ReadResult[]> ReadBatchAsync(DataPointReadRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                return request.Addresses.Select(addr => new ReadResult
                {
                    Success = false,
                    Address = addr,
                    Error = "Device is not connected"
                }).ToArray();
            }

            return await ReadBatchFromDeviceAsync(request, cancellationToken);
        }

        /// <summary>
        /// 写入单个数据点
        /// </summary>
        public async Task<WriteResult> WriteAsync<T>(string address, T value, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                return new WriteResult
                {
                    Success = false,
                    Address = address,
                    Value = value,
                    Error = "Device is not connected"
                };
            }

            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var result = await WriteToDeviceAsync(address, value, cancellationToken);
                UpdateStatistics(false, true, stopwatch.Elapsed.TotalMilliseconds);
                UpdateLastCommunicationTime();
                
                return result;
            }
            catch (Exception ex)
            {
                UpdateStatistics(false, false, stopwatch.Elapsed.TotalMilliseconds);
                
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

        /// <summary>
        /// 批量写入数据点
        /// </summary>
        public async Task<WriteResult[]> WriteBatchAsync(DataPointWriteRequest request, CancellationToken cancellationToken = default)
        {
            if (!IsConnected)
            {
                return request.DataPoints.Select(dp => new WriteResult
                {
                    Success = false,
                    Address = dp.Address,
                    Value = dp.Value,
                    Error = "Device is not connected"
                }).ToArray();
            }

            return await WriteBatchToDeviceAsync(request, cancellationToken);
        }

        /// <summary>
        /// 获取设备状态信息
        /// </summary>
        public virtual async Task<DeviceStatusInfo> GetStatusAsync(CancellationToken cancellationToken = default)
        {
            return new DeviceStatusInfo
            {
                DeviceId = DeviceId,
                Status = Status,
                LastCommunicationTime = LastCommunicationTime,
                Statistics = new CommunicationStatistics
                {
                    TotalReads = _statistics.TotalReads,
                    TotalWrites = _statistics.TotalWrites,
                    SuccessfulReads = _statistics.SuccessfulReads,
                    SuccessfulWrites = _statistics.SuccessfulWrites,
                    AverageResponseTimeMs = _statistics.AverageResponseTimeMs,
                    MaxResponseTimeMs = _statistics.MaxResponseTimeMs,
                    BytesTransferred = _statistics.BytesTransferred
                }
            };
        }

        /// <summary>
        /// 测试连接
        /// </summary>
        public virtual async Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default)
        {
            try
            {
                return await PerformConnectionTestAsync(cancellationToken);
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 重置统计信息
        /// </summary>
        public void ResetStatistics()
        {
            lock (_lockObject)
            {
                _statistics.TotalReads = 0;
                _statistics.TotalWrites = 0;
                _statistics.SuccessfulReads = 0;
                _statistics.SuccessfulWrites = 0;
                _statistics.AverageResponseTimeMs = 0;
                _statistics.MaxResponseTimeMs = 0;
                _statistics.BytesTransferred = 0;
            }
        }

        #region 抽象方法 - 子类需要实现

        /// <summary>
        /// 连接到具体设备（子类实现）
        /// </summary>
        protected abstract Task<bool> ConnectToDeviceAsync(DeviceConfiguration config, CancellationToken cancellationToken);

        /// <summary>
        /// 从具体设备断开连接（子类实现）
        /// </summary>
        protected abstract Task DisconnectFromDeviceAsync(CancellationToken cancellationToken);

        /// <summary>
        /// 从设备读取数据（子类实现）
        /// </summary>
        protected abstract Task<ReadResult> ReadFromDeviceAsync<T>(string address, CancellationToken cancellationToken);

        /// <summary>
        /// 批量从设备读取数据（子类实现）
        /// </summary>
        protected abstract Task<ReadResult[]> ReadBatchFromDeviceAsync(DataPointReadRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// 向设备写入数据（子类实现）
        /// </summary>
        protected abstract Task<WriteResult> WriteToDeviceAsync<T>(string address, T value, CancellationToken cancellationToken);

        /// <summary>
        /// 批量向设备写入数据（子类实现）
        /// </summary>
        protected abstract Task<WriteResult[]> WriteBatchToDeviceAsync(DataPointWriteRequest request, CancellationToken cancellationToken);

        /// <summary>
        /// 执行连接测试（子类实现）
        /// </summary>
        protected abstract Task<bool> PerformConnectionTestAsync(CancellationToken cancellationToken);

        #endregion

        #region 保护方法

        /// <summary>
        /// 触发状态变化事件
        /// </summary>
        protected virtual void OnStatusChanged(ConnectionStatus oldStatus, ConnectionStatus newStatus, string? errorMessage = null)
        {
            StatusChanged?.Invoke(this, new ConnectionStatusChangedEventArgs(DeviceId, oldStatus, newStatus, errorMessage));
        }

        /// <summary>
        /// 更新最后通信时间
        /// </summary>
        protected void UpdateLastCommunicationTime()
        {
            _lastCommunicationTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 更新统计信息
        /// </summary>
        protected void UpdateStatistics(bool readSuccess, bool writeSuccess, double responseTime)
        {
            lock (_lockObject)
            {
                if (readSuccess)
                {
                    _statistics.TotalReads++;
                    _statistics.SuccessfulReads++;
                }
                else if (!writeSuccess && !readSuccess)
                {
                    _statistics.TotalReads++; // 假设是读取操作失败
                }

                if (writeSuccess)
                {
                    _statistics.TotalWrites++;
                    _statistics.SuccessfulWrites++;
                }

                // 更新响应时间统计
                if (_statistics.MaxResponseTimeMs < responseTime)
                {
                    _statistics.MaxResponseTimeMs = responseTime;
                }

                // 计算平均响应时间
                var totalOperations = _statistics.TotalReads + _statistics.TotalWrites;
                if (totalOperations > 0)
                {
                    _statistics.AverageResponseTimeMs = 
                        (_statistics.AverageResponseTimeMs * (totalOperations - 1) + responseTime) / totalOperations;
                }
            }
        }

        #endregion

        #region IDisposable

        protected virtual void Dispose(bool disposing)
        {
            if (!_disposed && disposing)
            {
                try
                {
                    DisconnectAsync().Wait(5000);
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[设备连接] 设备 {DeviceId} 释放时出错: {ex.Message}");
                }
                
                _disposed = true;
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        #endregion
    }
}