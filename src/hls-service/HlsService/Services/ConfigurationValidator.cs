using System.Text.Json;
using System.Text.RegularExpressions;
using HlsService.Models;

namespace HlsService.Services
{
    /// <summary>
    /// 配置验证服务
    /// 提供设备配置、数据点位配置等的验证功能
    /// </summary>
    public static class ConfigurationValidator
    {
        /// <summary>
        /// 验证数据点位配置
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateDataPointConfiguration(DataPointConfiguration config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("Data point configuration is null");
                return (false, errors);
            }

            // 验证地址
            if (string.IsNullOrWhiteSpace(config.Address))
            {
                errors.Add("Address is required");
            }
            else if (!IsValidAddress(config.Address))
            {
                errors.Add($"Invalid address format: {config.Address}");
            }

            // 验证数据类型
            if (!Enum.IsDefined(typeof(DataPointType), config.DataType))
            {
                errors.Add($"Invalid data type: {config.DataType}");
            }

            // 验证访问模式
            if (!Enum.IsDefined(typeof(DataPointAccessMode), config.AccessMode))
            {
                errors.Add($"Invalid access mode: {config.AccessMode}");
            }

            // 验证标量系数
            if (config.ScaleFactor == 0)
            {
                errors.Add("Scale factor cannot be zero");
            }

