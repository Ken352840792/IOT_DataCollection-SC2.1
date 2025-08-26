using System.Text.Json.Serialization;
using System.Net.Sockets;

namespace HlsService.Models
{
    /// <summary>
    /// 标准化错误响应模型
    /// </summary>
    public class ErrorResponse
    {
        /// <summary>
        /// 错误代码
        /// </summary>
        [JsonPropertyName("code")]
        public string Code { get; set; } = string.Empty;

        /// <summary>
        /// 错误消息
        /// </summary>
        [JsonPropertyName("message")]
        public string Message { get; set; } = string.Empty;

        /// <summary>
        /// 详细错误信息
        /// </summary>
        [JsonPropertyName("details")]
        public List<string> Details { get; set; } = new();

        /// <summary>
        /// 错误发生时间
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 错误类型
        /// </summary>
        [JsonPropertyName("type")]
        public ErrorType Type { get; set; } = ErrorType.Unknown;

        /// <summary>
        /// 是否可重试
        /// </summary>
        [JsonPropertyName("retryable")]
        public bool Retryable { get; set; } = false;

        /// <summary>
        /// 建议的重试延迟（毫秒）
        /// </summary>
        [JsonPropertyName("retryDelayMs")]
        public int? RetryDelayMs { get; set; }

        /// <summary>
        /// 相关资源ID
        /// </summary>
        [JsonPropertyName("resourceId")]
        public string? ResourceId { get; set; }
    }

    /// <summary>
    /// 错误类型枚举
    /// </summary>
    public enum ErrorType
    {
        Unknown,
        Validation,
        Network,
        Timeout,
        Authentication,
        Authorization,
        NotFound,
        Conflict,
        Internal,
        Configuration
    }

    /// <summary>
    /// 标准化错误代码常量（按类别组织）
    /// </summary>
    public static class ErrorCodes
    {
        // 系统级错误 (1xxx)
        public const string SYSTEM_ERROR = "1000";
        public const string INVALID_REQUEST = "1001";
        public const string INVALID_PARAMETER = "1002";
        public const string PERMISSION_DENIED = "1003";
        public const string MISSING_PARAMETER = "1004";
        public const string INVALID_FORMAT = "1005";
        public const string INVALID_RANGE = "1006";
        public const string RESOURCE_EXHAUSTED = "1007";
        public const string SERVICE_UNAVAILABLE = "1008";
        public const string OPERATION_NOT_SUPPORTED = "1009";

        // 设备连接错误 (2xxx)
        public const string DEVICE_NOT_FOUND = "2001";
        public const string DEVICE_OFFLINE = "2002";
        public const string CONNECTION_TIMEOUT = "2003";
        public const string CONNECTION_FAILED = "2004";
        public const string DEVICE_ALREADY_EXISTS = "2005";
        public const string DEVICE_NOT_CONNECTED = "2006";
        public const string DEVICE_PROTOCOL_ERROR = "2007";
        public const string NETWORK_UNREACHABLE = "2008";
        public const string CONNECTION_REFUSED = "2009";
        public const string SOCKET_ERROR = "2010";

        // 数据操作错误 (3xxx)
        public const string INVALID_ADDRESS = "3001";
        public const string DATA_TYPE_ERROR = "3002";
        public const string READ_TIMEOUT = "3003";
        public const string WRITE_FAILED = "3004";
        public const string READ_PERMISSION_DENIED = "3005";
        public const string WRITE_PERMISSION_DENIED = "3006";
        public const string DATA_CONVERSION_ERROR = "3007";
        public const string UNSUPPORTED_DATA_TYPE = "3008";
        public const string DATA_OUT_OF_RANGE = "3009";

        // 配置相关错误 (4xxx)
        public const string INVALID_CONFIGURATION = "4001";
        public const string CONFIGURATION_NOT_FOUND = "4002";
        public const string CONFIGURATION_CONFLICT = "4003";

        // IPC通信错误 (5xxx)
        public const string INVALID_MESSAGE_FORMAT = "5001";
        public const string MESSAGE_TOO_LARGE = "5002";
        public const string COMMAND_NOT_FOUND = "5003";
        public const string UNSUPPORTED_PROTOCOL_VERSION = "5004";
        public const string MESSAGE_ID_INVALID = "5005";
        public const string MESSAGE_TIMESTAMP_INVALID = "5006";

