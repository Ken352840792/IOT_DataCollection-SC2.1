using System.Text.Json;
using System.Text.Json.Serialization;
using HlsService.Models;
using HlsService.Services;

namespace HlsService.Api.Controllers
{
    /// <summary>
    /// 连接管理API控制器
    /// 处理设备连接建立、断开和状态查询等操作
    /// </summary>
    public class ConnectionController
    {
        private readonly DeviceManager _deviceManager;
        private readonly ServerConfiguration _config;

        public ConnectionController(DeviceManager deviceManager, ServerConfiguration config)
        {
            _deviceManager = deviceManager;
            _config = config;
        }

        /// <summary>
        /// 建立设备连接
        /// 支持的命令: "connect"
        /// </summary>
        public async Task<IpcResponse> ConnectAsync(IpcRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 验证请求
                var validation = ValidateConnectRequest(request);
                if (!validation.IsValid)
                {
                    return CreateErrorResponse(request.MessageId, 
                        ErrorFactory.CreateConfigurationError("connect", validation.Errors), 
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 解析连接数据
                var connectionData = ParseConnectionData(request.Data);
                if (connectionData == null)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateValidationError("data", "Invalid connection data format"),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 生成连接ID
                var connectionId = GenerateConnectionId();

                // 创建设备配置
                var deviceConfig = new DeviceConfiguration
                {
                    DeviceId = connectionId,
                    Type = ParseDeviceType(connectionData.DeviceConfig.Type),
                    Name = $"Device_{connectionId}",
                    Description = "Auto-generated device configuration",
                    Connection = new ConnectionParameters
                    {
                        Host = connectionData.DeviceConfig.Host,
                        Port = connectionData.DeviceConfig.Port,
                        TimeoutMs = connectionData.DeviceConfig.Timeout ?? 5000
                    }
                };

                // 添加并连接设备
                var addResult = await _deviceManager.AddDeviceAsync(deviceConfig);
                if (!addResult.Success)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateDeviceConnectionError(connectionId, addResult.Message),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                var connectResult = await _deviceManager.ConnectDeviceAsync(connectionId);
                if (!connectResult.Success)
                {
                    // 清理失败的设备
                    await _deviceManager.RemoveDeviceAsync(connectionId);
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateDeviceConnectionError(connectionId, connectResult.Message),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 配置数据点（如果提供）
                if (connectionData.DataPoints?.Any() == true)
                {
                    await ConfigureDataPoints(connectionId, connectionData.DataPoints);
                }

                // 返回成功响应
                var responseData = new
                {
                    connectionId = connectionId,
                    status = "connected",
                    deviceInfo = new
                    {
                        type = connectionData.DeviceConfig.Type,
                        host = connectionData.DeviceConfig.Host,
                        port = connectionData.DeviceConfig.Port,
                        timeout = deviceConfig.Connection.TimeoutMs
                    },
                    dataPointsConfigured = connectionData.DataPoints?.Length ?? 0
                };

                return CreateSuccessResponse(request.MessageId, responseData, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(request.MessageId,
                    ErrorFactory.CreateInternalError($"Failed to establish connection: {ex.Message}", ex),
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 断开设备连接
        /// 支持的命令: "disconnect"
        /// </summary>
        public async Task<IpcResponse> DisconnectAsync(IpcRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 验证请求
                var connectionId = ExtractConnectionId(request.Data);
                if (string.IsNullOrEmpty(connectionId))
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateMissingParameterError("connectionId"),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 检查连接是否存在
                var status = await _deviceManager.GetDeviceStatusAsync(connectionId);
                if (status == null)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateDeviceNotFoundError(connectionId),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 断开连接
                var disconnectResult = await _deviceManager.DisconnectDeviceAsync(connectionId);
                if (!disconnectResult.Success)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateInternalError($"Failed to disconnect device: {disconnectResult.Message}"),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 移除设备
                var removeResult = await _deviceManager.RemoveDeviceAsync(connectionId);

                var responseData = new
                {
                    connectionId = connectionId,
                    status = "disconnected",
                    message = removeResult.Success 
                        ? "Device disconnected and removed successfully"
                        : $"Device disconnected but removal failed: {removeResult.Message}"
                };

                return CreateSuccessResponse(request.MessageId, responseData, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(request.MessageId,
                    ErrorFactory.CreateInternalError($"Failed to disconnect: {ex.Message}", ex),
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 查询连接状态
        /// 支持的命令: "status"
        /// </summary>
        public async Task<IpcResponse> GetStatusAsync(IpcRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var connectionId = ExtractConnectionId(request.Data);
                if (string.IsNullOrEmpty(connectionId))
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateMissingParameterError("connectionId"),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 获取设备状态
                var status = await _deviceManager.GetDeviceStatusAsync(connectionId);
                if (status == null)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateDeviceNotFoundError(connectionId),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 测试连接状态
                var isConnected = await _deviceManager.TestDeviceConnectionAsync(connectionId);

                var responseData = new
                {
                    connectionId = connectionId,
                    status = isConnected ? "connected" : "disconnected",
                    lastActivity = DateTime.UtcNow, // 实际项目中应该跟踪真实的最后活动时间
                    deviceInfo = status,
                    connectionHealth = new
                    {
                        isAlive = isConnected,
                        lastCheckTime = DateTime.UtcNow,
                        responseTime = stopwatch.Elapsed.TotalMilliseconds
                    }
                };

                return CreateSuccessResponse(request.MessageId, responseData, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(request.MessageId,
                    ErrorFactory.CreateInternalError($"Failed to get connection status: {ex.Message}", ex),
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 列出所有连接
        /// 支持的命令: "listConnections"
        /// </summary>
        public async Task<IpcResponse> ListConnectionsAsync(IpcRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                var deviceList = _deviceManager.GetDeviceList();
                var allStatus = await _deviceManager.GetAllDeviceStatusAsync();

                var connections = deviceList.Select(device => new
                {
                    connectionId = device.DeviceId,
                    deviceType = device.Type.ToString(),
                    host = device.Connection.Host,
                    port = device.Connection.Port,
                    status = GetDeviceStatusString(device.DeviceId, allStatus),
                    addedTime = device.CreatedTime
                }).ToArray();

                var responseData = new
                {
                    connections = connections,
                    totalCount = connections.Length,
                    activeCount = connections.Count(c => c.status == "connected"),
                    maxConnections = ProtocolConstants.MAX_CONCURRENT_CONNECTIONS,
                    timestamp = DateTime.UtcNow
                };

                return CreateSuccessResponse(request.MessageId, responseData, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(request.MessageId,
                    ErrorFactory.CreateInternalError($"Failed to list connections: {ex.Message}", ex),
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 验证连接参数
        /// 支持的命令: "validateConnection"
        /// </summary>
        public IpcResponse ValidateConnectionParameters(IpcRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 解析连接数据
                var connectionData = ParseConnectionData(request.Data);
                if (connectionData == null)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateValidationError("data", "Invalid connection data format"),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 验证设备配置
                var deviceConfig = new DeviceConfiguration
                {
                    DeviceId = "validation_temp",
                    Type = ParseDeviceType(connectionData.DeviceConfig.Type),
                    Name = "Validation Config",
                    Connection = new ConnectionParameters
                    {
                        Host = connectionData.DeviceConfig.Host,
                        Port = connectionData.DeviceConfig.Port,
                        TimeoutMs = connectionData.DeviceConfig.Timeout ?? 5000
                    }
                };

                var (isValid, errorMessage) = DeviceConnectionFactory.ValidateDeviceConfiguration(deviceConfig);
                var errors = new List<string>();

                if (!isValid)
                {
                    errors.Add(errorMessage);
                }

                // 验证数据点配置
                if (connectionData.DataPoints?.Any() == true)
                {
                    var dataPointErrors = ValidateDataPoints(connectionData.DataPoints);
                    errors.AddRange(dataPointErrors);
                }

                var responseData = new
                {
                    valid = errors.Count == 0,
                    errors = errors,
                    deviceConfig = new
                    {
                        type = connectionData.DeviceConfig.Type,
                        host = connectionData.DeviceConfig.Host,
                        port = connectionData.DeviceConfig.Port,
                        timeout = deviceConfig.Connection.TimeoutMs
                    },
                    dataPointsCount = connectionData.DataPoints?.Length ?? 0,
                    validation = new
                    {
                        timestamp = DateTime.UtcNow,
                        validatorVersion = _config.ProtocolVersion
                    }
                };

                return CreateSuccessResponse(request.MessageId, responseData, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(request.MessageId,
                    ErrorFactory.CreateInternalError($"Connection validation failed: {ex.Message}", ex),
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        #region Private Helper Methods

        /// <summary>
        /// 验证连接请求
        /// </summary>
        private ValidationResult ValidateConnectRequest(IpcRequest request)
        {
            var result = new ValidationResult();

            // 使用协议验证器验证基础消息格式
            var protocolValidation = MessageValidator.ValidateRequest(request);
            if (!protocolValidation.IsValid)
            {
                result.Errors.AddRange(protocolValidation.Errors);
            }

            // 验证命令类型
            if (!CommandTypes.IsValidCommand(request.Command))
            {
                result.Errors.Add($"Invalid command: {request.Command}");
            }

            // 验证数据存在
            if (request.Data == null)
            {
                result.Errors.Add("Connection data is required");
            }

            result.IsValid = result.Errors.Count == 0;
            return result;
        }

        /// <summary>
        /// 解析连接数据
        /// </summary>
        private ConnectionRequestData? ParseConnectionData(object? data)
        {
            if (data == null) return null;

            try
            {
                var json = JsonSerializer.Serialize(data);
                return JsonSerializer.Deserialize<ConnectionRequestData>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 解析设备类型
        /// </summary>
        private DeviceType ParseDeviceType(string typeString)
        {
            return typeString?.ToLower() switch
            {
                "modbus-tcp" => DeviceType.ModbusTcp,
                "modbus-rtu" => DeviceType.ModbusRtu,
                "siemens-s7" => DeviceType.SiemensS7,
                _ => DeviceType.ModbusTcp // 默认使用 Modbus TCP
            };
        }

        /// <summary>
        /// 构建连接字符串
        /// </summary>
        private string BuildConnectionString(DeviceConfigData config)
        {
            return $"{config.Host}:{config.Port}";
        }

        /// <summary>
        /// 生成连接ID
        /// </summary>
        private string GenerateConnectionId()
        {
            return $"conn_{DateTime.UtcNow:yyyyMMddHHmmss}_{Random.Shared.Next(1000, 9999)}";
        }

        /// <summary>
        /// 配置数据点
        /// </summary>
        private async Task ConfigureDataPoints(string connectionId, DataPointConfigData[] dataPoints)
        {
            try
            {
                // 这里应该实现数据点配置逻辑
                // 目前作为占位符实现
                await Task.Delay(10); // 模拟配置时间
                
                // 实际实现中，应该将数据点配置保存到设备管理器
                // 例如：await _deviceManager.ConfigureDataPointsAsync(connectionId, dataPoints);
            }
            catch (Exception ex)
            {
                // 记录配置错误但不阻塞连接建立
                Console.WriteLine($"[连接控制器] 配置数据点时出错: {ex.Message}");
            }
        }

        /// <summary>
        /// 提取连接ID
        /// </summary>
        private string? ExtractConnectionId(object? data)
        {
            if (data == null) return null;

            try
            {
                var json = JsonSerializer.Serialize(data);
                using var document = JsonDocument.Parse(json);
                
                if (document.RootElement.TryGetProperty("connectionId", out var element))
                {
                    return element.GetString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 获取设备状态字符串
        /// </summary>
        private string GetDeviceStatusString(string deviceId, object[] allStatus)
        {
            // 简化实现，实际项目中应该根据设备状态返回准确信息
            return "connected"; // 占位符
        }

        /// <summary>
        /// 验证数据点配置
        /// </summary>
        private List<string> ValidateDataPoints(DataPointConfigData[] dataPoints)
        {
            var errors = new List<string>();

            if (dataPoints.Length > ProtocolConstants.MAX_BATCH_SIZE)
            {
                errors.Add($"Too many data points: {dataPoints.Length} (max: {ProtocolConstants.MAX_BATCH_SIZE})");
            }

            foreach (var dp in dataPoints)
            {
                if (string.IsNullOrEmpty(dp.Name))
                {
                    errors.Add($"Data point name is required for address {dp.Address}");
                }

                if (string.IsNullOrEmpty(dp.Address))
                {
                    errors.Add($"Data point address is required for {dp.Name}");
                }

                if (!IsValidDataType(dp.DataType))
                {
                    errors.Add($"Invalid data type '{dp.DataType}' for data point {dp.Name}");
                }

                if (!IsValidAccessMode(dp.Access))
                {
                    errors.Add($"Invalid access mode '{dp.Access}' for data point {dp.Name}");
                }
            }

            return errors;
        }

        /// <summary>
        /// 验证数据类型
        /// </summary>
        private bool IsValidDataType(string dataType)
        {
            return dataType?.ToLower() switch
            {
                "bool" or "boolean" or "int16" or "uint16" or "int32" or "uint32" 
                or "int64" or "uint64" or "float" or "double" or "string" => true,
                _ => false
            };
        }

        /// <summary>
        /// 验证访问模式
        /// </summary>
        private bool IsValidAccessMode(string access)
        {
            return access?.ToLower() switch
            {
                "read" or "write" or "readwrite" => true,
                _ => false
            };
        }

        /// <summary>
        /// 创建成功响应
        /// </summary>
        private IpcResponse CreateSuccessResponse(string messageId, object data, double processingTimeMs)
        {
            return new IpcResponse
            {
                MessageId = messageId,
                Version = _config.ProtocolVersion,
                Success = true,
                Data = data,
                ProcessingTimeMs = processingTimeMs,
                Timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 创建错误响应
        /// </summary>
        private IpcResponse CreateErrorResponse(string messageId, ErrorResponse error, double processingTimeMs)
        {
            return new IpcResponse
            {
                MessageId = messageId,
                Version = _config.ProtocolVersion,
                Success = false,
                Error = error,
                ProcessingTimeMs = processingTimeMs,
                Timestamp = DateTime.UtcNow
            };
        }

        #endregion
    }

    #region Data Transfer Objects

    /// <summary>
    /// 连接请求数据
    /// </summary>
    public class ConnectionRequestData
    {
        [JsonPropertyName("deviceConfig")]
        public DeviceConfigData DeviceConfig { get; set; } = new();

        [JsonPropertyName("dataPoints")]
        public DataPointConfigData[]? DataPoints { get; set; }
    }

    /// <summary>
    /// 设备配置数据
    /// </summary>
    public class DeviceConfigData
    {
        [JsonPropertyName("type")]
        public string Type { get; set; } = "modbus-tcp";

        [JsonPropertyName("host")]
        public string Host { get; set; } = string.Empty;

        [JsonPropertyName("port")]
        public int Port { get; set; } = 502;

        [JsonPropertyName("timeout")]
        public int? Timeout { get; set; }

        [JsonPropertyName("settings")]
        public Dictionary<string, object>? Settings { get; set; }
    }

    /// <summary>
    /// 数据点配置数据
    /// </summary>
    public class DataPointConfigData
    {
        [JsonPropertyName("name")]
        public string Name { get; set; } = string.Empty;

        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = "int16";

        [JsonPropertyName("access")]
        public string Access { get; set; } = "read";

        [JsonPropertyName("description")]
        public string? Description { get; set; }

        [JsonPropertyName("unit")]
        public string? Unit { get; set; }

        [JsonPropertyName("scale")]
        public double? Scale { get; set; }

        [JsonPropertyName("offset")]
        public double? Offset { get; set; }
    }

    #endregion
}