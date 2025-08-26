using System.Text.Json.Serialization;

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
    /// 预定义的错误代码常量
    /// </summary>
    public static class ErrorCodes
    {
        // 参数验证错误
        public const string INVALID_PARAMETER = "INVALID_PARAMETER";
        public const string MISSING_PARAMETER = "MISSING_PARAMETER";
        public const string INVALID_FORMAT = "INVALID_FORMAT";
        public const string INVALID_RANGE = "INVALID_RANGE";

        // 设备相关错误
        public const string DEVICE_NOT_FOUND = "DEVICE_NOT_FOUND";
        public const string DEVICE_ALREADY_EXISTS = "DEVICE_ALREADY_EXISTS";
        public const string DEVICE_NOT_CONNECTED = "DEVICE_NOT_CONNECTED";
        public const string DEVICE_CONNECTION_FAILED = "DEVICE_CONNECTION_FAILED";
        public const string DEVICE_TIMEOUT = "DEVICE_TIMEOUT";
        public const string DEVICE_PROTOCOL_ERROR = "DEVICE_PROTOCOL_ERROR";

        // 网络相关错误
        public const string NETWORK_UNREACHABLE = "NETWORK_UNREACHABLE";
        public const string CONNECTION_REFUSED = "CONNECTION_REFUSED";
        public const string NETWORK_TIMEOUT = "NETWORK_TIMEOUT";
        public const string SOCKET_ERROR = "SOCKET_ERROR";

        // 数据点位相关错误
        public const string INVALID_ADDRESS = "INVALID_ADDRESS";
        public const string UNSUPPORTED_DATA_TYPE = "UNSUPPORTED_DATA_TYPE";
        public const string READ_PERMISSION_DENIED = "READ_PERMISSION_DENIED";
        public const string WRITE_PERMISSION_DENIED = "WRITE_PERMISSION_DENIED";
        public const string DATA_CONVERSION_ERROR = "DATA_CONVERSION_ERROR";

        // 配置相关错误
        public const string INVALID_CONFIGURATION = "INVALID_CONFIGURATION";
        public const string CONFIGURATION_NOT_FOUND = "CONFIGURATION_NOT_FOUND";
        public const string CONFIGURATION_CONFLICT = "CONFIGURATION_CONFLICT";

        // 系统相关错误
        public const string RESOURCE_EXHAUSTED = "RESOURCE_EXHAUSTED";
        public const string INTERNAL_ERROR = "INTERNAL_ERROR";
        public const string SERVICE_UNAVAILABLE = "SERVICE_UNAVAILABLE";
        public const string OPERATION_NOT_SUPPORTED = "OPERATION_NOT_SUPPORTED";

        // IPC通信错误
        public const string INVALID_MESSAGE_FORMAT = "INVALID_MESSAGE_FORMAT";
        public const string MESSAGE_TOO_LARGE = "MESSAGE_TOO_LARGE";
        public const string COMMAND_NOT_FOUND = "COMMAND_NOT_FOUND";
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
                Message = $"Invalid parameter: {parameter}",
                Details = new List<string> { message },
                Type = ErrorType.Validation,
                Retryable = false,
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
                Message = $"Required parameter is missing: {parameter}",
                Type = ErrorType.Validation,
                Retryable = false,
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
                Message = $"Device not found: {deviceId}",
                Type = ErrorType.NotFound,
                Retryable = false,
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
                Code = ErrorCodes.DEVICE_CONNECTION_FAILED,
                Message = $"Failed to connect to device: {deviceId}",
                Details = new List<string> { reason },
                Type = ErrorType.Network,
                Retryable = true,
                RetryDelayMs = 5000,
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
                Code = ErrorCodes.NETWORK_TIMEOUT,
                Message = $"Operation timed out: {operation}",
                Details = new List<string> { $"Timeout: {timeoutMs}ms" },
                Type = ErrorType.Timeout,
                Retryable = true,
                RetryDelayMs = Math.Min(timeoutMs, 10000)
            };
        }

        /// <summary>
        /// 创建数据转换错误
        /// </summary>
        public static ErrorResponse CreateDataConversionError(string address, string fromType, string toType, string reason = "")
        {
            return new ErrorResponse
            {
                Code = ErrorCodes.DATA_CONVERSION_ERROR,
                Message = $"Failed to convert data at address {address} from {fromType} to {toType}",
                Details = string.IsNullOrEmpty(reason) ? new List<string>() : new List<string> { reason },
                Type = ErrorType.Internal,
                Retryable = false,
                ResourceId = address
            };
        }

        /// <summary>
        /// 创建权限拒绝错误
        /// </summary>
        public static ErrorResponse CreatePermissionDeniedError(string operation, string resource)
        {
            return new ErrorResponse
            {
                Code = operation.ToLower().Contains("read") ? 
                    ErrorCodes.READ_PERMISSION_DENIED : ErrorCodes.WRITE_PERMISSION_DENIED,
                Message = $"Permission denied for {operation} on {resource}",
                Type = ErrorType.Authorization,
                Retryable = false,
                ResourceId = resource
            };
        }

        /// <summary>
        /// 创建配置错误
        /// </summary>
        public static ErrorResponse CreateConfigurationError(string configType, List<string> validationErrors)
        {
            return new ErrorResponse
            {
                Code = ErrorCodes.INVALID_CONFIGURATION,
                Message = $"Invalid {configType} configuration",
                Details = validationErrors,
                Type = ErrorType.Configuration,
                Retryable = false
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
                details.Add($"Stack trace: {exception.StackTrace}");
            }

            return new ErrorResponse
            {
                Code = ErrorCodes.INTERNAL_ERROR,
                Message = "Internal server error occurred",
                Details = details,
                Type = ErrorType.Internal,
                Retryable = true,
                RetryDelayMs = 1000
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