        // 向后兼容的字符串错误码
        public const string LEGACY_INVALID_PARAMETER = "INVALID_PARAMETER";
        public const string LEGACY_MISSING_PARAMETER = "MISSING_PARAMETER";
        public const string LEGACY_DEVICE_NOT_FOUND = "DEVICE_NOT_FOUND";
        public const string LEGACY_CONNECTION_FAILED = "DEVICE_CONNECTION_FAILED";
        public const string LEGACY_INTERNAL_ERROR = "INTERNAL_ERROR";

        /// <summary>
        /// 获取错误码的标准描述
        /// </summary>
        public static string GetErrorDescription(string errorCode)
        {
            return errorCode switch
            {
                // 系统级错误
                SYSTEM_ERROR => "系统内部错误",
                INVALID_REQUEST => "无效的请求",
                INVALID_PARAMETER => "参数无效",
                PERMISSION_DENIED => "权限拒绝",
                MISSING_PARAMETER => "缺少必需参数",
                INVALID_FORMAT => "格式无效",
                INVALID_RANGE => "数值超出有效范围",
                RESOURCE_EXHAUSTED => "系统资源耗尽",
                SERVICE_UNAVAILABLE => "服务不可用",
                OPERATION_NOT_SUPPORTED => "操作不支持",

                // 设备连接错误
                DEVICE_NOT_FOUND => "设备未找到",
                DEVICE_OFFLINE => "设备离线",
                CONNECTION_TIMEOUT => "连接超时",
                CONNECTION_FAILED => "连接失败",
                DEVICE_ALREADY_EXISTS => "设备已存在",
                DEVICE_NOT_CONNECTED => "设备未连接",
                DEVICE_PROTOCOL_ERROR => "设备通信协议错误",
                NETWORK_UNREACHABLE => "网络不可达",
                CONNECTION_REFUSED => "连接被拒绝",
                SOCKET_ERROR => "套接字错误",

                // 数据操作错误
                INVALID_ADDRESS => "地址无效",
                DATA_TYPE_ERROR => "数据类型错误",
                READ_TIMEOUT => "读取超时",
                WRITE_FAILED => "写入失败",
                READ_PERMISSION_DENIED => "读取权限拒绝",
                WRITE_PERMISSION_DENIED => "写入权限拒绝",
                DATA_CONVERSION_ERROR => "数据转换错误",
                UNSUPPORTED_DATA_TYPE => "不支持的数据类型",
                DATA_OUT_OF_RANGE => "数据超出范围",

                // 配置相关错误
                INVALID_CONFIGURATION => "配置无效",
                CONFIGURATION_NOT_FOUND => "配置未找到",
                CONFIGURATION_CONFLICT => "配置冲突",

                // IPC通信错误
                INVALID_MESSAGE_FORMAT => "消息格式无效",
                MESSAGE_TOO_LARGE => "消息过大",
                COMMAND_NOT_FOUND => "命令未找到",
                UNSUPPORTED_PROTOCOL_VERSION => "不支持的协议版本",
                MESSAGE_ID_INVALID => "消息ID无效",
                MESSAGE_TIMESTAMP_INVALID => "消息时间戳无效",

                _ => "未知错误"
            };
        }

        /// <summary>
        /// 检查错误码是否可重试
        /// </summary>
        public static bool IsRetryable(string errorCode)
        {
            return errorCode switch
            {
                CONNECTION_TIMEOUT or
                CONNECTION_FAILED or
                READ_TIMEOUT or
                WRITE_FAILED or
                NETWORK_UNREACHABLE or
                SERVICE_UNAVAILABLE or
                SYSTEM_ERROR => true,
                _ => false
            };
        }

        /// <summary>
        /// 获取建议的重试延迟时间（毫秒）
        /// </summary>
        public static int? GetRetryDelay(string errorCode)
        {
            return errorCode switch
            {
                CONNECTION_TIMEOUT => 5000,
                CONNECTION_FAILED => 3000,
                READ_TIMEOUT => 2000,
                WRITE_FAILED => 1000,
                NETWORK_UNREACHABLE => 10000,
                SERVICE_UNAVAILABLE => 5000,
                SYSTEM_ERROR => 1000,
                _ => null
            };
        }
    }

    /// <summary>
    /// 错误工厂类，用于创建标准化的错误响应
    /// </summary>
    public static class ErrorFactory
    {
        /// <summary>
        /// 创建参数验证错误
        /// </summary>
        public static ErrorResponse CreateValidationError(string parameter, string message)
        {
            return new ErrorResponse
            {
                Code = ErrorCodes.INVALID_PARAMETER,
                Message = ErrorCodes.GetErrorDescription(ErrorCodes.INVALID_PARAMETER),
                Details = new List<string> { $"Parameter: {parameter}", message },
                Type = ErrorType.Validation,
                Retryable = ErrorCodes.IsRetryable(ErrorCodes.INVALID_PARAMETER),
                RetryDelayMs = ErrorCodes.GetRetryDelay(ErrorCodes.INVALID_PARAMETER),
                ResourceId = parameter
            };
        }

