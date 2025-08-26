# HLS通信服务 API参考文档

## 版本: 1.0
## 最后更新: 2025-08-26

---

## 概述

本文档提供HLS-Communication服务的完整API参考，包含所有可用的命令、参数说明、响应格式和错误代码。

## API端点信息

- **通信协议**: TCP Socket / IPC
- **默认端口**: 8888
- **数据格式**: JSON
- **编码**: UTF-8

## 基础API结构

### 通用请求格式

```json
{
  "version": "1.0",
  "messageId": "唯一消息标识符",
  "timestamp": "ISO8601时间戳",
  "command": "命令名称",
  "data": {
    // 命令特定的数据载荷
  }
}
```

### 通用响应格式

```json
{
  "version": "1.0",
  "messageId": "对应请求的消息ID",
  "timestamp": "响应时间戳",
  "success": true,
  "processingTimeMs": 45.2,
  "data": {
    // 响应数据
  },
  "error": {
    // 错误时包含错误信息
    "code": "错误代码",
    "message": "错误描述",
    "details": ["详细信息"],
    "type": "错误类型",
    "retryable": false,
    "retryDelayMs": 1000,
    "resourceId": "相关资源ID"
  }
}
```

---

## 连接管理API

### 1. connect - 建立设备连接

建立与指定设备的连接，并配置数据点。

**请求格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_001",
  "timestamp": "2025-08-26T10:00:00Z",
  "command": "connect",
  "data": {
    "deviceConfig": {
      "type": "modbus-tcp",
      "host": "192.168.1.100",
      "port": 502,
      "timeout": 5000,
      "settings": {
        // 可选的设备特定设置
      }
    },
    "dataPoints": [
      {
        "name": "temperature",
        "address": "40001",
        "dataType": "float",
        "access": "read",
        "description": "温度传感器",
        "unit": "°C",
        "scale": 1.0,
        "offset": 0.0
      }
    ]
  }
}
```

**响应格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_001",
  "timestamp": "2025-08-26T10:00:01Z",
  "success": true,
  "processingTimeMs": 456.2,
  "data": {
    "connectionId": "conn_20250826100000_1234",
    "status": "connected",
    "deviceInfo": {
      "type": "modbus-tcp",
      "host": "192.168.1.100",
      "port": 502,
      "timeout": 5000
    },
    "dataPointsConfigured": 1
  }
}
```

**参数说明:**

| 字段 | 类型 | 必需 | 描述 |
|------|------|------|------|
| `deviceConfig.type` | string | 是 | 设备类型: "modbus-tcp", "modbus-rtu", "siemens-s7" |
| `deviceConfig.host` | string | 是 | 设备IP地址 |
| `deviceConfig.port` | number | 是 | 设备端口号 |
| `deviceConfig.timeout` | number | 否 | 连接超时时间(ms)，默认5000 |
| `dataPoints` | array | 否 | 数据点配置数组 |
| `dataPoints[].name` | string | 是 | 数据点名称 |
| `dataPoints[].address` | string | 是 | 设备地址 |
| `dataPoints[].dataType` | string | 是 | 数据类型: "bool", "int16", "uint16", "int32", "uint32", "int64", "uint64", "float", "double", "string" |
| `dataPoints[].access` | string | 是 | 访问权限: "read", "write", "readwrite" |

---

### 2. disconnect - 断开设备连接

断开指定的设备连接。

**请求格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_002",
  "timestamp": "2025-08-26T10:01:00Z",
  "command": "disconnect",
  "data": {
    "connectionId": "conn_20250826100000_1234"
  }
}
```

**响应格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_002",
  "timestamp": "2025-08-26T10:01:00Z",
  "success": true,
  "processingTimeMs": 123.4,
  "data": {
    "connectionId": "conn_20250826100000_1234",
    "status": "disconnected",
    "message": "Device disconnected and removed successfully"
  }
}
```

---

### 3. status - 查询连接状态

查询指定连接的当前状态。

