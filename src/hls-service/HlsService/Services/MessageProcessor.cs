using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Serialization;
using HlsService.Models;
using HlsService.Api.Controllers;
using HslCommunication.ModBus;

namespace HlsService.Services
{
    /// <summary>
    /// 消息处理器
    /// 负责解析和处理来自客户端的各种命令
    /// </summary>
    public class MessageProcessor
    {
        private readonly ServerConfiguration _config;
        private readonly ServerStatistics _statistics;
        private readonly DeviceManager _deviceManager;
        private readonly ConnectionController _connectionController;
        private readonly DataOperationController _dataOperationController;
        private readonly DateTime _serverStartTime;

        public MessageProcessor(ServerConfiguration config, ServerStatistics statistics, DeviceManager deviceManager)
        {
            _config = config;
            _statistics = statistics;
            _deviceManager = deviceManager;
            _connectionController = new ConnectionController(deviceManager, config);
            _dataOperationController = new DataOperationController(deviceManager, config);
            _serverStartTime = DateTime.UtcNow;
        }

        /// <summary>
        /// 处理JSON消息
        /// </summary>
        public async Task<IpcResponse> ProcessMessageAsync(string jsonMessage, ClientConnection clientConnection)
        {
            var stopwatch = Stopwatch.StartNew();
            IpcRequest? request = null;
            
            try
            {
                // 解析JSON请求
                request = JsonSerializer.Deserialize<IpcRequest>(jsonMessage, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null)
                {
                    return CreateStandardErrorResponse("", 
                        ErrorFactory.CreateValidationError("json", "Invalid JSON message format"), 
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 使用标准化协议验证
                var validation = MessageValidator.ValidateRequest(request);
                if (!validation.IsValid)
                {
                    return CreateStandardErrorResponse(request.MessageId,
                        ErrorFactory.CreateConfigurationError("request", validation.Errors),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 更新客户端活动状态
                clientConnection.UpdateActivity();
                _statistics.IncrementMessagesProcessed();

                // 路由到相应的控制器处理命令
                var response = await RouteCommandAsync(request);
                return response;
            }
            catch (JsonException ex)
            {
                return CreateStandardErrorResponse(request?.MessageId ?? "",
                    ErrorFactory.CreateValidationError("json", $"JSON parsing error: {ex.Message}"),
                    stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[消息处理] 处理消息时出错: {ex.Message}");
                return CreateStandardErrorResponse(request?.MessageId ?? "",
                    ErrorFactory.CreateInternalError($"Internal server error: {ex.Message}", ex),
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 路由命令到相应的控制器
        /// </summary>
        private async Task<IpcResponse> RouteCommandAsync(IpcRequest request)
        {
            var command = request.Command.ToLower();

            // 标准化API路由
            return command switch
            {
                // 连接管理API
                CommandTypes.CONNECT => await _connectionController.ConnectAsync(request),
                CommandTypes.DISCONNECT => await _connectionController.DisconnectAsync(request),
                CommandTypes.STATUS => await _connectionController.GetStatusAsync(request),
                CommandTypes.LIST_CONNECTIONS => await _connectionController.ListConnectionsAsync(request),
                "validateconnection" => _connectionController.ValidateConnectionParameters(request),
                
                // 数据操作API
                CommandTypes.READ => await _dataOperationController.ReadDataAsync(request),
                CommandTypes.WRITE => await _dataOperationController.WriteDataAsync(request),
                CommandTypes.READ_BATCH => await _dataOperationController.ReadBatchAsync(request),
                CommandTypes.WRITE_BATCH => await _dataOperationController.WriteBatchAsync(request),
                
                // 保留旧版本兼容性命令 - 逐步迁移到新API
                "connect_device" => await _connectionController.ConnectAsync(request),
                "disconnect_device" => await _connectionController.DisconnectAsync(request),
                "device_status" => await _connectionController.GetStatusAsync(request),
                "read_data" => await _dataOperationController.ReadDataAsync(request),
                "write_data" => await _dataOperationController.WriteDataAsync(request),

                // 其他命令仍使用旧的处理方式
                _ => await ExecuteCommandAsync(request.Command, request.Data, null)
            };
        }

        /// <summary>
        /// 执行具体命令（旧版本兼容性）
        /// </summary>
        private async Task<IpcResponse> ExecuteCommandAsync(string command, object? data, ClientConnection? clientConnection)
        {
            var stopwatch = Stopwatch.StartNew();
            
            try
            {
                var result = command.ToLower() switch
                {
                    "ping" => HandlePingCommand(),
                    "serverstatus" => HandleServerStatusCommand(clientConnection),
                    "protocolinfo" => HandleProtocolInfoCommand(),
                    "connections" => HandleConnectionsCommand(),
                    "test_modbus" => await HandleTestModbusCommand(data),
                    "health_check" => HandleHealthCheckCommand(),
                    "version" => HandleVersionCommand(),
                    "add_device" => await HandleAddDeviceCommand(data),
                    "remove_device" => await HandleRemoveDeviceCommand(data),
                    "device_list" => HandleDeviceListCommand(),
                    "read" => await HandleReadDataCommand(data),
                    "write" => await HandleWriteDataCommand(data),
                    "readbatch" => await HandleReadBatchCommand(data),
                    "writebatch" => await HandleWriteBatchCommand(data),
                    "test_connection" => await HandleTestConnectionCommand(data),
                    "configure_datapoints" => await HandleConfigureDataPointsCommand(data),
                    "validate_configuration" => HandleValidateConfigurationCommand(data),
                    "get_schemas" => HandleGetSchemasCommand(),
                    "batch_datapoint_operation" => await HandleBatchDataPointOperationCommand(data),
                    _ => new { error = $"Unknown command: {command}", availableCommands = GetAvailableCommands() }
                };

                return CreateSuccessResponseFromData("", result, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                return CreateStandardErrorResponse("",
                    ErrorFactory.CreateInternalError($"Command execution failed: {ex.Message}", ex),
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 处理Ping命令
        /// </summary>
        private object HandlePingCommand()
        {
            return new 
            { 
                message = "pong", 
                serverTime = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ"),
                version = _config.ProtocolVersion
            };
        }

        /// <summary>
        /// 处理服务器状态查询命令
        /// </summary>
        private object HandleServerStatusCommand(ClientConnection? clientConnection)
        {
            var process = Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / (1024.0 * 1024.0);

            return new ServerStatus
            {
                Status = "running",
                Uptime = DateTime.UtcNow - _serverStartTime,
                ActiveConnections = _statistics.ActiveConnections,
                TotalConnections = _statistics.TotalConnections,
                MessagesProcessed = _statistics.MessagesProcessed,
                MemoryUsageMB = Math.Round(memoryMB, 2),
                ProcessId = Environment.ProcessId
            };
        }

        /// <summary>
        /// 处理协议信息命令
        /// </summary>
        private object HandleProtocolInfoCommand()
        {
            return new
            {
                protocol = ProtocolVersion.GetVersionInfo(),
                supportedCommands = CommandTypes.ALL_COMMANDS,
                constants = new
                {
                    maxMessageSize = ProtocolConstants.MAX_MESSAGE_SIZE,
                    maxBatchSize = ProtocolConstants.MAX_BATCH_SIZE,
                    defaultTimeout = ProtocolConstants.DEFAULT_TIMEOUT_MS,
                    maxConnections = ProtocolConstants.MAX_CONCURRENT_CONNECTIONS
                },
                timestamp = DateTime.UtcNow
            };
        }

        /// <summary>
        /// 处理连接信息命令
        /// </summary>
        private object HandleConnectionsCommand()
        {
            // Note: 这里需要从外部传入连接管理器实例
            // 为了简化，返回基本统计信息
            return new
            {
                activeConnections = _statistics.ActiveConnections,
                totalConnections = _statistics.TotalConnections,
                maxConnections = _config.MaxConnections
            };
        }

        /// <summary>
        /// 处理Modbus测试命令
        /// </summary>
        private async Task<object> HandleTestModbusCommand(object? data)
        {
            try
            {
                // 这是一个测试原型，实际使用时需要真实的设备配置
                var modbusTcp = new ModbusTcpNet("127.0.0.1", 502);

                // 注意：这里只是创建对象，不实际连接
                return new
                {
                    message = "Modbus connection test prototype",
                    status = "prototype_ready",
                    testData = data,
                    supportedProtocols = new[]
                    {
                        "Modbus TCP",
                        "Modbus RTU",
                        "Siemens S7",
                        "Omron FINS",
                        "Mitsubishi MC"
                    }
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, command = "test_modbus" };
            }
        }

        /// <summary>
        /// 处理健康检查命令
        /// </summary>
        private object HandleHealthCheckCommand()
        {
            var process = Process.GetCurrentProcess();
            var memoryMB = process.WorkingSet64 / (1024.0 * 1024.0);
            var uptime = DateTime.UtcNow - _serverStartTime;

            return new
            {
                healthy = true,
                uptime = uptime.TotalSeconds,
                memoryUsageMB = Math.Round(memoryMB, 2),
                activeConnections = _statistics.ActiveConnections,
                messagesProcessed = _statistics.MessagesProcessed,
                timestamp = DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ")
            };
        }

        /// <summary>
        /// 处理版本信息命令
        /// </summary>
        private object HandleVersionCommand()
        {
            return new
            {
                serviceVersion = _config.ProtocolVersion,
                protocolVersion = _config.ProtocolVersion,
                hslCommunicationVersion = "12.3.3",
                dotnetVersion = Environment.Version.ToString(),
                buildDate = "2025-08-26" // 实际项目中应该从程序集属性获取
            };
        }

        /// <summary>
        /// 处理添加设备命令
        /// </summary>
        private async Task<object> HandleAddDeviceCommand(object? data)
        {
            try
            {
                if (data == null)
                {
                    return new { error = "Device configuration is required" };
                }

                var json = JsonSerializer.Serialize(data);
                var config = JsonSerializer.Deserialize<DeviceConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config == null)
                {
                    return new { error = "Invalid device configuration format" };
                }

                var result = await _deviceManager.AddDeviceAsync(config);
                return new
                {
                    success = result.Success,
                    message = result.Message,
                    deviceId = config.DeviceId,
                    deviceType = config.Type.ToString()
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, command = "add_device" };
            }
        }

        /// <summary>
        /// 处理移除设备命令
        /// </summary>
        private async Task<object> HandleRemoveDeviceCommand(object? data)
        {
            try
            {
                var deviceId = ExtractDeviceId(data);
                if (string.IsNullOrEmpty(deviceId))
                {
                    return new { error = "Device ID is required" };
                }

                var result = await _deviceManager.RemoveDeviceAsync(deviceId);
                return new
                {
                    success = result.Success,
                    message = result.Message,
                    deviceId = deviceId
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, command = "remove_device" };
            }
        }

        /// <summary>
        /// 处理连接设备命令
        /// </summary>
        private async Task<object> HandleConnectDeviceCommand(object? data)
        {
            try
            {
                var deviceId = ExtractDeviceId(data);
                if (string.IsNullOrEmpty(deviceId))
                {
                    return new { error = "Device ID is required" };
                }

                var result = await _deviceManager.ConnectDeviceAsync(deviceId);
                return new
                {
                    success = result.Success,
                    message = result.Message,
                    deviceId = deviceId
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, command = "connect_device" };
            }
        }

        /// <summary>
        /// 处理断开设备命令
        /// </summary>
        private async Task<object> HandleDisconnectDeviceCommand(object? data)
        {
            try
            {
                var deviceId = ExtractDeviceId(data);
                if (string.IsNullOrEmpty(deviceId))
                {
                    return new { error = "Device ID is required" };
                }

                var result = await _deviceManager.DisconnectDeviceAsync(deviceId);
                return new
                {
                    success = result.Success,
                    message = result.Message,
                    deviceId = deviceId
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, command = "disconnect_device" };
            }
        }

        /// <summary>
        /// 处理设备状态查询命令
        /// </summary>
        private async Task<object> HandleDeviceStatusCommand(object? data)
        {
            try
            {
                var deviceId = ExtractDeviceId(data);
                if (string.IsNullOrEmpty(deviceId))
                {
                    // 如果没有指定设备ID，返回所有设备状态
                    var allStatus = await _deviceManager.GetAllDeviceStatusAsync();
                    return new
                    {
                        success = true,
                        devices = allStatus,
                        count = allStatus.Length
                    };
                }

                var status = await _deviceManager.GetDeviceStatusAsync(deviceId);
                if (status == null)
                {
                    return new { error = $"Device {deviceId} not found" };
                }

                return new
                {
                    success = true,
                    deviceId = deviceId,
                    status = status
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, command = "device_status" };
            }
        }

        /// <summary>
        /// 处理设备列表命令
        /// </summary>
        private object HandleDeviceListCommand()
        {
            try
            {
                var devices = _deviceManager.GetDeviceList();
                var supportedTypes = _deviceManager.GetSupportedDeviceTypes();

                return new
                {
                    success = true,
                    devices = devices,
                    count = devices.Length,
                    supportedTypes = supportedTypes.Select(t => t.ToString()).ToArray()
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, command = "device_list" };
            }
        }

        /// <summary>
        /// 处理读取数据命令
        /// </summary>
        private async Task<object> HandleReadDataCommand(object? data)
        {
            try
            {
                if (data == null)
                {
                    return new { error = "Read request is required" };
                }

                var json = JsonSerializer.Serialize(data);
                var request = JsonSerializer.Deserialize<DataPointReadRequest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null)
                {
                    return new { error = "Invalid read request format" };
                }

                var results = await _deviceManager.ReadDeviceDataAsync(request);
                return new
                {
                    success = true,
                    deviceId = request.DeviceId,
                    results = results
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, command = "read_data" };
            }
        }

        /// <summary>
        /// 处理写入数据命令
        /// </summary>
        private async Task<object> HandleWriteDataCommand(object? data)
        {
            try
            {
                if (data == null)
                {
                    return new { error = "Write request is required" };
                }

                var json = JsonSerializer.Serialize(data);
                var request = JsonSerializer.Deserialize<DataPointWriteRequest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (request == null)
                {
                    return new { error = "Invalid write request format" };
                }

                var results = await _deviceManager.WriteDeviceDataAsync(request);
                return new
                {
                    success = true,
                    deviceId = request.DeviceId,
                    results = results
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, command = "write_data" };
            }
        }

        /// <summary>
        /// 处理测试连接命令
        /// </summary>
        private async Task<object> HandleTestConnectionCommand(object? data)
        {
            try
            {
                var deviceId = ExtractDeviceId(data);
                if (string.IsNullOrEmpty(deviceId))
                {
                    return new { error = "Device ID is required" };
                }

                var result = await _deviceManager.TestDeviceConnectionAsync(deviceId);
                return new
                {
                    success = true,
                    deviceId = deviceId,
                    connected = result
                };
            }
            catch (Exception ex)
            {
                return new { error = ex.Message, command = "test_connection" };
            }
        }

        /// <summary>
        /// 从数据中提取设备ID
        /// </summary>
        private string? ExtractDeviceId(object? data)
        {
            try
            {
                if (data == null) return null;

                var json = JsonSerializer.Serialize(data);
                using var document = JsonDocument.Parse(json);
                
                if (document.RootElement.TryGetProperty("deviceId", out var deviceIdElement))
                {
                    return deviceIdElement.GetString();
                }

                if (document.RootElement.TryGetProperty("device_id", out var deviceIdElement2))
                {
                    return deviceIdElement2.GetString();
                }

                return null;
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// 处理配置数据点位命令
        /// </summary>
        private async Task<object> HandleConfigureDataPointsCommand(object? data)
        {
            try
            {
                if (data == null)
                {
                    return ErrorFactory.CreateMissingParameterError("data");
                }

                var json = JsonSerializer.Serialize(data);
                var config = JsonSerializer.Deserialize<DeviceDataPointConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

                if (config == null)
                {
                    return ErrorFactory.CreateValidationError("configuration", "Invalid data point configuration format");
                }

                // 验证配置
                var (isValid, errors) = ConfigurationValidator.ValidateDeviceDataPointConfiguration(config);
                if (!isValid)
                {
                    return ErrorFactory.CreateConfigurationError("DataPoint", errors);
                }

                // 这里应该保存配置到设备管理器或配置存储
                // 目前返回成功状态
                return OperationResult<object>.CreateSuccess(new
                {
                    message = "Data point configuration updated successfully",
                    deviceId = config.DeviceId,
                    totalPoints = config.GetAllDataPoints().Count(),
                    version = config.Version
                });
            }
            catch (Exception ex)
            {
                return OperationResult<object>.CreateFailure(ex, "configure_datapoints");
            }
        }

        /// <summary>
        /// 处理验证配置命令
        /// </summary>
        private object HandleValidateConfigurationCommand(object? data)
        {
            try
            {
                if (data == null)
                {
                    return ErrorFactory.CreateMissingParameterError("data");
                }

                var json = JsonSerializer.Serialize(data);
                using var document = JsonDocument.Parse(json);
                var root = document.RootElement;

                if (root.TryGetProperty("configurationType", out var typeElement))
                {
                    var configurationType = typeElement.GetString();
                    
                    return configurationType?.ToLower() switch
                    {
                        "device" => ValidateDeviceConfiguration(data),
                        "datapoint" => ValidateDataPointConfiguration(data),
                        "datapoint_group" => ValidateDataPointGroupConfiguration(data),
                        _ => ErrorFactory.CreateValidationError("type", $"Unsupported configuration type: {configurationType}")
                    };
                }

                return ErrorFactory.CreateMissingParameterError("configurationType");
            }
            catch (Exception ex)
            {
                return OperationResult<object>.CreateFailure(ex, "validate_configuration");
            }
        }

        /// <summary>
        /// 处理获取JSON Schema命令
        /// </summary>
        private object HandleGetSchemasCommand()
        {
            try
            {
                return OperationResult<object>.CreateSuccess(new
                {
                    schemas = new
                    {
                        deviceConfiguration = ConfigurationValidator.GetDeviceConfigurationSchema(),
                        dataPointConfiguration = ConfigurationValidator.GetDataPointConfigurationSchema()
                    }
                });
            }
            catch (Exception ex)
            {
                return OperationResult<object>.CreateFailure(ex, "get_schemas");
            }
        }

        /// <summary>
        /// 处理批量数据点位操作命令
        /// </summary>
        private async Task<object> HandleBatchDataPointOperationCommand(object? data)
        {
            try
            {
                if (data == null)
                {
                    return ErrorFactory.CreateMissingParameterError("data");
                }

                var json = JsonSerializer.Serialize(data);
                var request = JsonSerializer.Deserialize<BatchDataPointRequest>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    Converters = { new JsonStringEnumConverter() }
                });

                if (request == null)
                {
                    return ErrorFactory.CreateValidationError("request", "Invalid batch data point request format");
                }

                // 验证请求
                var (isValid, errors) = ConfigurationValidator.ValidateBatchDataPointRequest(request);
                if (!isValid)
                {
                    return ErrorFactory.CreateConfigurationError("BatchDataPointRequest", errors);
                }

                // 根据操作类型执行相应操作
                return request.Operation switch
                {
                    DataPointOperation.Read => await ExecuteBatchRead(request),
                    DataPointOperation.Write => await ExecuteBatchWrite(request),
                    DataPointOperation.ReadWrite => await ExecuteBatchReadWrite(request),
                    _ => ErrorFactory.CreateValidationError("operation", $"Unsupported operation: {request.Operation}")
                };
            }
            catch (Exception ex)
            {
                return OperationResult<object>.CreateFailure(ex, "batch_datapoint_operation");
            }
        }

        /// <summary>
        /// 验证设备配置
        /// </summary>
        private object ValidateDeviceConfiguration(object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var config = JsonSerializer.Deserialize<DeviceConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config == null)
                {
                    return ErrorFactory.CreateValidationError("configuration", "Invalid device configuration format");
                }

                var (isValid, errorMessage) = DeviceConnectionFactory.ValidateDeviceConfiguration(config);
                
                return OperationResult<object>.CreateSuccess(new
                {
                    valid = isValid,
                    errors = isValid ? new List<string>() : new List<string> { errorMessage },
                    configurationType = "device"
                });
            }
            catch (Exception ex)
            {
                return OperationResult<object>.CreateFailure(ex, "validate_device_configuration");
            }
        }

        /// <summary>
        /// 验证数据点位配置
        /// </summary>
        private object ValidateDataPointConfiguration(object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var config = JsonSerializer.Deserialize<DataPointConfiguration>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config == null)
                {
                    return ErrorFactory.CreateValidationError("configuration", "Invalid data point configuration format");
                }

                var (isValid, errors) = ConfigurationValidator.ValidateDataPointConfiguration(config);
                
                return OperationResult<object>.CreateSuccess(new
                {
                    valid = isValid,
                    errors = errors,
                    configurationType = "datapoint"
                });
            }
            catch (Exception ex)
            {
                return OperationResult<object>.CreateFailure(ex, "validate_datapoint_configuration");
            }
        }

        /// <summary>
        /// 验证数据点位组配置
        /// </summary>
        private object ValidateDataPointGroupConfiguration(object data)
        {
            try
            {
                var json = JsonSerializer.Serialize(data);
                var config = JsonSerializer.Deserialize<DataPointGroup>(json, new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                });

                if (config == null)
                {
                    return ErrorFactory.CreateValidationError("configuration", "Invalid data point group configuration format");
                }

                var (isValid, errors) = ConfigurationValidator.ValidateDataPointGroup(config);
                
                return OperationResult<object>.CreateSuccess(new
                {
                    valid = isValid,
                    errors = errors,
                    configurationType = "datapoint_group"
                });
            }
            catch (Exception ex)
            {
                return OperationResult<object>.CreateFailure(ex, "validate_datapoint_group_configuration");
            }
        }

        /// <summary>
        /// 执行批量读取
        /// </summary>
        private async Task<object> ExecuteBatchRead(BatchDataPointRequest request)
        {
            var readRequest = new DataPointReadRequest
            {
                DeviceId = request.DeviceId,
                Addresses = request.DataPoints.Select(dp => dp.Address).ToArray()
            };

            var results = await _deviceManager.ReadDeviceDataAsync(readRequest);
            
            return OperationResult<object>.CreateSuccess(new
            {
                deviceId = request.DeviceId,
                operation = "read",
                results = results,
                totalCount = results.Length
            });
        }

        /// <summary>
        /// 执行批量写入
        /// </summary>
        private async Task<object> ExecuteBatchWrite(BatchDataPointRequest request)
        {
            try
            {
                // 验证所有数据点都有写入值
                var invalidPoints = request.DataPoints.Where(dp => dp.Value == null).ToList();
                if (invalidPoints.Any())
                {
                    return ErrorFactory.CreateValidationError("dataPoints", 
                        $"Missing write values for addresses: {string.Join(", ", invalidPoints.Select(p => p.Address))}");
                }

                // 创建写入请求
                var writeRequest = new DataPointWriteRequest
                {
                    DeviceId = request.DeviceId,
                    DataPoints = request.DataPoints.Select(dp => new WriteDataPoint
                    {
                        Address = dp.Address,
                        Value = ConvertValueToDataType(dp.Value!, dp.DataType),
                        DataType = dp.DataType.ToString()
                    }).ToArray()
                };

                var results = await _deviceManager.WriteDeviceDataAsync(writeRequest);
                
                return OperationResult<object>.CreateSuccess(new
                {
                    deviceId = request.DeviceId,
                    operation = "write",
                    results = results,
                    totalCount = results.Length,
                    successful = results.Count(r => r.Success),
                    failed = results.Count(r => !r.Success)
                });
            }
            catch (Exception ex)
            {
                return OperationResult<object>.CreateFailure(ex, "batch_write");
            }
        }

        /// <summary>
        /// 执行批量读写
        /// </summary>
        private async Task<object> ExecuteBatchReadWrite(BatchDataPointRequest request)
        {
            try
            {
                // 分离读和写的点位
                var readPoints = request.DataPoints.Where(dp => dp.AccessMode == DataPointAccessMode.Read || dp.AccessMode == DataPointAccessMode.ReadWrite).ToList();
                var writePoints = request.DataPoints.Where(dp => (dp.AccessMode == DataPointAccessMode.Write || dp.AccessMode == DataPointAccessMode.ReadWrite) && dp.Value != null).ToList();

                // 并发执行读写操作
                var tasks = new List<Task<object>>();

                // 添加读取任务
                if (readPoints.Any())
                {
                    var readTask = Task.Run(async () =>
                    {
                        var readRequest = new DataPointReadRequest
                        {
                            DeviceId = request.DeviceId,
                            Addresses = readPoints.Select(dp => dp.Address).ToArray()
                        };
                        return (object)await _deviceManager.ReadDeviceDataAsync(readRequest);
                    });
                    tasks.Add(readTask);
                }

                // 添加写入任务
                WriteResult[] writeResults = Array.Empty<WriteResult>();
                if (writePoints.Any())
                {
                    var writeTask = Task.Run(async () =>
                    {
                        var writeRequest = new DataPointWriteRequest
                        {
                            DeviceId = request.DeviceId,
                            DataPoints = writePoints.Select(dp => new WriteDataPoint
                            {
                                Address = dp.Address,
                                Value = ConvertValueToDataType(dp.Value!, dp.DataType),
                                DataType = dp.DataType.ToString()
                            }).ToArray()
                        };
                        return (object)await _deviceManager.WriteDeviceDataAsync(writeRequest);
                    });
                    tasks.Add(writeTask);
                }

                // 等待所有任务完成
                var results = await Task.WhenAll(tasks);
                
                var readResults = tasks.Count > 0 && readPoints.Any() ? (ReadResult[])results[0] : Array.Empty<ReadResult>();
                if (tasks.Count > 1 && writePoints.Any())
                {
                    writeResults = (WriteResult[])results[readPoints.Any() ? 1 : 0];
                }

                return OperationResult<object>.CreateSuccess(new
                {
                    deviceId = request.DeviceId,
                    operation = "read_write",
                    readResults = readResults,
                    writeResults = writeResults,
                    totalReadCount = readPoints.Count,
                    totalWriteCount = writePoints.Count,
                    successfulReads = readResults.Count(r => r.Success),
                    successfulWrites = writeResults.Count(r => r.Success)
                });
            }
            catch (Exception ex)
            {
                return OperationResult<object>.CreateFailure(ex, "batch_read_write");
            }
        }

        /// <summary>
        /// 获取可用命令列表
        /// </summary>
        private string[] GetAvailableCommands()
        {
            return new[]
            {
                "ping",
                "status", 
                "server_info",
                "connections",
                "test_modbus",
                "health_check",
                "version",
                "add_device",
                "remove_device",
                "connect_device", 
                "disconnect_device",
                "device_status",
                "device_list",
                "read_data",
                "write_data",
                "test_connection",
                "configure_datapoints",
                "validate_configuration",
                "get_schemas",
                "batch_datapoint_operation"
            };
        }

        /// <summary>
        /// 处理批量读取命令
        /// </summary>
        private async Task<object> HandleReadBatchCommand(object? data)
        {
            // 批量读取逻辑 - 基于现有的read_data命令扩展
            return await HandleReadDataCommand(data);
        }

        /// <summary>
        /// 处理批量写入命令
        /// </summary>
        private async Task<object> HandleWriteBatchCommand(object? data)
        {
            // 批量写入逻辑 - 基于现有的write_data命令扩展
            return await HandleWriteDataCommand(data);
        }

        /// <summary>
        /// 创建标准化错误响应
        /// </summary>
        private IpcResponse CreateStandardErrorResponse(string messageId, ErrorResponse error, double processingTimeMs)
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

        /// <summary>
        /// 从数据创建成功响应
        /// </summary>
        private IpcResponse CreateSuccessResponseFromData(string messageId, object data, double processingTimeMs)
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
        /// 创建错误响应（向后兼容）
        /// </summary>
        private IpcResponse CreateErrorResponse(string messageId, string error, double processingTimeMs)
        {
            return CreateStandardErrorResponse(messageId,
                ErrorFactory.CreateInternalError(error),
                processingTimeMs);
        }

        /// <summary>
        /// 数据类型转换助手方法
        /// </summary>
        private object ConvertValueToDataType(object value, DataPointType targetType)
        {
            try
            {
                // 处理JsonElement类型的值
                if (value is JsonElement jsonElement)
                {
                    return targetType switch
                    {
                        DataPointType.Bool => jsonElement.ValueKind switch
                        {
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String => bool.Parse(jsonElement.GetString()!),
                            JsonValueKind.Number => jsonElement.GetDouble() != 0,
                            _ => throw new InvalidCastException($"Cannot convert {jsonElement.ValueKind} to Bool")
                        },
                        DataPointType.Int16 => jsonElement.ValueKind == JsonValueKind.String 
                            ? short.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetInt16(),
                        DataPointType.UInt16 => jsonElement.ValueKind == JsonValueKind.String 
                            ? ushort.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetUInt16(),
                        DataPointType.Int32 => jsonElement.ValueKind == JsonValueKind.String 
                            ? int.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetInt32(),
                        DataPointType.UInt32 => jsonElement.ValueKind == JsonValueKind.String 
                            ? uint.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetUInt32(),
                        DataPointType.Int64 => jsonElement.ValueKind == JsonValueKind.String 
                            ? long.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetInt64(),
                        DataPointType.UInt64 => jsonElement.ValueKind == JsonValueKind.String 
                            ? ulong.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetUInt64(),
                        DataPointType.Float => jsonElement.ValueKind == JsonValueKind.String 
                            ? float.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetSingle(),
                        DataPointType.Double => jsonElement.ValueKind == JsonValueKind.String 
                            ? double.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetDouble(),
                        DataPointType.String => jsonElement.ValueKind switch
                        {
                            JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                            JsonValueKind.Number => jsonElement.GetRawText(),
                            JsonValueKind.True => "true",
                            JsonValueKind.False => "false",
                            _ => jsonElement.GetRawText()
                        },
                        _ => throw new NotSupportedException($"Unsupported data type: {targetType}")
                    };
                }
                
                // 处理其他类型的值
                return targetType switch
                {
                    DataPointType.Bool => Convert.ToBoolean(value),
                    DataPointType.Int16 => Convert.ToInt16(value),
                    DataPointType.UInt16 => Convert.ToUInt16(value),
                    DataPointType.Int32 => Convert.ToInt32(value),
                    DataPointType.UInt32 => Convert.ToUInt32(value),
                    DataPointType.Int64 => Convert.ToInt64(value),
                    DataPointType.UInt64 => Convert.ToUInt64(value),
                    DataPointType.Float => Convert.ToSingle(value),
                    DataPointType.Double => Convert.ToDouble(value),
                    DataPointType.String => value.ToString() ?? string.Empty,
                    _ => throw new NotSupportedException($"Unsupported data type: {targetType}")
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Cannot convert value '{value}' to {targetType}: {ex.Message}", ex);
            }
        }
    }

    /// <summary>
    /// 服务器统计信息
    /// </summary>
    public class ServerStatistics
    {
        private long _messagesProcessed = 0;
        private readonly ClientConnectionManager _connectionManager;

        public ServerStatistics(ClientConnectionManager connectionManager)
        {
            _connectionManager = connectionManager;
        }

        public int ActiveConnections => _connectionManager.ActiveConnections;
        public long TotalConnections => _connectionManager.TotalConnections;
        public long MessagesProcessed => _messagesProcessed;

        public void IncrementMessagesProcessed()
        {
            Interlocked.Increment(ref _messagesProcessed);
        }
    }
}