using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HlsService.Models
{
    /// <summary>
    /// 连接请求数据模型
    /// </summary>
    public class ConnectRequestData
    {
        /// <summary>
        /// 设备配置
        /// </summary>
        [Required]
        [JsonPropertyName("deviceConfig")]
        public DeviceConfiguration DeviceConfig { get; set; } = new();
        
        /// <summary>
        /// 数据点配置列表
        /// </summary>
        [JsonPropertyName("dataPoints")]
        public List<DataPointConfiguration> DataPoints { get; set; } = new();
        
        /// <summary>
        /// 连接选项
        /// </summary>
        [JsonPropertyName("options")]
        public ConnectionOptions? Options { get; set; }
    }
    
    /// <summary>
    /// 连接响应数据模型
    /// </summary>
    public class ConnectResponseData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 连接状态
        /// </summary>
        [JsonPropertyName("status")]
        public ConnectionStatus Status { get; set; }
        
        /// <summary>
        /// 设备信息
        /// </summary>
        [JsonPropertyName("deviceInfo")]
        public ConnectedDeviceInfo DeviceInfo { get; set; } = new();
        
        /// <summary>
        /// 支持的数据点数量
        /// </summary>
        [JsonPropertyName("supportedDataPointsCount")]
        public int SupportedDataPointsCount { get; set; }
        
        /// <summary>
        /// 连接建立时间
        /// </summary>
        [JsonPropertyName("connectedAt")]
        public DateTime ConnectedAt { get; set; } = DateTime.UtcNow;
    }
    
    /// <summary>
    /// 断开连接请求数据模型
    /// </summary>
    public class DisconnectRequestData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [Required]
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 强制断开标志
        /// </summary>
        [JsonPropertyName("force")]
        public bool Force { get; set; } = false;
    }
    
    /// <summary>
    /// 断开连接响应数据模型
    /// </summary>
    public class DisconnectResponseData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 断开状态
        /// </summary>
        [JsonPropertyName("disconnected")]
        public bool Disconnected { get; set; }
        
        /// <summary>
        /// 断开时间
        /// </summary>
        [JsonPropertyName("disconnectedAt")]
        public DateTime DisconnectedAt { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 连接持续时间（毫秒）
        /// </summary>
        [JsonPropertyName("connectionDurationMs")]
        public long ConnectionDurationMs { get; set; }
    }
    
    /// <summary>
    /// 连接状态查询请求数据模型
    /// </summary>
    public class StatusRequestData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [Required]
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 是否包含详细信息
        /// </summary>
        [JsonPropertyName("includeDetails")]
        public bool IncludeDetails { get; set; } = false;
    }
    
    /// <summary>
    /// 连接状态响应数据模型
    /// </summary>
    public class StatusResponseData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 连接状态
        /// </summary>
        [JsonPropertyName("status")]
        public ConnectionStatus Status { get; set; }
        
        /// <summary>
        /// 状态描述
        /// </summary>
        [JsonPropertyName("statusDescription")]
        public string StatusDescription { get; set; } = string.Empty;
        
        /// <summary>
        /// 连接建立时间
        /// </summary>
        [JsonPropertyName("connectedAt")]
        public DateTime? ConnectedAt { get; set; }
        
        /// <summary>
        /// 最后活动时间
        /// </summary>
        [JsonPropertyName("lastActivity")]
        public DateTime? LastActivity { get; set; }
        
        /// <summary>
        /// 设备信息（详细模式）
        /// </summary>
        [JsonPropertyName("deviceInfo")]
        public ConnectedDeviceInfo? DeviceInfo { get; set; }
        
        /// <summary>
        /// 连接统计信息（详细模式）
        /// </summary>
        [JsonPropertyName("statistics")]
        public ConnectionStatistics? Statistics { get; set; }
    }
    
    /// <summary>
    /// 连接列表请求数据模型
    /// </summary>
    public class ListConnectionsRequestData
    {
        /// <summary>
        /// 是否包含详细信息
        /// </summary>
        [JsonPropertyName("includeDetails")]
        public bool IncludeDetails { get; set; } = false;
        
        /// <summary>
        /// 状态过滤器
        /// </summary>
        [JsonPropertyName("statusFilter")]
        public ConnectionStatus? StatusFilter { get; set; }
        
        /// <summary>
        /// 分页大小
        /// </summary>
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; } = 50;
        
        /// <summary>
        /// 页码（从0开始）
        /// </summary>
        [JsonPropertyName("pageIndex")]
        public int PageIndex { get; set; } = 0;
    }
    
    /// <summary>
    /// 连接列表响应数据模型
    /// </summary>
    public class ListConnectionsResponseData
    {
        /// <summary>
        /// 连接列表
        /// </summary>
        [JsonPropertyName("connections")]
        public List<ConnectionSummary> Connections { get; set; } = new();
        
        /// <summary>
        /// 总连接数
        /// </summary>
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        
        /// <summary>
        /// 活跃连接数
        /// </summary>
        [JsonPropertyName("activeCount")]
        public int ActiveCount { get; set; }
        
        /// <summary>
        /// 分页信息
        /// </summary>
        [JsonPropertyName("pagination")]
        public PaginationInfo Pagination { get; set; } = new();
    }
    
    /// <summary>
    /// 连接摘要信息
    /// </summary>
    public class ConnectionSummary
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 连接状态
        /// </summary>
        [JsonPropertyName("status")]
        public ConnectionStatus Status { get; set; }
        
        /// <summary>
        /// 设备名称
        /// </summary>
        [JsonPropertyName("deviceName")]
        public string DeviceName { get; set; } = string.Empty;
        
        /// <summary>
        /// 设备类型
        /// </summary>
        [JsonPropertyName("deviceType")]
        public string DeviceType { get; set; } = string.Empty;
        
        /// <summary>
        /// 连接建立时间
        /// </summary>
        [JsonPropertyName("connectedAt")]
        public DateTime? ConnectedAt { get; set; }
        
        /// <summary>
        /// 最后活动时间
        /// </summary>
        [JsonPropertyName("lastActivity")]
        public DateTime? LastActivity { get; set; }
        
        /// <summary>
        /// 数据点数量
        /// </summary>
        [JsonPropertyName("dataPointsCount")]
        public int DataPointsCount { get; set; }
        
        /// <summary>
        /// 详细信息（可选）
        /// </summary>
        [JsonPropertyName("details")]
        public StatusResponseData? Details { get; set; }
    }
    
    /// <summary>
    /// 已连接设备信息
    /// </summary>
    public class ConnectedDeviceInfo
    {
        /// <summary>
        /// 设备名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;
        
        /// <summary>
        /// 设备类型
        /// </summary>
        [JsonPropertyName("type")]
        public string Type { get; set; } = string.Empty;
        
        /// <summary>
        /// 设备制造商
        /// </summary>
        [JsonPropertyName("manufacturer")]
        public string Manufacturer { get; set; } = string.Empty;
        
        /// <summary>
        /// 设备型号
        /// </summary>
        [JsonPropertyName("model")]
        public string Model { get; set; } = string.Empty;
        
        /// <summary>
        /// 固件版本
        /// </summary>
        [JsonPropertyName("firmwareVersion")]
        public string FirmwareVersion { get; set; } = string.Empty;
        
        /// <summary>
        /// 设备地址
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
        
        /// <summary>
        /// 端口
        /// </summary>
        [JsonPropertyName("port")]
        public int Port { get; set; }
        
        /// <summary>
        /// 协议版本
        /// </summary>
        [JsonPropertyName("protocolVersion")]
        public string ProtocolVersion { get; set; } = string.Empty;
    }
    
    /// <summary>
    /// 连接统计信息
    /// </summary>
    public class ConnectionStatistics
    {
        /// <summary>
        /// 总消息数
        /// </summary>
        [JsonPropertyName("totalMessages")]
        public long TotalMessages { get; set; }
        
        /// <summary>
        /// 成功读取次数
        /// </summary>
        [JsonPropertyName("successfulReads")]
        public long SuccessfulReads { get; set; }
        
        /// <summary>
        /// 失败读取次数
        /// </summary>
        [JsonPropertyName("failedReads")]
        public long FailedReads { get; set; }
        
        /// <summary>
        /// 成功写入次数
        /// </summary>
        [JsonPropertyName("successfulWrites")]
        public long SuccessfulWrites { get; set; }
        
        /// <summary>
        /// 失败写入次数
        /// </summary>
        [JsonPropertyName("failedWrites")]
        public long FailedWrites { get; set; }
        
        /// <summary>
        /// 平均响应时间（毫秒）
        /// </summary>
        [JsonPropertyName("averageResponseTimeMs")]
        public double AverageResponseTimeMs { get; set; }
        
        /// <summary>
        /// 最后错误时间
        /// </summary>
        [JsonPropertyName("lastErrorTime")]
        public DateTime? LastErrorTime { get; set; }
        
        /// <summary>
        /// 最后错误信息
        /// </summary>
        [JsonPropertyName("lastError")]
        public string? LastError { get; set; }
    }
    
    /// <summary>
    /// 连接选项
    /// </summary>
    public class ConnectionOptions
    {
        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        [JsonPropertyName("timeoutMs")]
        public int TimeoutMs { get; set; } = 5000;
        
        /// <summary>
        /// 重试次数
        /// </summary>
        [JsonPropertyName("retryCount")]
        public int RetryCount { get; set; } = 3;
        
        /// <summary>
        /// 重试间隔（毫秒）
        /// </summary>
        [JsonPropertyName("retryIntervalMs")]
        public int RetryIntervalMs { get; set; } = 1000;
        
        /// <summary>
        /// 是否启用自动重连
        /// </summary>
        [JsonPropertyName("autoReconnect")]
        public bool AutoReconnect { get; set; } = true;
        
        /// <summary>
        /// 自动重连间隔（毫秒）
        /// </summary>
        [JsonPropertyName("autoReconnectIntervalMs")]
        public int AutoReconnectIntervalMs { get; set; } = 10000;
        
        /// <summary>
        /// 连接标签（用于识别）
        /// </summary>
        [JsonPropertyName("tags")]
        public Dictionary<string, string> Tags { get; set; } = new();
    }
    
    /// <summary>
    /// 分页信息
    /// </summary>
    public class PaginationInfo
    {
        /// <summary>
        /// 当前页码（从0开始）
        /// </summary>
        [JsonPropertyName("pageIndex")]
        public int PageIndex { get; set; }
        
        /// <summary>
        /// 页面大小
        /// </summary>
        [JsonPropertyName("pageSize")]
        public int PageSize { get; set; }
        
        /// <summary>
        /// 总页数
        /// </summary>
        [JsonPropertyName("totalPages")]
        public int TotalPages { get; set; }
        
        /// <summary>
        /// 总记录数
        /// </summary>
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        
        /// <summary>
        /// 是否有下一页
        /// </summary>
        [JsonPropertyName("hasNextPage")]
        public bool HasNextPage { get; set; }
        
        /// <summary>
        /// 是否有上一页
        /// </summary>
        [JsonPropertyName("hasPreviousPage")]
        public bool HasPreviousPage { get; set; }
    }
}