**请求格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_003",
  "timestamp": "2025-08-26T10:02:00Z",
  "command": "status",
  "data": {
    "connectionId": "conn_20250826100000_1234"
  }
}
```

**响应格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_003",
  "timestamp": "2025-08-26T10:02:00Z",
  "success": true,
  "processingTimeMs": 89.1,
  "data": {
    "connectionId": "conn_20250826100000_1234",
    "status": "connected",
    "lastActivity": "2025-08-26T10:01:30Z",
    "deviceInfo": {
      "type": "modbus-tcp",
      "host": "192.168.1.100",
      "port": 502
    },
    "connectionHealth": {
      "isAlive": true,
      "lastCheckTime": "2025-08-26T10:02:00Z",
      "responseTime": 89.1
    }
  }
}
```

---

### 4. listConnections - 列出所有连接

获取所有活动连接的列表。

**请求格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_004",
  "timestamp": "2025-08-26T10:03:00Z",
  "command": "listConnections"
}
```

**响应格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_004",
  "timestamp": "2025-08-26T10:03:00Z",
  "success": true,
  "processingTimeMs": 67.8,
  "data": {
    "connections": [
      {
        "connectionId": "conn_20250826100000_1234",
        "deviceType": "ModbusTcp",
        "host": "192.168.1.100",
        "port": 502,
        "status": "connected",
        "addedTime": "2025-08-26T10:00:00Z"
      }
    ],
    "totalCount": 1,
    "activeCount": 1,
    "maxConnections": 10,
    "timestamp": "2025-08-26T10:03:00Z"
  }
}
```

---

### 5. validateConnection - 验证连接参数

验证连接配置和数据点设置的有效性。

**请求格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_005",
  "timestamp": "2025-08-26T10:04:00Z",
  "command": "validateConnection",
  "data": {
    "deviceConfig": {
      "type": "modbus-tcp",
      "host": "192.168.1.100",
      "port": 502,
      "timeout": 5000
    },
    "dataPoints": [
      {
        "name": "test_point",
        "address": "40001",
        "dataType": "int16",
        "access": "read"
      }
    ]
  }
}
```

**响应格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_005",
  "timestamp": "2025-08-26T10:04:00Z",
  "success": true,
  "processingTimeMs": 34.5,
  "data": {
    "valid": true,
    "errors": [],
    "deviceConfig": {
      "type": "modbus-tcp",
      "host": "192.168.1.100",
      "port": 502,
      "timeout": 5000
    },
    "dataPointsCount": 1,
    "validation": {
      "timestamp": "2025-08-26T10:04:00Z",
      "validatorVersion": "1.0"
    }
  }
}
```

---

## 数据操作API

### 6. read - 读取数据

读取指定数据点的当前值。

**请求格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_006",
  "timestamp": "2025-08-26T10:05:00Z",
  "command": "read",
  "data": {
    "connectionId": "conn_20250826100000_1234",
    "dataPoints": ["40001", "40002"],
    "options": {
      "timeout": 5000,
      "retryCount": 3,
      "includeQuality": true
    }
  }
}
```

**响应格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_006",
  "timestamp": "2025-08-26T10:05:00Z",
  "success": true,
  "processingTimeMs": 123.4,
  "data": {
    "connectionId": "conn_20250826100000_1234",
    "operation": "read",
    "requestedCount": 2,
    "results": [
      {
        "address": "40001",
        "value": 25.6,
        "dataType": "float",
        "quality": "good",
        "success": true,
        "timestamp": "2025-08-26T10:05:00Z",
        "responseTimeMs": 45.2,
        "error": null
      },
      {
        "address": "40002",
        "value": 68,
        "dataType": "int16",
        "quality": "good",
        "success": true,
        "timestamp": "2025-08-26T10:05:00Z",
        "responseTimeMs": 43.1,
        "error": null
      }
    ],
    "summary": {
      "successful": 2,
      "failed": 0,
      "totalProcessingTimeMs": 123.4
    }
  }
}
```

