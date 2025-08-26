using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HlsService.Models
{
    /// <summary>
    /// 协议版本管理
    /// </summary>
    public static class ProtocolVersion
    {
        /// <summary>
        /// 当前支持的协议版本
        /// </summary>
        public const string CURRENT = "1.0";
        
        /// <summary>
        /// 支持的协议版本列表
        /// </summary>
        public static readonly string[] SUPPORTED_VERSIONS = { "1.0" };
        
        /// <summary>
        /// 检查版本兼容性
        /// </summary>
        public static bool IsVersionSupported(string version)
        {
            return SUPPORTED_VERSIONS.Contains(version);
        }
        
        /// <summary>
        /// 获取版本信息
        /// </summary>
        public static ProtocolVersionInfo GetVersionInfo()
        {
            return new ProtocolVersionInfo
            {
                Current = CURRENT,
                Supported = SUPPORTED_VERSIONS.ToList(),
                Features = GetVersionFeatures(CURRENT)
            };
        }
        
        /// <summary>
        /// 获取版本特性
        /// </summary>
        private static List<string> GetVersionFeatures(string version)
        {
            return version switch
            {
                "1.0" => new List<string>
                {
                    "Basic device connection management",
                    "Data read/write operations",
                    "Batch operations support",
                    "Error handling and status codes",
                    "Message validation",
                    "Connection status monitoring"
                },
                _ => new List<string>()
            };
        }
    }
    
    /// <summary>
    /// 协议版本信息
    /// </summary>
    public class ProtocolVersionInfo
    {
        [JsonPropertyName("current")]
        public string Current { get; set; } = string.Empty;
        
        [JsonPropertyName("supported")]
        public List<string> Supported { get; set; } = new();
        
        [JsonPropertyName("features")]
        public List<string> Features { get; set; } = new();
    }
    
    /// <summary>
    /// 支持的命令类型
    /// </summary>
    public static class CommandTypes
    {
        // 连接管理命令
        public const string CONNECT = "connect";
        public const string DISCONNECT = "disconnect";
        public const string STATUS = "status";
        public const string LIST_CONNECTIONS = "listConnections";
        
        // 数据操作命令
        public const string READ = "read";
        public const string WRITE = "write";
        public const string READ_BATCH = "readBatch";
        public const string WRITE_BATCH = "writeBatch";
        
        // 服务器管理命令
        public const string SERVER_STATUS = "serverStatus";
        public const string PROTOCOL_INFO = "protocolInfo";
        
        /// <summary>
        /// 获取所有支持的命令
        /// </summary>
        public static readonly string[] ALL_COMMANDS = {
            CONNECT, DISCONNECT, STATUS, LIST_CONNECTIONS,
            READ, WRITE, READ_BATCH, WRITE_BATCH,
            SERVER_STATUS, PROTOCOL_INFO
        };
        
        /// <summary>
        /// 验证命令是否支持
        /// </summary>
        public static bool IsValidCommand(string command)
        {
            return ALL_COMMANDS.Contains(command, StringComparer.OrdinalIgnoreCase);
        }
        
        /// <summary>
        /// 获取命令描述
        /// </summary>
        public static string GetCommandDescription(string command)
        {
            return command.ToLower() switch
            {
                CONNECT => "建立设备连接",
                DISCONNECT => "断开设备连接", 
                STATUS => "查询连接状态",
                LIST_CONNECTIONS => "列出所有连接",
                READ => "读取单个数据点",
                WRITE => "写入单个数据点",
                READ_BATCH => "批量读取数据点",
                WRITE_BATCH => "批量写入数据点",
                SERVER_STATUS => "获取服务器状态",
                PROTOCOL_INFO => "获取协议信息",
                _ => "未知命令"
            };
        }
    }
    
    /// <summary>
    /// 消息验证器
    /// </summary>
    public static class MessageValidator
    {
        /// <summary>
        /// 验证IPC请求消息
        /// </summary>
        public static ValidationResult ValidateRequest(IpcRequest request)
        {
            var result = new ValidationResult();
            
            // 验证必要字段
            if (string.IsNullOrEmpty(request.MessageId))
            {
                result.Errors.Add("MessageId is required");
            }
            else if (!IsValidMessageId(request.MessageId))
            {
                result.Errors.Add("MessageId format is invalid");
            }
            
            if (string.IsNullOrEmpty(request.Command))
            {
                result.Errors.Add("Command is required");
            }
            else if (!CommandTypes.IsValidCommand(request.Command))
            {
                result.Errors.Add($"Unsupported command: {request.Command}");
            }
            
            if (string.IsNullOrEmpty(request.Version))
            {
                result.Errors.Add("Version is required");
            }
            else if (!ProtocolVersion.IsVersionSupported(request.Version))
            {
                result.Errors.Add($"Unsupported protocol version: {request.Version}");
            }
            
            // 验证时间戳
            if (request.Timestamp == default)
            {
                result.Errors.Add("Timestamp is required");
            }
            else if (Math.Abs((DateTime.UtcNow - request.Timestamp).TotalMinutes) > 5)
            {
                result.Errors.Add("Timestamp is too old or in future");
            }
            
            result.IsValid = result.Errors.Count == 0;
            return result;
        }
        
        /// <summary>
        /// 验证消息ID格式
        /// </summary>
        private static bool IsValidMessageId(string messageId)
        {
            // 支持UUID和自定义格式
            return Guid.TryParse(messageId, out _) || 
                   (messageId.Length >= 8 && messageId.Length <= 64 && 
                    messageId.All(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
        }
        
        /// <summary>
        /// 验证连接配置
        /// </summary>
        public static ValidationResult ValidateConnectionConfig(object? configData)
        {
            var result = new ValidationResult();
            
            if (configData == null)
            {
                result.Errors.Add("Connection configuration is required");
                result.IsValid = false;
                return result;
            }
            
            // 这里可以添加更多的连接配置验证逻辑
            // 具体验证逻辑会在设备连接实现时添加
            
            result.IsValid = result.Errors.Count == 0;
            return result;
        }
    }
    
    /// <summary>
    /// 验证结果
    /// </summary>
    public class ValidationResult
    {
        [JsonPropertyName("isValid")]
        public bool IsValid { get; set; }
        
        [JsonPropertyName("errors")]
        public List<string> Errors { get; set; } = new();
        
        [JsonPropertyName("warnings")]
        public List<string> Warnings { get; set; } = new();
    }
    
    /// <summary>
    /// 消息ID生成器
    /// </summary>
    public static class MessageIdGenerator
    {
        private static readonly Random _random = new();
        
        /// <summary>
        /// 生成UUID格式的消息ID
        /// </summary>
        public static string GenerateUuid()
        {
            return Guid.NewGuid().ToString();
        }
        
        /// <summary>
        /// 生成简短格式的消息ID
        /// </summary>
        public static string GenerateShort(string prefix = "msg")
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var randomSuffix = _random.Next(1000, 9999);
            return $"{prefix}_{timestamp}_{randomSuffix}";
        }
        
        /// <summary>
        /// 生成带时间戳的消息ID
        /// </summary>
        public static string GenerateWithTimestamp(string prefix = "")
        {
            var timestamp = DateTime.UtcNow.ToString("yyyyMMddHHmmssffff");
            var randomPart = _random.Next(100, 999);
            
            return string.IsNullOrEmpty(prefix) 
                ? $"{timestamp}_{randomPart}"
                : $"{prefix}_{timestamp}_{randomPart}";
        }
    }
    
    /// <summary>
    /// 协议常量定义
    /// </summary>
    public static class ProtocolConstants
    {
        /// <summary>
        /// 最大消息大小（字节）
        /// </summary>
        public const int MAX_MESSAGE_SIZE = 1024 * 1024; // 1MB
        
        /// <summary>
        /// 批量操作最大数据点数量
        /// </summary>
        public const int MAX_BATCH_SIZE = 100;
        
        /// <summary>
        /// 默认超时时间（毫秒）
        /// </summary>
        public const int DEFAULT_TIMEOUT_MS = 5000;
        
        /// <summary>
        /// 最大并发连接数
        /// </summary>
        public const int MAX_CONCURRENT_CONNECTIONS = 10;
        
        /// <summary>
        /// 消息ID最大长度
        /// </summary>
        public const int MAX_MESSAGE_ID_LENGTH = 64;
        
        /// <summary>
        /// 连接ID最大长度
        /// </summary>
        public const int MAX_CONNECTION_ID_LENGTH = 32;
    }
}