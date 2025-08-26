using System.ComponentModel.DataAnnotations;
using System.Text.Json.Serialization;

namespace HlsService.Models
{
    /// <summary>
    /// 数据点位配置模型
    /// 用于标准化数据点位的定义和管理
    /// </summary>
    public class DataPointConfiguration
    {
        /// <summary>
        /// 数据点位地址
        /// </summary>
        [Required]
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        /// <summary>
        /// 数据类型
        /// </summary>
        [Required]
        [JsonPropertyName("dataType")]
        public DataPointType DataType { get; set; } = DataPointType.Int16;

        /// <summary>
        /// 点位名称
        /// </summary>
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        /// <summary>
        /// 点位描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 读写权限
        /// </summary>
        [JsonPropertyName("accessMode")]
        public DataPointAccessMode AccessMode { get; set; } = DataPointAccessMode.Read;

        /// <summary>
        /// 数据单位
        /// </summary>
        [JsonPropertyName("unit")]
        public string Unit { get; set; } = string.Empty;

        /// <summary>
        /// 标量系数（用于数据转换）
        /// </summary>
        [JsonPropertyName("scaleFactor")]
        public double ScaleFactor { get; set; } = 1.0;

        /// <summary>
        /// 偏移量（用于数据转换）
        /// </summary>
        [JsonPropertyName("offset")]
        public double Offset { get; set; } = 0.0;

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
    /// 数据点位类型枚举
    /// </summary>
    public enum DataPointType
    {
        Bool,
        Int16,
        UInt16,
        Int32,
        UInt32,
        Int64,
        UInt64,
        Float,
        Double,
        String
    }

    /// <summary>
    /// 数据点位访问模式
    /// </summary>
    public enum DataPointAccessMode
    {
        Read,      // 只读
        Write,     // 只写
        ReadWrite  // 读写
    }

    /// <summary>
    /// 数据点位组配置
    /// 用于管理一组相关的数据点位
    /// </summary>
    public class DataPointGroup
    {
        /// <summary>
        /// 组ID
        /// </summary>
        [Required]
        [JsonPropertyName("groupId")]
        public string GroupId { get; set; } = string.Empty;

        /// <summary>
        /// 组名称
        /// </summary>
        [Required]
        [JsonPropertyName("groupName")]
        public string GroupName { get; set; } = string.Empty;

        /// <summary>
        /// 组描述
        /// </summary>
        [JsonPropertyName("description")]
        public string Description { get; set; } = string.Empty;

        /// <summary>
        /// 数据点位列表
        /// </summary>
        [Required]
        [JsonPropertyName("dataPoints")]
        public List<DataPointConfiguration> DataPoints { get; set; } = new();

        /// <summary>
        /// 扫描间隔（毫秒）
        /// </summary>
        [JsonPropertyName("scanIntervalMs")]
        [Range(100, 60000)]
        public int ScanIntervalMs { get; set; } = 1000;

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
    /// 数据点位读取结果增强版
    /// </summary>
    public class DataPointReadResultEx : ReadResult
    {
        /// <summary>
        /// 数据点位配置信息
        /// </summary>
        [JsonPropertyName("pointConfig")]
        public DataPointConfiguration? PointConfig { get; set; }

        /// <summary>
        /// 原始值（转换前）
        /// </summary>
        [JsonPropertyName("rawValue")]
        public object? RawValue { get; set; }

        /// <summary>
        /// 经过标量和偏移处理后的值
        /// </summary>
        [JsonPropertyName("scaledValue")]
        public object? ScaledValue { get; set; }

        /// <summary>
        /// 数据质量指示
        /// </summary>
        [JsonPropertyName("quality")]
        public DataQuality Quality { get; set; } = DataQuality.Good;
    }

    /// <summary>
    /// 数据质量枚举
    /// </summary>
    public enum DataQuality
    {
        Good,       // 数据正常
        Bad,        // 数据异常
        Uncertain,  // 数据不确定
        Timeout     // 读取超时
    }

    /// <summary>
    /// 批量数据点位操作请求
    /// </summary>
    public class BatchDataPointRequest
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        [Required]
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 数据点位组ID（可选）
        /// </summary>
        [JsonPropertyName("groupId")]
        public string? GroupId { get; set; }

        /// <summary>
        /// 数据点位列表
        /// </summary>
        [Required]
        [JsonPropertyName("dataPoints")]
        public List<DataPointConfiguration> DataPoints { get; set; } = new();

        /// <summary>
        /// 操作类型
        /// </summary>
        [Required]
        [JsonPropertyName("operation")]
        public DataPointOperation Operation { get; set; }

        /// <summary>
        /// 是否并行执行
        /// </summary>
        [JsonPropertyName("parallel")]
        public bool Parallel { get; set; } = true;

        /// <summary>
        /// 超时时间（毫秒）
        /// </summary>
        [JsonPropertyName("timeoutMs")]
        [Range(1000, 30000)]
        public int TimeoutMs { get; set; } = 5000;
    }

    /// <summary>
    /// 数据点位操作类型
    /// </summary>
    public enum DataPointOperation
    {
        Read,
        Write,
        ReadWrite
    }

    /// <summary>
    /// 设备数据点位配置集合
    /// 用于管理特定设备的所有数据点位
    /// </summary>
    public class DeviceDataPointConfiguration
    {
        /// <summary>
        /// 设备ID
        /// </summary>
        [Required]
        [JsonPropertyName("deviceId")]
        public string DeviceId { get; set; } = string.Empty;

        /// <summary>
        /// 数据点位组列表
        /// </summary>
        [JsonPropertyName("groups")]
        public List<DataPointGroup> Groups { get; set; } = new();

        /// <summary>
        /// 单独的数据点位（不属于任何组）
        /// </summary>
        [JsonPropertyName("standalonePoints")]
        public List<DataPointConfiguration> StandalonePoints { get; set; } = new();

        /// <summary>
        /// 配置版本
        /// </summary>
        [JsonPropertyName("version")]
        public string Version { get; set; } = "1.0";

        /// <summary>
        /// 最后更新时间
        /// </summary>
        [JsonPropertyName("lastUpdated")]
        public DateTime LastUpdated { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// 获取所有数据点位
        /// </summary>
        public IEnumerable<DataPointConfiguration> GetAllDataPoints()
        {
            foreach (var group in Groups)
            {
                foreach (var point in group.DataPoints)
                {
                    yield return point;
                }
            }

            foreach (var point in StandalonePoints)
            {
                yield return point;
            }
        }

        /// <summary>
        /// 根据地址查找数据点位
        /// </summary>
        public DataPointConfiguration? FindDataPoint(string address)
        {
            return GetAllDataPoints().FirstOrDefault(p => p.Address.Equals(address, StringComparison.OrdinalIgnoreCase));
        }
    }
}