---

### 7. readBatch - 批量读取数据

批量读取多个数据点的值（最多100个数据点）。

**请求格式:** 与 `read` 相同
**响应格式:** 与 `read` 相同

---

### 8. write - 写入数据

向指定数据点写入数值。

**请求格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_007",
  "timestamp": "2025-08-26T10:06:00Z",
  "command": "write",
  "data": {
    "connectionId": "conn_20250826100000_1234",
    "dataPoints": [
      {
        "address": "40001",
        "value": 30.5,
        "dataType": "float"
      },
      {
        "address": "40002",
        "value": 100,
        "dataType": "int16"
      }
    ],
    "options": {
      "timeout": 5000,
      "validateBeforeWrite": true,
      "retryCount": 3
    }
  }
}
```

**响应格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_007",
  "timestamp": "2025-08-26T10:06:00Z",
  "success": true,
  "processingTimeMs": 156.7,
  "data": {
    "connectionId": "conn_20250826100000_1234",
    "operation": "write",
    "requestedCount": 2,
    "results": [
      {
        "address": "40001",
        "value": 30.5,
        "dataType": "unknown",
        "success": true,
        "timestamp": "2025-08-26T10:06:00Z",
        "responseTimeMs": 78.3,
        "error": null
      },
      {
        "address": "40002",
        "value": 100,
        "dataType": "unknown",
        "success": true,
        "timestamp": "2025-08-26T10:06:00Z",
        "responseTimeMs": 76.4,
        "error": null
      }
    ],
    "summary": {
      "successful": 2,
      "failed": 0,
      "totalProcessingTimeMs": 156.7
    }
  }
}
```

---

### 9. writeBatch - 批量写入数据

批量写入多个数据点的值（最多100个数据点）。

**请求格式:** 与 `write` 相同
**响应格式:** 与 `write` 相同

---

## 服务器管理API

### 10. serverStatus - 获取服务器状态

获取服务器的运行状态信息。

**请求格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_008",
  "timestamp": "2025-08-26T10:07:00Z",
  "command": "serverStatus"
}
```

**响应格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_008",
  "timestamp": "2025-08-26T10:07:00Z",
  "success": true,
  "processingTimeMs": 12.3,
  "data": {
    "status": "running",
    "uptime": "01:30:45",
    "activeConnections": 3,
    "totalConnections": 15,
    "messagesProcessed": 1247,
    "hslCommunicationVersion": "12.3.3",
    "dotnetVersion": "8.0.0",
    "memoryUsageMB": 156.7,
    "processId": 12345
  }
}
```

---

### 11. protocolInfo - 获取协议信息

获取支持的协议版本和功能信息。

