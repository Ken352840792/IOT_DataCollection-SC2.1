using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HlsService.Models
{
    /// <summary>
    /// IPC通信请求消息模型
    /// </summary>
    public class IpcRequest
    {
        /// <summary>
        /// 消息唯一标识符
        /// </summary>
        [Required]
        [JsonPropertyName("messageId")]
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// 协议版本
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 命令类型
        /// </summary>
        [Required]
        [JsonPropertyName("command")]
        public string Command { get; set; } = string.Empty;

        /// <summary>
        /// 请求数据
        /// </summary>
        [JsonPropertyName("data")]
        public object? Data { get; set; }

        /// <summary>
        /// 时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// IPC通信响应消息模型
    /// </summary>
    public class IpcResponse
    {
        /// <summary>
        /// 消息唯一标识符（与请求匹配）
        /// </summary>
        [JsonPropertyName("messageId")]
        public string MessageId { get; set; } = string.Empty;

        /// <summary>
        /// 协议版本
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 处理是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 响应数据
        /// </summary>
        [JsonPropertyName("data")]
        public object? Data { get; set; }

        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        [JsonPropertyName("error")]
        public string? Error { get; set; }

        /// <summary>
        /// 响应时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 处理耗时（毫秒）
        /// </summary>
        [JsonPropertyName("processingTimeMs")]
        public double ProcessingTimeMs { get; set; }
    }

    /// <summary>
    /// 服务器状态信息
    /// </summary>
    public class ServerStatus
    {
        [JsonPropertyName("status")]
        public string Status { get; set; } = "running";

        [JsonPropertyName("uptime")]
        public TimeSpan Uptime { get; set; }

        [JsonPropertyName("activeConnections")]
        public int ActiveConnections { get; set; }

        [JsonPropertyName("totalConnections")]
        public long TotalConnections { get; set; }

        [JsonPropertyName("messagesProcessed")]
        public long MessagesProcessed { get; set; }

        [JsonPropertyName("hslCommunicationVersion")]
        public string HslCommunicationVersion { get; set; } = "12.3.3";

        [JsonPropertyName("dotnetVersion")]
        public string DotNetVersion { get; set; } = Environment.Version.ToString();

        [JsonPropertyName("memoryUsageMB")]
        public double MemoryUsageMB { get; set; }

        [JsonPropertyName("processId")]
        public int ProcessId { get; set; } = Environment.ProcessId;
    }

    /// <summary>
    /// 客户端连接信息
    /// </summary>
    public class ClientInfo
    {
        [JsonPropertyName("clientId")]
        public string ClientId { get; set; } = string.Empty;

        [JsonPropertyName("remoteEndpoint")]
        public string RemoteEndpoint { get; set; } = string.Empty;

        [JsonPropertyName("connectedTime")]
        public DateTime ConnectedTime { get; set; }

        [JsonPropertyName("lastActivity")]
        public DateTime LastActivity { get; set; }

        [JsonPropertyName("messageCount")]
        public long MessageCount { get; set; }

        [JsonPropertyName("isActive")]
        public bool IsActive { get; set; }
    }
}