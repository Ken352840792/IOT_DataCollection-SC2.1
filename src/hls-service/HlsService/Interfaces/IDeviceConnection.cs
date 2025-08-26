using HlsService.Models;

namespace HlsService.Interfaces
{
    /// <summary>
    /// 设备连接接口
    /// 定义了所有设备连接必须实现的基本功能
    /// </summary>
    public interface IDeviceConnection : IDisposable
    {
        /// <summary>
        /// 设备唯一标识符
        /// </summary>
        string DeviceId { get; }

        /// <summary>
        /// 设备类型
        /// </summary>
        DeviceType DeviceType { get; }

        /// <summary>
        /// 当前连接状态
        /// </summary>
        ConnectionStatus Status { get; }

        /// <summary>
        /// 设备配置
        /// </summary>
        DeviceConfiguration Configuration { get; }

        /// <summary>
        /// 最后通信时间
        /// </summary>
        DateTime? LastCommunicationTime { get; }

        /// <summary>
        /// 是否已连接
        /// </summary>
        bool IsConnected { get; }

        /// <summary>
        /// 连接状态变化事件
        /// </summary>
        event EventHandler<ConnectionStatusChangedEventArgs>? StatusChanged;

        /// <summary>
        /// 异步连接到设备
        /// </summary>
        /// <param name="config">设备配置</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接是否成功</returns>
        Task<bool> ConnectAsync(DeviceConfiguration config, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步断开设备连接
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        Task DisconnectAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步读取单个数据点
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="address">数据地址</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>读取结果</returns>
        Task<ReadResult> ReadAsync<T>(string address, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步批量读取数据点
        /// </summary>
        /// <param name="request">读取请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>读取结果数组</returns>
        Task<ReadResult[]> ReadBatchAsync(DataPointReadRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步写入单个数据点
        /// </summary>
        /// <typeparam name="T">数据类型</typeparam>
        /// <param name="address">数据地址</param>
        /// <param name="value">要写入的值</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>写入结果</returns>
        Task<WriteResult> WriteAsync<T>(string address, T value, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步批量写入数据点
        /// </summary>
        /// <param name="request">写入请求</param>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>写入结果数组</returns>
        Task<WriteResult[]> WriteBatchAsync(DataPointWriteRequest request, CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步获取设备状态信息
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>设备状态信息</returns>
        Task<DeviceStatusInfo> GetStatusAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 异步测试连接
        /// </summary>
        /// <param name="cancellationToken">取消令牌</param>
        /// <returns>连接测试是否成功</returns>
        Task<bool> TestConnectionAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// 重置统计信息
        /// </summary>
        void ResetStatistics();
    }

    /// <summary>
    /// 连接状态变化事件参数
    /// </summary>
    public class ConnectionStatusChangedEventArgs : EventArgs
    {
        public string DeviceId { get; }
        public ConnectionStatus OldStatus { get; }
        public ConnectionStatus NewStatus { get; }
        public string? ErrorMessage { get; }
        public DateTime Timestamp { get; }

        public ConnectionStatusChangedEventArgs(string deviceId, ConnectionStatus oldStatus, ConnectionStatus newStatus, string? errorMessage = null)
        {
            DeviceId = deviceId;
            OldStatus = oldStatus;
            NewStatus = newStatus;
            ErrorMessage = errorMessage;
            Timestamp = DateTime.UtcNow;
        }
    }
}