        /// <summary>
        /// 创建参数缺失错误
        /// </summary>
        public static ErrorResponse CreateMissingParameterError(string parameter)
        {
            return new ErrorResponse
            {
                Code = ErrorCodes.MISSING_PARAMETER,
                Message = ErrorCodes.GetErrorDescription(ErrorCodes.MISSING_PARAMETER),
                Details = new List<string> { $"Parameter: {parameter}" },
                Type = ErrorType.Validation,
                Retryable = ErrorCodes.IsRetryable(ErrorCodes.MISSING_PARAMETER),
                RetryDelayMs = ErrorCodes.GetRetryDelay(ErrorCodes.MISSING_PARAMETER),
                ResourceId = parameter
            };
        }

        /// <summary>
        /// 创建设备未找到错误
        /// </summary>
        public static ErrorResponse CreateDeviceNotFoundError(string deviceId)
        {
            return new ErrorResponse
            {
                Code = ErrorCodes.DEVICE_NOT_FOUND,
                Message = ErrorCodes.GetErrorDescription(ErrorCodes.DEVICE_NOT_FOUND),
                Details = new List<string> { $"Device ID: {deviceId}" },
                Type = ErrorType.NotFound,
                Retryable = ErrorCodes.IsRetryable(ErrorCodes.DEVICE_NOT_FOUND),
                RetryDelayMs = ErrorCodes.GetRetryDelay(ErrorCodes.DEVICE_NOT_FOUND),
                ResourceId = deviceId
            };
        }

        /// <summary>
        /// 创建设备连接失败错误
        /// </summary>
        public static ErrorResponse CreateDeviceConnectionError(string deviceId, string reason)
        {
            return new ErrorResponse
            {
                Code = ErrorCodes.CONNECTION_FAILED,
                Message = ErrorCodes.GetErrorDescription(ErrorCodes.CONNECTION_FAILED),
                Details = new List<string> { $"Device ID: {deviceId}", reason },
                Type = ErrorType.Network,
                Retryable = ErrorCodes.IsRetryable(ErrorCodes.CONNECTION_FAILED),
                RetryDelayMs = ErrorCodes.GetRetryDelay(ErrorCodes.CONNECTION_FAILED),
                ResourceId = deviceId
            };
        }

        /// <summary>
        /// 创建网络超时错误
        /// </summary>
        public static ErrorResponse CreateTimeoutError(string operation, int timeoutMs)
        {
            return new ErrorResponse
            {
                Code = ErrorCodes.CONNECTION_TIMEOUT,
                Message = ErrorCodes.GetErrorDescription(ErrorCodes.CONNECTION_TIMEOUT),
                Details = new List<string> { $"Operation: {operation}", $"Timeout: {timeoutMs}ms" },
                Type = ErrorType.Timeout,
                Retryable = ErrorCodes.IsRetryable(ErrorCodes.CONNECTION_TIMEOUT),
                RetryDelayMs = ErrorCodes.GetRetryDelay(ErrorCodes.CONNECTION_TIMEOUT),
                ResourceId = operation
            };
        }

        /// <summary>
        /// 创建数据转换错误
        /// </summary>
        public static ErrorResponse CreateDataConversionError(string address, string fromType, string toType, string reason = "")
        {
            var details = new List<string> 
            { 
                $"Address: {address}", 
                $"From: {fromType} To: {toType}" 
            };
            if (!string.IsNullOrEmpty(reason))
                details.Add(reason);

            return new ErrorResponse
            {
                Code = ErrorCodes.DATA_CONVERSION_ERROR,
                Message = ErrorCodes.GetErrorDescription(ErrorCodes.DATA_CONVERSION_ERROR),
                Details = details,
                Type = ErrorType.Internal,
                Retryable = ErrorCodes.IsRetryable(ErrorCodes.DATA_CONVERSION_ERROR),
                RetryDelayMs = ErrorCodes.GetRetryDelay(ErrorCodes.DATA_CONVERSION_ERROR),
                ResourceId = address
            };
        }