**请求格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_009",
  "timestamp": "2025-08-26T10:08:00Z",
  "command": "protocolInfo"
}
```

**响应格式:**
```json
{
  "version": "1.0",
  "messageId": "msg_009",
  "timestamp": "2025-08-26T10:08:00Z",
  "success": true,
  "processingTimeMs": 8.7,
  "data": {
    "protocol": {
      "current": "1.0",
      "supported": ["1.0"],
      "features": [
        "Basic device connection management",
        "Data read/write operations",
        "Batch operations support",
        "Error handling and status codes",
        "Message validation",
        "Connection status monitoring"
      ]
    },
    "supportedCommands": [
      "connect", "disconnect", "status", "listConnections",
      "read", "write", "readBatch", "writeBatch",
      "serverStatus", "protocolInfo"
    ],
    "constants": {
      "maxMessageSize": 1048576,
      "maxBatchSize": 100,
      "defaultTimeout": 5000,
      "maxConnections": 10
    },
    "timestamp": "2025-08-26T10:08:00Z"
  }
}
```

---

## 错误代码参考

### 错误代码分类

#### 系统级错误 (1xxx)
- `1000`: 系统内部错误
- `1001`: 无效的请求
- `1002`: 参数无效
- `1003`: 权限拒绝
- `1004`: 缺少必需参数
- `1005`: 格式无效
- `1006`: 数值超出有效范围
- `1007`: 系统资源耗尽
- `1008`: 服务不可用
- `1009`: 操作不支持

#### 设备连接错误 (2xxx)
- `2001`: 设备未找到
- `2002`: 设备离线
- `2003`: 连接超时
- `2004`: 连接失败
- `2005`: 设备已存在
- `2006`: 设备未连接
- `2007`: 设备通信协议错误
- `2008`: 网络不可达
- `2009`: 连接被拒绝
- `2010`: 套接字错误

#### 数据操作错误 (3xxx)
- `3001`: 地址无效
- `3002`: 数据类型错误
- `3003`: 读取超时
- `3004`: 写入失败
- `3005`: 读取权限拒绝
- `3006`: 写入权限拒绝
- `3007`: 数据转换错误
- `3008`: 不支持的数据类型
- `3009`: 数据超出范围

#### 配置相关错误 (4xxx)
- `4001`: 配置无效
- `4002`: 配置未找到
- `4003`: 配置冲突

#### IPC通信错误 (5xxx)
- `5001`: 消息格式无效
- `5002`: 消息过大
- `5003`: 命令未找到
- `5004`: 不支持的协议版本
- `5005`: 消息ID无效
- `5006`: 消息时间戳无效

### 错误响应示例

```json
{
  "version": "1.0",
  "messageId": "msg_error",
  "timestamp": "2025-08-26T10:09:00Z",
  "success": false,
  "processingTimeMs": 23.4,
  "error": {
    "code": "2004",
    "message": "连接失败",
    "details": [
      "Device ID: conn_test_123",
      "Connection refused by remote host"
    ],
    "type": "Network",
    "retryable": true,
    "retryDelayMs": 3000,
    "resourceId": "conn_test_123",
    "timestamp": "2025-08-26T10:09:00Z"
  }
}
```

---

## 性能规范

### 响应时间要求
- **IPC调用响应时间**: < 100ms
- **设备连接建立**: < 5秒
- **单点数据读取**: < 50ms
- **批量数据读取**: < 200ms（≤100个数据点）

### 并发限制
- **最大并发连接数**: 10个设备连接
- **单次批量操作限制**: ≤100个数据点
- **消息大小限制**: ≤1MB

---

## 向后兼容性

### 支持的旧版本命令

以下旧版本命令仍然支持，但建议使用新的标准化命令：

| 旧命令 | 新命令 | 描述 |
|--------|--------|------|
| `connect_device` | `connect` | 建立设备连接 |
| `disconnect_device` | `disconnect` | 断开设备连接 |
| `device_status` | `status` | 查询连接状态 |
| `read_data` | `read` | 读取数据 |
| `write_data` | `write` | 写入数据 |

---

## 数据类型支持

### 支持的数据类型

| 类型 | 描述 | 示例值 |
|------|------|--------|
| `bool`, `boolean` | 布尔值 | `true`, `false` |
| `int16` | 16位有符号整数 | `-32768` 到 `32767` |
| `uint16` | 16位无符号整数 | `0` 到 `65535` |
| `int32` | 32位有符号整数 | `-2147483648` 到 `2147483647` |
| `uint32` | 32位无符号整数 | `0` 到 `4294967295` |
| `int64` | 64位有符号整数 | `-9223372036854775808` 到 `9223372036854775807` |
| `uint64` | 64位无符号整数 | `0` 到 `18446744073709551615` |
| `float` | 32位浮点数 | `3.14159` |
| `double` | 64位浮点数 | `3.141592653589793` |
| `string` | 字符串 | `"Hello World"` |

### 数据类型转换

系统会自动尝试进行数据类型转换，支持以下转换：
- 数字类型之间的转换
- 字符串到数字的转换
- 布尔值到数字的转换（true=1, false=0）
- 数字到字符串的转换

---

*API参考文档版本: 1.0，最后更新: 2025-08-26*