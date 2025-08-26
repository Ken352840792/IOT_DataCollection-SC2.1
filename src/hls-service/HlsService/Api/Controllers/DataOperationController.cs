using System.Text.Json;
using System.Text.Json.Serialization;
using HlsService.Models;
using HlsService.Services;

namespace HlsService.Api.Controllers
{
    /// <summary>
    /// 数据操作API控制器
    /// 处理设备数据读取、写入等操作
    /// </summary>
    public class DataOperationController
    {
        private readonly DeviceManager _deviceManager;
        private readonly ServerConfiguration _config;

        public DataOperationController(DeviceManager deviceManager, ServerConfiguration config)
        {
            _deviceManager = deviceManager;
            _config = config;
        }

        /// <summary>
        /// 单点数据读取
        /// 支持的命令: "read"
        /// </summary>
        public async Task<IpcResponse> ReadDataAsync(IpcRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 解析读取数据
                var readData = ParseReadRequest(request.Data);
                if (readData == null)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateValidationError("data", "Invalid read data format"),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 验证连接存在
                if (!await ValidateConnection(readData.ConnectionId))
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateDeviceNotFoundError(readData.ConnectionId),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 验证数据点数量限制
                if (readData.DataPoints?.Length > ProtocolConstants.MAX_BATCH_SIZE)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateValidationError("dataPoints", 
                            $"Too many data points: {readData.DataPoints.Length} (max: {ProtocolConstants.MAX_BATCH_SIZE})"),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 构建数据读取请求
                var dataRequest = new DataPointReadRequest
                {
                    DeviceId = readData.ConnectionId,
                    Addresses = readData.DataPoints ?? Array.Empty<string>()
                };

                // 执行数据读取
                var results = await _deviceManager.ReadDeviceDataAsync(dataRequest);

                // 构建标准化响应
                var responseData = new
                {
                    connectionId = readData.ConnectionId,
                    operation = "read",
                    requestedCount = dataRequest.Addresses.Length,
                    results = results.Select(r => new
                    {
                        address = r.Address,
                        value = r.Value,
                        dataType = r.DataType ?? "unknown",
                        quality = "good", // 默认质量，因为ReadResult没有Quality属性
                        success = r.Success,
                        timestamp = r.Timestamp,
                        responseTimeMs = r.ResponseTimeMs,
                        error = r.Success ? null : new
                        {
                            code = ErrorCodes.READ_TIMEOUT,
                            message = ErrorCodes.GetErrorDescription(ErrorCodes.READ_TIMEOUT),
                            details = new[] { r.Error ?? "Read operation failed" }
                        }
                    }).ToArray(),
                    summary = new
                    {
                        successful = results.Count(r => r.Success),
                        failed = results.Count(r => !r.Success),
                        totalProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
                    }
                };

                return CreateSuccessResponse(request.MessageId, responseData, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(request.MessageId,
                    ErrorFactory.CreateInternalError($"Data read operation failed: {ex.Message}", ex),
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 批量数据读取
        /// 支持的命令: "readBatch"
        /// </summary>
        public async Task<IpcResponse> ReadBatchAsync(IpcRequest request)
        {
            // 批量读取与单点读取使用相同的逻辑，只是数据点数量可能更多
            return await ReadDataAsync(request);
        }

        /// <summary>
        /// 单点数据写入
        /// 支持的命令: "write"
        /// </summary>
        public async Task<IpcResponse> WriteDataAsync(IpcRequest request)
        {
            var stopwatch = System.Diagnostics.Stopwatch.StartNew();

            try
            {
                // 解析写入数据
                var writeData = ParseWriteRequest(request.Data);
                if (writeData == null)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateValidationError("data", "Invalid write data format"),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 验证连接存在
                if (!await ValidateConnection(writeData.ConnectionId))
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateDeviceNotFoundError(writeData.ConnectionId),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 验证数据点数量限制
                if (writeData.DataPoints?.Length > ProtocolConstants.MAX_BATCH_SIZE)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateValidationError("dataPoints", 
                            $"Too many data points: {writeData.DataPoints.Length} (max: {ProtocolConstants.MAX_BATCH_SIZE})"),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 验证所有数据点都有值
                var invalidPoints = writeData.DataPoints?.Where(dp => dp.Value == null).ToList();
                if (invalidPoints?.Any() == true)
                {
                    return CreateErrorResponse(request.MessageId,
                        ErrorFactory.CreateValidationError("dataPoints", 
                            $"Missing values for addresses: {string.Join(", ", invalidPoints.Select(p => p.Address))}"),
                        stopwatch.Elapsed.TotalMilliseconds);
                }

                // 构建数据写入请求
                var dataRequest = new DataPointWriteRequest
                {
                    DeviceId = writeData.ConnectionId,
                    DataPoints = writeData.DataPoints?.Select(dp => new WriteDataPoint
                    {
                        Address = dp.Address,
                        Value = ConvertAndValidateValue(dp.Value!, dp.DataType),
                        DataType = dp.DataType
                    }).ToArray() ?? Array.Empty<WriteDataPoint>()
                };

                // 执行数据写入
                var results = await _deviceManager.WriteDeviceDataAsync(dataRequest);

                // 构建标准化响应
                var responseData = new
                {
                    connectionId = writeData.ConnectionId,
                    operation = "write",
                    requestedCount = dataRequest.DataPoints.Length,
                    results = results.Select(r => new
                    {
                        address = r.Address,
                        value = r.Value,
                        dataType = "unknown", // WriteResult没有DataType属性
                        success = r.Success,
                        timestamp = r.Timestamp,
                        responseTimeMs = r.ResponseTimeMs,
                        error = r.Success ? null : new
                        {
                            code = ErrorCodes.WRITE_FAILED,
                            message = ErrorCodes.GetErrorDescription(ErrorCodes.WRITE_FAILED),
                            details = new[] { r.Error ?? "Write operation failed" }
                        }
                    }).ToArray(),
                    summary = new
                    {
                        successful = results.Count(r => r.Success),
                        failed = results.Count(r => !r.Success),
                        totalProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds
                    }
                };

                return CreateSuccessResponse(request.MessageId, responseData, stopwatch.Elapsed.TotalMilliseconds);
            }
            catch (Exception ex)
            {
                return CreateErrorResponse(request.MessageId,
                    ErrorFactory.CreateInternalError($"Data write operation failed: {ex.Message}", ex),
                    stopwatch.Elapsed.TotalMilliseconds);
            }
        }

        /// <summary>
        /// 批量数据写入
        /// 支持的命令: "writeBatch"
        /// </summary>
        public async Task<IpcResponse> WriteBatchAsync(IpcRequest request)
        {
            // 批量写入与单点写入使用相同的逻辑，只是数据点数量可能更多
            return await WriteDataAsync(request);
        }

        #region Private Helper Methods

        /// <summary>
        /// 解析读取请求数据
        /// </summary>
        private ReadRequestData? ParseReadRequest(object? data)
        {
            if (data == null) return null;

            try
            {
                var json = JsonSerializer.Serialize(data);
                return JsonSerializer.Deserialize<ReadRequestData>(json, new JsonSerializerOptions
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
        /// 解析写入请求数据
        /// </summary>
        private WriteRequestData? ParseWriteRequest(object? data)
        {
            if (data == null) return null;

            try
            {
                var json = JsonSerializer.Serialize(data);
                return JsonSerializer.Deserialize<WriteRequestData>(json, new JsonSerializerOptions
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
        /// 验证连接是否存在
        /// </summary>
        private async Task<bool> ValidateConnection(string connectionId)
        {
            try
            {
                var status = await _deviceManager.GetDeviceStatusAsync(connectionId);
                return status != null;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// 转换和验证数据值
        /// </summary>
        private object ConvertAndValidateValue(object value, string dataType)
        {
            try
            {
                // 处理JsonElement类型的值
                if (value is JsonElement jsonElement)
                {
                    return dataType?.ToLower() switch
                    {
                        "bool" or "boolean" => jsonElement.ValueKind switch
                        {
                            JsonValueKind.True => true,
                            JsonValueKind.False => false,
                            JsonValueKind.String => bool.Parse(jsonElement.GetString()!),
                            JsonValueKind.Number => jsonElement.GetDouble() != 0,
                            _ => throw new InvalidCastException($"Cannot convert {jsonElement.ValueKind} to Bool")
                        },
                        "int16" => jsonElement.ValueKind == JsonValueKind.String 
                            ? short.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetInt16(),
                        "uint16" => jsonElement.ValueKind == JsonValueKind.String 
                            ? ushort.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetUInt16(),
                        "int32" => jsonElement.ValueKind == JsonValueKind.String 
                            ? int.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetInt32(),
                        "uint32" => jsonElement.ValueKind == JsonValueKind.String 
                            ? uint.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetUInt32(),
                        "int64" => jsonElement.ValueKind == JsonValueKind.String 
                            ? long.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetInt64(),
                        "uint64" => jsonElement.ValueKind == JsonValueKind.String 
                            ? ulong.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetUInt64(),
                        "float" => jsonElement.ValueKind == JsonValueKind.String 
                            ? float.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetSingle(),
                        "double" => jsonElement.ValueKind == JsonValueKind.String 
                            ? double.Parse(jsonElement.GetString()!) 
                            : jsonElement.GetDouble(),
                        "string" => jsonElement.ValueKind switch
                        {
                            JsonValueKind.String => jsonElement.GetString() ?? string.Empty,
                            JsonValueKind.Number => jsonElement.GetRawText(),
                            JsonValueKind.True => "true",
                            JsonValueKind.False => "false",
                            _ => jsonElement.GetRawText()
                        },
                        _ => throw new NotSupportedException($"Unsupported data type: {dataType}")
                    };
                }
                
                // 处理其他类型的值
                return dataType?.ToLower() switch
                {
                    "bool" or "boolean" => Convert.ToBoolean(value),
                    "int16" => Convert.ToInt16(value),
                    "uint16" => Convert.ToUInt16(value),
                    "int32" => Convert.ToInt32(value),
                    "uint32" => Convert.ToUInt32(value),
                    "int64" => Convert.ToInt64(value),
                    "uint64" => Convert.ToUInt64(value),
                    "float" => Convert.ToSingle(value),
                    "double" => Convert.ToDouble(value),
                    "string" => value.ToString() ?? string.Empty,
                    _ => throw new NotSupportedException($"Unsupported data type: {dataType}")
                };
            }
            catch (Exception ex)
            {
                throw new ArgumentException($"Cannot convert value '{value}' to {dataType}: {ex.Message}", ex);
            }
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
    /// 读取请求数据
    /// </summary>
    public class ReadRequestData
    {
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;

        [JsonPropertyName("dataPoints")]
        public string[]? DataPoints { get; set; }

        [JsonPropertyName("options")]
        public ReadOptions? Options { get; set; }
    }

    /// <summary>
    /// 写入请求数据
    /// </summary>
    public class WriteRequestData
    {
        [JsonPropertyName("connectionId")]
        public string ConnectionId { get; set; } = string.Empty;

        [JsonPropertyName("dataPoints")]
        public WriteDataPointData[]? DataPoints { get; set; }

        [JsonPropertyName("options")]
        public WriteOptions? Options { get; set; }
    }

    /// <summary>
    /// 写入数据点数据
    /// </summary>
    public class WriteDataPointData
    {
        [JsonPropertyName("address")]
        public string Address { get; set; } = string.Empty;

        [JsonPropertyName("value")]
        public object? Value { get; set; }

        [JsonPropertyName("dataType")]
        public string DataType { get; set; } = "int16";
    }

    /// <summary>
    /// 读取选项
    /// </summary>
    public class ReadOptions
    {
        [JsonPropertyName("timeout")]
        public int? Timeout { get; set; }

        [JsonPropertyName("retryCount")]
        public int? RetryCount { get; set; }

        [JsonPropertyName("includeQuality")]
        public bool IncludeQuality { get; set; } = true;
    }

    /// <summary>
    /// 写入选项
    /// </summary>
    public class WriteOptions
    {
        [JsonPropertyName("timeout")]
        public int? Timeout { get; set; }

        [JsonPropertyName("validateBeforeWrite")]
        public bool ValidateBeforeWrite { get; set; } = true;

        [JsonPropertyName("retryCount")]
        public int? RetryCount { get; set; }
    }

    #endregion
}