        /// <summary>
        /// 创建权限拒绝错误
        /// </summary>
        public static ErrorResponse CreatePermissionDeniedError(string operation, string resource)
        {
            var errorCode = operation.ToLower().Contains("read") ? 
                ErrorCodes.READ_PERMISSION_DENIED : ErrorCodes.WRITE_PERMISSION_DENIED;

            return new ErrorResponse
            {
                Code = errorCode,
                Message = ErrorCodes.GetErrorDescription(errorCode),
                Details = new List<string> { $"Operation: {operation}", $"Resource: {resource}" },
                Type = ErrorType.Authorization,
                Retryable = ErrorCodes.IsRetryable(errorCode),
                RetryDelayMs = ErrorCodes.GetRetryDelay(errorCode),
                ResourceId = resource
            };
        }

        /// <summary>
        /// 创建配置错误
        /// </summary>
        public static ErrorResponse CreateConfigurationError(string configType, List<string> validationErrors)
        {
            var details = new List<string> { $"Configuration type: {configType}" };
            details.AddRange(validationErrors);

            return new ErrorResponse
            {
                Code = ErrorCodes.INVALID_CONFIGURATION,
                Message = ErrorCodes.GetErrorDescription(ErrorCodes.INVALID_CONFIGURATION),
                Details = details,
                Type = ErrorType.Configuration,
                Retryable = ErrorCodes.IsRetryable(ErrorCodes.INVALID_CONFIGURATION),
                RetryDelayMs = ErrorCodes.GetRetryDelay(ErrorCodes.INVALID_CONFIGURATION)
            };
        }

        /// <summary>
        /// 创建内部服务器错误
        /// </summary>
        public static ErrorResponse CreateInternalError(string message, Exception? exception = null)
        {
            var details = new List<string> { message };
            if (exception != null)
            {
                details.Add($"Exception: {exception.GetType().Name}");
                if (!string.IsNullOrEmpty(exception.Message))
                    details.Add($"Exception Message: {exception.Message}");
            }

            return new ErrorResponse
            {
                Code = ErrorCodes.SYSTEM_ERROR,
                Message = ErrorCodes.GetErrorDescription(ErrorCodes.SYSTEM_ERROR),
                Details = details,
                Type = ErrorType.Internal,
                Retryable = ErrorCodes.IsRetryable(ErrorCodes.SYSTEM_ERROR),
                RetryDelayMs = ErrorCodes.GetRetryDelay(ErrorCodes.SYSTEM_ERROR)
            };
        }

        /// <summary>
        /// 从异常创建错误响应
        /// </summary>
        public static ErrorResponse FromException(Exception exception, string? context = null)
        {
            return exception switch
            {
                ArgumentNullException argEx => CreateMissingParameterError(argEx.ParamName ?? "unknown"),
                ArgumentException argEx => CreateValidationError(argEx.ParamName ?? "unknown", argEx.Message),
                TimeoutException => CreateTimeoutError(context ?? "operation", 5000),
                UnauthorizedAccessException => CreatePermissionDeniedError("access", context ?? "resource"),
                KeyNotFoundException => CreateDeviceNotFoundError(context ?? "unknown"),
                SocketException => new ErrorResponse
                {
                    Code = ErrorCodes.SOCKET_ERROR,
                    Message = ErrorCodes.GetErrorDescription(ErrorCodes.SOCKET_ERROR),
                    Details = new List<string> { exception.Message },
                    Type = ErrorType.Network,
                    Retryable = true,
                    RetryDelayMs = 3000,
                    ResourceId = context
                },
                _ => CreateInternalError(exception.Message, exception)
            };
        }
    }

    /// <summary>
    /// 操作结果包装类
    /// </summary>
    /// <typeparam name="T">结果数据类型</typeparam>
    public class OperationResult<T>
    {
        /// <summary>
        /// 是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }

        /// <summary>
        /// 结果数据
        /// </summary>
        [JsonPropertyName("data")]
        public T? Data { get; set; }

        /// <summary>
        /// 错误信息
        /// </summary>
        [JsonPropertyName("error")]
        public ErrorResponse? Error { get; set; }

        /// <summary>
        /// 操作时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 创建成功结果
        /// </summary>
        public static OperationResult<T> CreateSuccess(T data)
        {
            return new OperationResult<T>
            {
                Success = true,
                Data = data
            };
        }

        /// <summary>
        /// 创建失败结果
        /// </summary>
        public static OperationResult<T> CreateFailure(ErrorResponse error)
        {
            return new OperationResult<T>
            {
                Success = false,
                Error = error
            };
        }

        /// <summary>
        /// 创建失败结果（从异常）
        /// </summary>
        public static OperationResult<T> CreateFailure(Exception exception, string? context = null)
        {
            return new OperationResult<T>
            {
                Success = false,
                Error = ErrorFactory.FromException(exception, context)
            };
        }
    }
}