            // 验证名称（如果提供）
            if (!string.IsNullOrEmpty(config.Name) && config.Name.Length > 100)
            {
                errors.Add("Name cannot exceed 100 characters");
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 验证数据点位组配置
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateDataPointGroup(DataPointGroup group)
        {
            var errors = new List<string>();

            if (group == null)
            {
                errors.Add("Data point group is null");
                return (false, errors);
            }

            // 验证组基础信息
            if (string.IsNullOrWhiteSpace(group.GroupId))
            {
                errors.Add("Group ID is required");
            }

            if (string.IsNullOrWhiteSpace(group.GroupName))
            {
                errors.Add("Group name is required");
            }

            // 验证扫描间隔
            if (group.ScanIntervalMs < 100 || group.ScanIntervalMs > 60000)
            {
                errors.Add("Scan interval must be between 100ms and 60000ms");
            }

            // 验证数据点位
            if (group.DataPoints == null || group.DataPoints.Count == 0)
            {
                errors.Add("Group must contain at least one data point");
            }
            else
            {
                var addresses = new HashSet<string>();
                for (int i = 0; i < group.DataPoints.Count; i++)
                {
                    var point = group.DataPoints[i];
                    var (isValid, pointErrors) = ValidateDataPointConfiguration(point);
                    
                    if (!isValid)
                    {
                        errors.AddRange(pointErrors.Select(e => $"DataPoint[{i}]: {e}"));
                    }

                    // 检查地址重复
                    if (!string.IsNullOrEmpty(point.Address))
                    {
                        if (addresses.Contains(point.Address))
                        {
                            errors.Add($"Duplicate address found: {point.Address}");
                        }
                        addresses.Add(point.Address);
                    }
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 验证批量数据点位操作请求
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateBatchDataPointRequest(BatchDataPointRequest request)
        {
            var errors = new List<string>();

            if (request == null)
            {
                errors.Add("Batch data point request is null");
                return (false, errors);
            }

            // 验证设备ID
            if (string.IsNullOrWhiteSpace(request.DeviceId))
            {
                errors.Add("Device ID is required");
            }

            // 验证操作类型
            if (!Enum.IsDefined(typeof(DataPointOperation), request.Operation))
            {
                errors.Add($"Invalid operation type: {request.Operation}");
            }

            // 验证超时时间
            if (request.TimeoutMs < 1000 || request.TimeoutMs > 30000)
            {
                errors.Add("Timeout must be between 1000ms and 30000ms");
            }

            // 验证数据点位
            if (request.DataPoints == null || request.DataPoints.Count == 0)
            {
                errors.Add("At least one data point is required");
            }
            else
            {
                for (int i = 0; i < request.DataPoints.Count; i++)
                {
                    var point = request.DataPoints[i];
                    var (isValid, pointErrors) = ValidateDataPointConfiguration(point);
                    
                    if (!isValid)
                    {
                        errors.AddRange(pointErrors.Select(e => $"DataPoint[{i}]: {e}"));
                    }
                }
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 验证设备数据点位配置集合
        /// </summary>
        public static (bool IsValid, List<string> Errors) ValidateDeviceDataPointConfiguration(DeviceDataPointConfiguration config)
        {
            var errors = new List<string>();

            if (config == null)
            {
                errors.Add("Device data point configuration is null");
                return (false, errors);
            }

            // 验证设备ID
            if (string.IsNullOrWhiteSpace(config.DeviceId))
            {
                errors.Add("Device ID is required");
            }

            // 验证版本
            if (string.IsNullOrWhiteSpace(config.Version))
            {
                errors.Add("Configuration version is required");
            }

            var allAddresses = new HashSet<string>();

            // 验证数据点位组
            if (config.Groups != null)
            {
                for (int i = 0; i < config.Groups.Count; i++)
                {
                    var group = config.Groups[i];
                    var (isValid, groupErrors) = ValidateDataPointGroup(group);
                    
                    if (!isValid)
                    {
                        errors.AddRange(groupErrors.Select(e => $"Group[{i}]: {e}"));
                    }

                    // 收集所有地址用于重复检查
                    if (group.DataPoints != null)
                    {
                        foreach (var point in group.DataPoints)
                        {
                            if (!string.IsNullOrEmpty(point.Address))
                            {
                                allAddresses.Add(point.Address);
                            }
                        }
                    }
                }
            }

            // 验证单独的数据点位
            if (config.StandalonePoints != null)
            {
                for (int i = 0; i < config.StandalonePoints.Count; i++)
                {
                    var point = config.StandalonePoints[i];
                    var (isValid, pointErrors) = ValidateDataPointConfiguration(point);
                    
                    if (!isValid)
                    {
                        errors.AddRange(pointErrors.Select(e => $"StandalonePoint[{i}]: {e}"));
                    }

                    // 检查与组内点位的地址冲突
                    if (!string.IsNullOrEmpty(point.Address))
                    {
                        if (allAddresses.Contains(point.Address))
                        {
                            errors.Add($"Address conflict found: {point.Address} exists in both group and standalone points");
                        }
                    }
                }
            }

            // 检查是否至少有一个数据点位
            var totalPoints = (config.Groups?.Sum(g => g.DataPoints?.Count ?? 0) ?? 0) + 
                            (config.StandalonePoints?.Count ?? 0);
            
            if (totalPoints == 0)
            {
                errors.Add("At least one data point must be configured");
            }

            return (errors.Count == 0, errors);
        }

        /// <summary>
        /// 验证地址格式是否有效
        /// </summary>
        private static bool IsValidAddress(string address)
        {
            if (string.IsNullOrWhiteSpace(address))
                return false;

            // 支持常见的地址格式
            // Modbus: 40001, 30001, 10001, 00001
            // S7: DB1.DBX0.0, M0.0, I0.0, Q0.0
            // 通用格式验证
            var patterns = new[]
            {
                @"^\d{1,6}$",                           // 纯数字地址 (Modbus)
                @"^[0-4]\d{4}$",                        // Modbus标准地址格式
                @"^DB\d+\.(DB[XWDB])?(\d+\.)*\d+$",     // S7 DB地址
                @"^[MIQABSC]\d+\.\d+$",                 // S7标准地址
                @"^[DWCH]\d+$",                         // Omron/Mitsubishi地址
                @"^[A-Z]+\d+(\.\d+)?$"                  // 通用字母+数字格式
            };

            return patterns.Any(pattern => Regex.IsMatch(address, pattern, RegexOptions.IgnoreCase));
        }

        /// <summary>
        /// 生成设备配置的JSON Schema
        /// </summary>
        public static string GetDeviceConfigurationSchema()
        {
            return JsonSerializer.Serialize(new
            {
                @type = "object",
                title = "Device Configuration Schema",
                description = "Schema for device configuration validation",
                properties = new
                {
                    deviceId = new { type = "string", minLength = 1, maxLength = 100 },
                    type = new { type = "integer", minimum = 0, maximum = 10 },
                    name = new { type = "string", maxLength = 200 },
                    description = new { type = "string", maxLength = 500 },
                    connection = new
                    {
                        type = "object",
                        properties = new
                        {
                            host = new { type = "string", pattern = @"^(?:[0-9]{1,3}\.){3}[0-9]{1,3}$|^[a-zA-Z0-9.-]+$" },
                            port = new { type = "integer", minimum = 1, maximum = 65535 },
                            timeoutMs = new { type = "integer", minimum = 1000, maximum = 30000 },
                            station = new { type = "integer", minimum = 0, maximum = 255 }
                        },
                        required = new[] { "host", "port" }
                    }
                },
                required = new[] { "deviceId", "type", "connection" }
            }, new JsonSerializerOptions { WriteIndented = true });
        }

        /// <summary>
        /// 生成数据点位配置的JSON Schema
        /// </summary>
        public static string GetDataPointConfigurationSchema()
        {
            return JsonSerializer.Serialize(new
            {
                @type = "object",
                title = "Data Point Configuration Schema",
                description = "Schema for data point configuration validation",
                properties = new
                {
                    address = new { type = "string", minLength = 1, maxLength = 50 },
                    dataType = new { type = "string", @enum = Enum.GetNames(typeof(DataPointType)) },
                    name = new { type = "string", maxLength = 100 },
                    description = new { type = "string", maxLength = 500 },
                    accessMode = new { type = "string", @enum = Enum.GetNames(typeof(DataPointAccessMode)) },
                    unit = new { type = "string", maxLength = 20 },
                    scaleFactor = new { type = "number", exclusiveMinimum = 0 },
                    offset = new { type = "number" },
                    enabled = new { type = "boolean" }
                },
                required = new[] { "address", "dataType" }
            }, new JsonSerializerOptions { WriteIndented = true });
        }
    }
}