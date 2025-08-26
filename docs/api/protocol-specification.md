# HLS通信协议规范

## 版本: 1.0
## 最后更新: 2025-08-26

---

## 概述

本文档定义了HLS-Communication服务与Node-RED定制节点之间的标准化JSON通信协议。该协议基于IPC（进程间通信）机制，提供设备连接管理、数据读写操作和错误处理等功能。

## 协议特性

- **轻量级**: 基于JSON格式，便于解析和调试
- **版本控制**: 支持协议版本管理和向后兼容性
- **消息跟踪**: 提供唯一消息ID机制确保请求响应匹配
- **错误处理**: 标准化错误码和详细错误信息
- **批量操作**: 支持高效的批量数据操作
- **性能优化**: IPC调用响应时间 < 100ms

## 消息格式规范

### 通用请求格式

```json
{
  "version": "1.0",
  "messageId": "uuid或自定义格式",
  "timestamp": "2025-08-26T10:00:00Z",
  "command": "命令类型",
  "data": {
    // 具体命令的数据内容
  }
}
```

#### 请求字段说明

| 字段 | 类型 | 必需 | 描述 |
|------|------|------|------|
| `version` | String | 是 | 协议版本号，当前为 "1.0" |
| `messageId` | String | 是 | 消息唯一标识符，支持UUID或8-64字符的自定义格式 |
| `timestamp` | String | 是 | ISO8601格式的UTC时间戳 |
| `command` | String | 是 | 命令类型，参见支持的命令列表 |
| `data` | Object | 否 | 命令相关的数据负载 |

### 通用响应格式

```json
{
  "version": "1.0",
  "messageId": "对应请求的messageId",
  "timestamp": "2025-08-26T10:00:01Z",
  "success": true,
  "processingTimeMs": 45.2,
  "data": {
    // 响应数据内容
  },
  "error": {
    "code": "错误代码",
    "message": "错误描述",
    "details": ["详细错误信息"],
    "type": "错误类型",
    "retryable": false,
    "retryDelayMs": 1000,
    "resourceId": "相关资源ID"
  }
}
```

#### 响应字段说明

| 字段 | 类型 | 必需 | 描述 |
|------|------|------|------|
| `version` | String | 是 | 协议版本号 |
| `messageId` | String | 是 | 与请求消息ID匹配 |
| `timestamp` | String | 是 | 响应时间戳 |
| `success` | Boolean | 是 | 操作是否成功 |
| `processingTimeMs` | Number | 是 | 处理耗时（毫秒） |
| `data` | Object | 否 | 成功时的响应数据 |
| `error` | Object | 否 | 失败时的错误详情 |

## 支持的命令

### 连接管理命令

#### 1. connect - 建立设备连接

**请求示例:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100001_001",
  "timestamp": "2025-08-26T10:00:01Z",
  "command": "connect",
  "data": {
    "deviceConfig": {
      "type": "modbus-tcp",
      "host": "192.168.1.100",
      "port": 502,
      "timeout": 5000
    },
    "dataPoints": [
      {
        "name": "temperature",
        "address": "40001",
        "dataType": "float",
        "access": "read"
      }
    ]
  }
}
```

**响应示例:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100001_001",
  "timestamp": "2025-08-26T10:00:01.500Z",
  "success": true,
  "processingTimeMs": 456.2,
  "data": {
    "connectionId": "conn_001",
    "status": "connected",
    "deviceInfo": {
      "type": "modbus-tcp",
      "host": "192.168.1.100",
      "port": 502
    }
  }
}
```

#### 2. disconnect - 断开设备连接

**请求示例:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100002_002",
  "timestamp": "2025-08-26T10:00:02Z",
  "command": "disconnect",
  "data": {
    "connectionId": "conn_001"
  }
}
```

#### 3. status - 查询连接状态

**请求示例:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100003_003",
  "timestamp": "2025-08-26T10:00:03Z",
  "command": "status",
  "data": {
    "connectionId": "conn_001"
  }
}
```

### 数据操作命令

#### 4. read - 读取数据

**单点读取:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100004_004",
  "timestamp": "2025-08-26T10:00:04Z",
  "command": "read",
  "data": {
    "connectionId": "conn_001",
    "dataPoints": ["40001"]
  }
}
```

**响应示例:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100004_004",
  "timestamp": "2025-08-26T10:00:04.100Z",
  "success": true,
  "processingTimeMs": 85.3,
  "data": {
    "results": [
      {
        "address": "40001",
        "value": 25.6,
        "dataType": "float",
        "timestamp": "2025-08-26T10:00:04.095Z",
        "quality": "good"
      }
    ]
  }
}
```

#### 5. readBatch - 批量读取数据

**请求示例:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100005_005",
  "timestamp": "2025-08-26T10:00:05Z",
  "command": "readBatch",
  "data": {
    "connectionId": "conn_001",
    "dataPoints": ["40001", "40002", "40003"],
    "options": {
      "timeout": 5000,
      "retryCount": 3
    }
  }
}
```

#### 6. write - 写入数据

**单点写入:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100006_006",
  "timestamp": "2025-08-26T10:00:06Z",
  "command": "write",
  "data": {
    "connectionId": "conn_001",
    "dataPoints": [
      {
        "address": "40001",
        "value": 30.5,
        "dataType": "float"
      }
    ]
  }
}
```

#### 7. writeBatch - 批量写入数据

