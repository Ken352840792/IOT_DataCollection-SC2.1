using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HlsService.Models
{
    /// <summary>
    /// 数据读取请求模型
    /// </summary>
    public class ReadRequestData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [Required]
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 数据点地址
        /// </summary>
        [Required]
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
        
        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string? DataType { get; set; }
        
        /// <summary>
        /// 读取选项
        /// </summary>
        [JsonPropertyName("options")]
        public ReadOptions? Options { get; set; }
    }
    
    /// <summary>
    /// 数据读取响应模型
    /// </summary>
    public class ReadResponseData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 数据点地址
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
        
        /// <summary>
        /// 读取的值
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value { get; set; }
        
        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = string.Empty;
        
        /// <summary>
        /// 数据质量
        /// </summary>
        [JsonPropertyName("quality")]
        public DataQuality Quality { get; set; } = DataQuality.Good;
        
        /// <summary>
        /// 读取时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 读取耗时（毫秒）
        /// </summary>
        [JsonPropertyName("readTimeMs")]
        public double ReadTimeMs { get; set; }
    }
    
    /// <summary>
    /// 数据写入请求模型
    /// </summary>
    public class WriteRequestData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [Required]
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 数据点地址
        /// </summary>
        [Required]
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
        
        /// <summary>
        /// 要写入的值
        /// </summary>
        [Required]
        [JsonPropertyName("value")]
        public object Value { get; set; } = null!;
        
        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string? DataType { get; set; }
        
        /// <summary>
        /// 写入选项
        /// </summary>
        [JsonPropertyName("options")]
        public WriteOptions? Options { get; set; }
    }
    
    /// <summary>
    /// 数据写入响应模型
    /// </summary>
    public class WriteResponseData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 数据点地址
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
        
        /// <summary>
        /// 写入是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        /// <summary>
        /// 写入的值
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value { get; set; }
        
        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = string.Empty;
        
        /// <summary>
        /// 写入时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 写入耗时（毫秒）
        /// </summary>
        [JsonPropertyName("writeTimeMs")]
        public double WriteTimeMs { get; set; }
    }
    
    /// <summary>
    /// 批量读取请求模型
    /// </summary>
    public class ReadBatchRequestData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [Required]
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 要读取的数据点列表
        /// </summary>
        [Required]
        [JsonPropertyName("addresses")]
        public List<string> Addresses { get; set; } = new();
        
        /// <summary>
        /// 数据点配置（可选，用于指定特定地址的数据类型等）
        /// </summary>
        [JsonPropertyName("dataPointConfigs")]
        public Dictionary<string, DataPointReadConfig>? DataPointConfigs { get; set; }
        
        /// <summary>
        /// 批量读取选项
        /// </summary>
        [JsonPropertyName("options")]
        public BatchReadOptions? Options { get; set; }
    }
    
    /// <summary>
    /// 批量读取响应模型
    /// </summary>
    public class ReadBatchResponseData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 读取结果列表
        /// </summary>
        [JsonPropertyName("results")]
        public List<DataPointReadResult> Results { get; set; } = new();
        
        /// <summary>
        /// 总数据点数
        /// </summary>
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        
        /// <summary>
        /// 成功读取数量
        /// </summary>
        [JsonPropertyName("successCount")]
        public int SuccessCount { get; set; }
        
        /// <summary>
        /// 失败读取数量
        /// </summary>
        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }
        
        /// <summary>
        /// 批量读取总耗时（毫秒）
        /// </summary>
        [JsonPropertyName("totalReadTimeMs")]
        public double TotalReadTimeMs { get; set; }
        
        /// <summary>
        /// 平均读取耗时（毫秒）
        /// </summary>
        [JsonPropertyName("averageReadTimeMs")]
        public double AverageReadTimeMs { get; set; }
    }
    
    /// <summary>
    /// 批量写入请求模型
    /// </summary>
    public class WriteBatchRequestData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [Required]
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 要写入的数据点列表
        /// </summary>
        [Required]
        [JsonPropertyName("dataPoints")]
        public List<DataPointWriteItem> DataPoints { get; set; } = new();
        
        /// <summary>
        /// 批量写入选项
        /// </summary>
        [JsonPropertyName("options")]
        public BatchWriteOptions? Options { get; set; }
    }
    
    /// <summary>
    /// 批量写入响应模型
    /// </summary>
    public class WriteBatchResponseData
    {
        /// <summary>
        /// 连接ID
        /// </summary>
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;
        
        /// <summary>
        /// 写入结果列表
        /// </summary>
        [JsonPropertyName("results")]
        public List<DataPointWriteResult> Results { get; set; } = new();
        
        /// <summary>
        /// 总数据点数
        /// </summary>
        [JsonPropertyName("totalCount")]
        public int TotalCount { get; set; }
        
        /// <summary>
        /// 成功写入数量
        /// </summary>
        [JsonPropertyName("successCount")]
        public int SuccessCount { get; set; }
        
        /// <summary>
        /// 失败写入数量
        /// </summary>
        [JsonPropertyName("failedCount")]
        public int FailedCount { get; set; }
        
        /// <summary>
        /// 批量写入总耗时（毫秒）
        /// </summary>
        [JsonPropertyName("totalWriteTimeMs")]
        public double TotalWriteTimeMs { get; set; }
        
        /// <summary>
        /// 平均写入耗时（毫秒）
        /// </summary>
        [JsonPropertyName("averageWriteTimeMs")]
        public double AverageWriteTimeMs { get; set; }
    }
    
    /// <summary>
    /// 数据点读取配置
    /// </summary>
    public class DataPointReadConfig
    {
        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string? DataType { get; set; }
        
        /// <summary>
        /// 数据长度（用于字符串或数组）
        /// </summary>
        [JsonPropertyName("length")]
        public int? Length { get; set; }
        
        /// <summary>
        /// 字节序（BigEndian/LittleEndian）
        /// </summary>
        [JsonPropertyName("byteOrder")]
        public string? ByteOrder { get; set; }
        
        /// <summary>
        /// 缩放因子
        /// </summary>
        [JsonPropertyName("scaleFactor")]
        public double? ScaleFactor { get; set; }
        
        /// <summary>
        /// 偏移量
        /// </summary>
        [JsonPropertyName("offset")]
        public double? Offset { get; set; }
    }
    
    /// <summary>
    /// 数据点读取结果
    /// </summary>
    public class DataPointReadResult
    {
        /// <summary>
        /// 数据点地址
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
        
        /// <summary>
        /// 读取是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        /// <summary>
        /// 读取的值
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value { get; set; }
        
        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = string.Empty;
        
        /// <summary>
        /// 数据质量
        /// </summary>
        [JsonPropertyName("quality")]
        public DataQuality Quality { get; set; } = DataQuality.Good;
        
        /// <summary>
        /// 读取时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 读取耗时（毫秒）
        /// </summary>
        [JsonPropertyName("readTimeMs")]
        public double ReadTimeMs { get; set; }
        
        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        [JsonPropertyName("error")]
        public ErrorResponse? Error { get; set; }
    }
    
    /// <summary>
    /// 数据点写入项
    /// </summary>
    public class DataPointWriteItem
    {
        /// <summary>
        /// 数据点地址
        /// </summary>
        [Required]
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
        
        /// <summary>
        /// 要写入的值
        /// </summary>
        [Required]
        [JsonPropertyName("value")]
        public object Value { get; set; } = null!;
        
        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string? DataType { get; set; }
    }
    
    /// <summary>
    /// 数据点写入结果
    /// </summary>
    public class DataPointWriteResult
    {
        /// <summary>
        /// 数据点地址
        /// </summary>
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;
        
        /// <summary>
        /// 写入是否成功
        /// </summary>
        [JsonPropertyName("success")]
        public bool Success { get; set; }
        
        /// <summary>
        /// 写入的值
        /// </summary>
        [JsonPropertyName("value")]
        public object? Value { get; set; }
        
        /// <summary>
        /// 数据类型
        /// </summary>
        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = string.Empty;
        
        /// <summary>
        /// 写入时间戳
        /// </summary>
        [JsonPropertyName("timestamp")]
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;
        
        /// <summary>
        /// 写入耗时（毫秒）
        /// </summary>
        [JsonPropertyName("writeTimeMs")]
        public double WriteTimeMs { get; set; }
        
        /// <summary>
        /// 错误信息（如果失败）
        /// </summary>
        [JsonPropertyName("error")]
        public ErrorResponse? Error { get; set; }
    }
    
    /// <summary>
    /// 读取选项
    /// </summary>
    public class ReadOptions
    {
        /// <summary>
        /// 读取超时时间（毫秒）
        /// </summary>
        [JsonPropertyName("timeoutMs")]
        public int TimeoutMs { get; set; } = 5000;
        
        /// <summary>
        /// 重试次数
        /// </summary>
        [JsonPropertyName("retryCount")]
        public int RetryCount { get; set; } = 3;
        
        /// <summary>
        /// 是否使用缓存
        /// </summary>
        [JsonPropertyName("useCache")]
        public bool UseCache { get; set; } = false;
        
        /// <summary>
        /// 缓存有效期（毫秒）
        /// </summary>
        [JsonPropertyName("cacheTimeoutMs")]
        public int CacheTimeoutMs { get; set; } = 1000;
    }
    
    /// <summary>
    /// 写入选项
    /// </summary>
    public class WriteOptions
    {
        /// <summary>
        /// 写入超时时间（毫秒）
        /// </summary>
        [JsonPropertyName("timeoutMs")]
        public int TimeoutMs { get; set; } = 5000;
        
        /// <summary>
        /// 重试次数
        /// </summary>
        [JsonPropertyName("retryCount")]
        public int RetryCount { get; set; } = 3;
        
        /// <summary>
        /// 写入后是否验证
        /// </summary>
        [JsonPropertyName("verifyAfterWrite")]
        public bool VerifyAfterWrite { get; set; } = false;
        
        /// <summary>
        /// 验证延迟（毫秒）
        /// </summary>
        [JsonPropertyName("verifyDelayMs")]
        public int VerifyDelayMs { get; set; } = 100;
    }
    
    /// <summary>
    /// 批量读取选项
    /// </summary>
    public class BatchReadOptions
    {
        /// <summary>
        /// 批量读取超时时间（毫秒）
        /// </summary>
        [JsonPropertyName("timeoutMs")]
        public int TimeoutMs { get; set; } = 10000;
        
        /// <summary>
        /// 最大并发读取数
        /// </summary>
        [JsonPropertyName("maxConcurrency")]
        public int MaxConcurrency { get; set; } = 10;
        
        /// <summary>
        /// 是否在第一个错误时停止
        /// </summary>
        [JsonPropertyName("stopOnFirstError")]
        public bool StopOnFirstError { get; set; } = false;
        
        /// <summary>
        /// 是否包含失败的项
        /// </summary>
        [JsonPropertyName("includeFailedItems")]
        public bool IncludeFailedItems { get; set; } = true;
    }
    
    /// <summary>
    /// 批量写入选项
    /// </summary>
    public class BatchWriteOptions
    {
        /// <summary>
        /// 批量写入超时时间（毫秒）
        /// </summary>
        [JsonPropertyName("timeoutMs")]
        public int TimeoutMs { get; set; } = 15000;
        
        /// <summary>
        /// 最大并发写入数
        /// </summary>
        [JsonPropertyName("maxConcurrency")]
        public int MaxConcurrency { get; set; } = 5;
        
        /// <summary>
        /// 是否在第一个错误时停止
        /// </summary>
        [JsonPropertyName("stopOnFirstError")]
        public bool StopOnFirstError { get; set; } = false;
        
        /// <summary>
        /// 是否包含失败的项
        /// </summary>
        [JsonPropertyName("includeFailedItems")]
        public bool IncludeFailedItems { get; set; } = true;
        
        /// <summary>
        /// 写入后是否验证
        /// </summary>
        [JsonPropertyName("verifyAfterWrite")]
        public bool VerifyAfterWrite { get; set; } = false;
    }
    
    /// <summary>
    /// 数据质量枚举
    /// </summary>
    public enum DataQuality
    {
        /// <summary>
        /// 良好
        /// </summary>
        Good = 0,
        
        /// <summary>
        /// 不确定
        /// </summary>
        Uncertain = 1,
        
        /// <summary>
        /// 错误
        /// </summary>
        Bad = 2,
        
        /// <summary>
        /// 配置错误
        /// </summary>
        ConfigError = 3,
        
        /// <summary>
        /// 未连接
        /// </summary>
        NotConnected = 4,
        
        /// <summary>
        /// 设备故障
        /// </summary>
        DeviceFault = 5,
        
        /// <summary>
        /// 传感器故障
        /// </summary>
        SensorFault = 6,
        
        /// <summary>
        /// 无最新数据
        /// </summary>
        NoLastKnownValue = 7,
        
        /// <summary>
        /// 通信错误
        /// </summary>
        CommFailure = 8,
        
        /// <summary>
        /// 超出范围
        /// </summary>
        OutOfRange = 9
    }
}