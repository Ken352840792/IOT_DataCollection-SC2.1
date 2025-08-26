using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HlsService.Models
{
    /// <summary>
    /// 设备类型枚举
    /// </summary>
    public enum DeviceType
    {
        ModbusTcp,
        ModbusRtu,
        SiemensS7,
        OmronFins,
        MitsubishiMC,
        Unknown
    }

    /// <summary>
    /// 连接状态枚举
    /// </summary>
    public enum ConnectionStatus
    {
        Disconnected,
        Connecting,
        Connected,
        Error,
        Timeout
    }

    /// <summary>
    /// 设备配置模型
    /// </summary>
    public class DeviceConfiguration
    {
        /// <summary>
        /// 设备唯一标识符
        /// </summary>
        [Required]
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 设备类型
        /// </summary>
        [JsonPropertyName("type")]
        public DeviceType Type { get; set; }

        /// <summary>
        /// 设备名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 设备描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 连接参数
        /// </summary>
        [JsonPropertyName("connection")]
        public ConnectionParameters Connection { get; set; } = new();

        /// <summary>
        /// 是否启用
        /// </summary>
        [JsonPropertyName("enabled")]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// 创建时间
        /// </summary>
        [JsonPropertyName("createdTime")]
        public DateTime CreatedTime { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// 连接参数
    /// </summary>
    public class ConnectionParameters
    {
        /// <summary>
        /// 主机地址（IP地址）
        /// </summary>
        [JsonPropertyName("host")]
        public string Host { get; set; } = string.Empty;

        /// <summary>
        /// 端口号
        /// </summary>
        [JsonPropertyName("port")]
        [Range(1, 65535)]
        public int Port { get; set; } = 502;

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        [JsonPropertyName("timeout")]
        [Range(1000, 30000)]
        public int TimeoutMs { get; set; } = 3000;

        /// <summary>
        /// 站号/从机地址
        /// </summary>
        [JsonPropertyName("station")]
        [Range(0, 255)]
        public byte Station { get; set; } = 1;

        /// <summary>
        /// 串口端口（用于串口通信）
        /// </summary>
        [JsonPropertyName("comPort")]
        public string ComPort { get; set; } = string.Empty;

        /// <summary>
        /// 波特率
        /// </summary>
        [JsonPropertyName("baudRate")]
        public int BaudRate { get; set; } = 9600;

        /// <summary>
        /// 数据位
        /// </summary>
        [JsonPropertyName("dataBits")]
        [Range(5, 8)]
        public int DataBits { get; set; } = 8;

        /// <summary>
        /// 停止位
        /// </summary>
        [JsonPropertyName("stopBits")]
        public int StopBits { get; set; } = 1;

        /// <summary>
        /// 校验位
        /// </summary>
        [JsonPropertyName("parity")]
        public string Parity { get; set; } = "None";

        /// <summary>
        /// 机架号（西门子S7）
        /// </summary>
        [JsonPropertyName("rack")]
        [Range(0, 7)]
        public int Rack { get; set; } = 0;

        /// <summary>
        /// 槽号（西门子S7）
        /// </summary>
        [JsonPropertyName("slot")]
        [Range(0, 31)]
        public int Slot { get; set; } = 2;

        // === Siemens S7特有参数 ===
        
        /// <summary>
        /// S7系列类型（S7-200, S7-300, S7-400, S7-1200, S7-1500等）
        /// </summary>
        [JsonPropertyName("s7Type")]
        public string? S7Type { get; set; } = "S7-1200";

        // === Omron FINS特有参数 ===
        
        /// <summary>
        /// 目标网络地址（Omron FINS）
        /// </summary>
        [JsonPropertyName("da1")]
        [Range(0, 255)]
        public int DA1 { get; set; } = 0x00;

        /// <summary>
        /// 目标节点地址（Omron FINS）
        /// </summary>
        [JsonPropertyName("da2")]
        [Range(0, 255)]
        public int DA2 { get; set; } = 0x00;

        /// <summary>
        /// 源网络地址（Omron FINS）
        /// </summary>
        [JsonPropertyName("sa1")]
        [Range(0, 255)]
        public int SA1 { get; set; } = 0x00;

        /// <summary>
        /// 源节点地址（Omron FINS）
        /// </summary>
        [JsonPropertyName("sa2")]
        [Range(0, 255)]
        public int SA2 { get; set; } = 0x00;

        // === Mitsubishi MC特有参数 ===
        
        /// <summary>
        /// 网络号（Mitsubishi MC）
        /// </summary>
        [JsonPropertyName("networkNumber")]
        [Range(0, 255)]
        public int NetworkNumber { get; set; } = 0x00;

        /// <summary>
        /// 网络站号（Mitsubishi MC）
        /// </summary>
        [JsonPropertyName("networkStationNumber")]
        [Range(0, 255)]
        public int NetworkStationNumber { get; set; } = 0x00;

        /// <summary>
        /// 扩展参数
        /// </summary>
        [JsonPropertyName("additionalParams")]
        public Dictionary<string, object> AdditionalParams { get; set; } = new();
    }

    /// <summary>
    /// 设备状态信息
    /// </summary>
    public class DeviceStatusInfo
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 连接状态
        /// </summary>
        [JsonPropertyName("status")]
        public ConnectionStatus Status { get; set; }

        /// <summary>
        /// 最后连接时间
        /// </summary>
        [JsonPropertyName("lastConnectedTime")]
        public DateTime? LastConnectedTime { get; set; }

        /// <summary>
        /// 最后通信时间
        /// </summary>
        [JsonPropertyName("lastCommunicationTime")]
        public DateTime? LastCommunicationTime { get; set; }

        /// <summary>
        /// 连接持续时间
        /// </summary>
        [JsonPropertyName("connectionDuration")]
        public TimeSpan? ConnectionDuration { get; set; }

        /// <summary>
        /// 错误计数
        /// </summary>
        [JsonPropertyName("errorCount")]
        public int ErrorCount { get; set; }

        /// <summary>
        /// 最后错误信息
        /// </summary>
        [JsonPropertyName("lastError")]
        public string? LastError { get; set; }

        /// <summary>
        /// 通信统计
        /// </summary>
        [JsonPropertyName("statistics")]
        public CommunicationStatistics Statistics { get; set; } = new();
    }

    /// <summary>
    /// 通信统计信息
    /// </summary>
    public class CommunicationStatistics
    {
        /// <summary>
        /// 总读取次数
        /// </summary>
        [JsonPropertyName("totalReads")]
        public long TotalReads { get; set; }

        /// <summary>
        /// 总写入次数
        /// </summary>
        [JsonPropertyName("totalWrites")]
        public long TotalWrites { get; set; }

        /// <summary>
        /// 成功读取次数
        /// </summary>
        [JsonPropertyName("successfulReads")]
        public long SuccessfulReads { get; set; }

        /// <summary>
        /// 成功写入次数
        /// </summary>
        [JsonPropertyName("successfulWrites")]
        public long SuccessfulWrites { get; set; }

        /// <summary>
        /// 平均响应时间（毫秒）
        /// </summary>
        [JsonPropertyName("averageResponseTimeMs")]
        public double AverageResponseTimeMs { get; set; }

        /// <summary>
        /// 最大响应时间（毫秒）
        /// </summary>
        [JsonPropertyName("maxResponseTimeMs")]
        public double MaxResponseTimeMs { get; set; }

        /// <summary>
        /// 数据传输字节数
        /// </summary>
        [JsonPropertyName("bytesTransferred")]
        public long BytesTransferred { get; set; }
    }

    /// <summary>
    /// 数据点读取请求
    /// </summary>
    public class DataPointReadRequest
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 数据点地址列表
        /// </summary>
        [JsonPropertyName("addresses")]
        public string[] Addresses { get; set; } = Array.Empty<string>();

        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = "int16";

        /// <summary>
        /// 批量读取大小
        /// </summary>
        [JsonPropertyName("batchSize")]
        public int BatchSize { get; set; } = 1;
    }

    /// <summary>
    /// 数据点写入请求
    /// </summary>
    public class DataPointWriteRequest
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 写入数据点
        /// </summary>
        [JsonPropertyName("dataPoints")]
        public WriteDataPoint[] DataPoints { get; set; } = Array.Empty<WriteDataPoint>();
    }

    /// <summary>
    /// 写入数据点
    /// </summary>
    public class WriteDataPoint
    {
        /// <summary>
        /// 地址
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 值
        /// </summary>
        [JsonPropertyName("value")]
        public object Value { get; set; } = new();

        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = "int16";
    }

    /// <summary>
    /// 读取结果
    /// </summary>
    public class ReadResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 值
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value { get; set; }

        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = string.Empty;

        /// <summary>
        /// 错误信息
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// 读取时间
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 响应时间（毫秒）
        /// </summary>
        [JsonPropertyName("responseTimeMs")]
        public double ResponseTimeMs { get; set; }
    }

    /// <summary>
    /// 写入结果
    /// </summary>
    public class WriteResult
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 地址
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 写入的值
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// 写入时间
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 响应时间（毫秒）
        /// </summary>
        [JsonPropertyName("responseTimeMs")]
        public double ResponseTimeMs { get; set; }
    }
}