**请求示例:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100007_007",
  "timestamp": "2025-08-26T10:00:07Z",
  "command": "writeBatch",
  "data": {
    "connectionId": "conn_001",
    "dataPoints": [
      {"address": "40001", "value": 30.5, "dataType": "float"},
      {"address": "40002", "value": 100, "dataType": "int16"}
    ],
    "options": {
      "timeout": 5000,
      "validateBeforeWrite": true
    }
  }
}
```

### 服务器管理命令

#### 8. serverStatus - 获取服务器状态

**请求示例:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100008_008",
  "timestamp": "2025-08-26T10:00:08Z",
  "command": "serverStatus"
}
```

**响应示例:**
```json
{
  "version": "1.0",
  "messageId": "msg_20250826100008_008",
  "timestamp": "2025-08-26T10:00:08.050Z",
  "success": true,
  "processingTimeMs": 12.5,
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

## 错误处理规范

### 错误码分类

错误码采用数字格式，按类别组织：

#### 系统级错误 (1xxx)
- **1000**: 系统内部错误
- **1001**: 无效的请求
- **1002**: 参数无效  
- **1003**: 权限拒绝
- **1004**: 缺少必需参数
- **1005**: 格式无效
- **1006**: 数值超出有效范围

#### 设备连接错误 (2xxx)
- **2001**: 设备未找到
- **2002**: 设备离线
- **2003**: 连接超时
- **2004**: 连接失败
- **2005**: 设备已存在
- **2006**: 设备未连接

#### 数据操作错误 (3xxx)
- **3001**: 地址无效
- **3002**: 数据类型错误
- **3003**: 读取超时
- **3004**: 写入失败
- **3005**: 读取权限拒绝
- **3006**: 写入权限拒绝

#### IPC通信错误 (5xxx)
- **5001**: 消息格式无效
- **5002**: 消息过大
- **5003**: 命令未找到
- **5004**: 不支持的协议版本

### 错误响应示例

```json
{
  "version": "1.0",
  "messageId": "msg_20250826100009_009",
  "timestamp": "2025-08-26T10:00:09.200Z",
  "success": false,
  "processingTimeMs": 15.8,
  "error": {
    "code": "2004",
    "message": "连接失败",
    "details": [
      "Device ID: modbus_device_001",
      "Connection refused by remote host"
    ],
    "type": "Network",
    "retryable": true,
    "retryDelayMs": 3000,
    "resourceId": "modbus_device_001",
    "timestamp": "2025-08-26T10:00:09.200Z"
  }
}
```

## 协议版本管理

### 当前版本: 1.0

**支持功能:**
- 基础设备连接管理
- 数据读写操作
- 批量操作支持
- 错误处理和状态码
- 消息验证
- 连接状态监控

### 版本兼容性

- **向后兼容**: 支持旧版本客户端
- **版本检测**: 自动检测并适配客户端版本
- **优雅降级**: 不支持的功能返回适当错误码

## 性能要求

### 响应时间要求
- **IPC调用响应时间**: < 100ms
- **设备连接建立**: < 5秒
- **单点数据读取**: < 50ms
- **批量数据读取**: < 200ms（≤100个数据点）

### 并发限制
- **最大并发连接数**: 10个设备连接
- **单次批量操作限制**: ≤100个数据点
- **消息大小限制**: ≤1MB

## 消息验证规则

### 必填字段验证
- `version`: 必须是支持的协议版本
- `messageId`: 8-64字符，支持字母数字及 `-` `_`
- `timestamp`: 必须是有效的ISO8601格式
- `command`: 必须是支持的命令类型

### 业务逻辑验证
- 时间戳不能超过5分钟前或未来时间
- connectionId必须是已建立的连接
- 数据点地址必须符合设备协议规范
- 批量操作不能超过限制数量

## 使用示例

### Node.js客户端示例

```javascript
const net = require('net');

class HLSIPCClient {
  constructor(options = {}) {
    this.host = options.host || 'localhost';
    this.port = options.port || 8888;
    this.timeout = options.timeout || 5000;
    this.socket = null;
  }

  async connect() {
    return new Promise((resolve, reject) => {
      this.socket = net.createConnection(this.port, this.host);
      this.socket.setTimeout(this.timeout);
      
      this.socket.on('connect', () => resolve());
      this.socket.on('error', (err) => reject(err));
    });
  }

  async sendCommand(command, data = {}) {
    const request = {
      version: '1.0',
      messageId: this.generateMessageId(),
      timestamp: new Date().toISOString(),
      command,
      data
    };

    return this.sendMessage(request);
  }

  async connectDevice(deviceConfig, dataPoints) {
    return this.sendCommand('connect', {
      deviceConfig,
      dataPoints
    });
  }

  async readData(connectionId, addresses) {
    return this.sendCommand('read', {
      connectionId,
      dataPoints: addresses
    });
  }

  generateMessageId() {
    const timestamp = Date.now();
    const random = Math.floor(Math.random() * 1000);
    return `msg_${timestamp}_${random}`;
  }
}
```

## 故障排除

### 常见问题

1. **连接失败 (2004)**
   - 检查设备IP地址和端口
   - 确认网络连通性
   - 验证设备协议配置

2. **读取超时 (3003)**
   - 检查设备响应性能
   - 调整超时参数
   - 确认数据点地址正确

3. **消息格式无效 (5001)**
   - 验证JSON格式正确性
   - 检查必填字段完整性
   - 确认字段类型匹配

### 调试建议

1. 启用详细日志记录
2. 使用消息ID跟踪请求响应
3. 监控响应时间和错误率
4. 定期检查连接状态

## 安全考虑

1. **消息验证**: 所有输入消息都经过格式和业务逻辑验证
2. **连接管理**: 限制并发连接数防止资源耗尽  
3. **错误信息**: 避免泄露敏感系统信息
4. **超时机制**: 防止长时间阻塞操作

---

*本协议规范版本: 1.0，最后更新: 2025-08-26*