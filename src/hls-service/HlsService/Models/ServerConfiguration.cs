using System.ComponentModel.DataAnnotations;

namespace HlsService.Models
{
    /// <summary>
    /// 服务器配置模型
    /// </summary>
    public class ServerConfiguration
    {
        /// <summary>
        /// 监听端口
        /// </summary>
        [Range(1024, 65535)]
        public int Port { get; set; } = 8888;

        /// <summary>
        /// 监听IP地址（默认本地回环）
        /// </summary>
        public string Host { get; set; } = "127.0.0.1";

        /// <summary>
        /// 最大并发连接数
        /// </summary>
        [Range(1, 1000)]
        public int MaxConnections { get; set; } = 50;

        /// <summary>
        /// 连接超时时间（毫秒）
        /// </summary>
        [Range(1000, 300000)]
        public int ConnectionTimeoutMs { get; set; } = 30000;

        /// <summary>
        /// 读取超时时间（毫秒）
        /// </summary>
        [Range(100, 10000)]
        public int ReadTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 写入超时时间（毫秒）
        /// </summary>
        [Range(100, 10000)]
        public int WriteTimeoutMs { get; set; } = 5000;

        /// <summary>
        /// 缓冲区大小
        /// </summary>
        [Range(1024, 1048576)]
        public int BufferSize { get; set; } = 8192;

        /// <summary>
        /// 是否启用详细日志
        /// </summary>
        public bool EnableVerboseLogging { get; set; } = false;

        /// <summary>
        /// 心跳检测间隔（毫秒）
        /// </summary>
        [Range(1000, 60000)]
        public int HeartbeatIntervalMs { get; set; } = 10000;

        /// <summary>
        /// 协议版本
        /// </summary>
        public string ProtocolVersion { get; set; } = "1.0";
    }
}