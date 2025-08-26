# HLS Communication Protocol 故障排除指南

## 目录
- [常见错误代码](#常见错误代码)
- [连接问题](#连接问题)
- [数据操作问题](#数据操作问题)
- [性能问题](#性能问题)
- [协议相关问题](#协议相关问题)
- [日志分析](#日志分析)
- [工具和调试](#工具和调试)

## 常见错误代码

### 系统错误 (1xxx)
| 错误代码 | 描述 | 解决方案 |
|---------|------|---------|
| 1001 | 内部服务器错误 | 检查服务器日志，确保服务正常运行 |
| 1002 | 协议版本不匹配 | 检查客户端和服务器的协议版本 |
| 1003 | 无效的消息格式 | 验证JSON格式是否正确 |
| 1004 | 缺少必需参数 | 检查请求中是否包含所有必需字段 |
| 1005 | 参数验证失败 | 验证参数值是否符合规范 |

### 连接错误 (2xxx)
| 错误代码 | 描述 | 解决方案 |
|---------|------|---------|
| 2001 | 设备连接失败 | 检查网络连接和设备状态 |
| 2002 | 设备未找到 | 验证设备ID是否正确 |
| 2003 | 连接超时 | 增加超时时间或检查网络延迟 |
| 2004 | 设备已连接 | 断开现有连接再重新连接 |
| 2005 | 连接参数无效 | 检查IP地址、端口等参数 |

### 数据操作错误 (3xxx)
| 错误代码 | 描述 | 解决方案 |
|---------|------|---------|
| 3001 | 数据读取失败 | 检查地址是否正确，设备是否响应 |
| 3002 | 数据写入失败 | 验证写入权限和数据类型 |
| 3003 | 数据类型转换失败 | 确保数据类型匹配 |
| 3004 | 地址格式无效 | 检查地址格式是否符合协议规范 |
| 3005 | 数据超出范围 | 验证数据值是否在允许范围内 |

## 连接问题

### 问题：连接设备失败
**症状**：收到错误代码2001或2003

**排查步骤**：
1. **网络连通性**
   ```bash
   # Windows
   ping <设备IP>
   telnet <设备IP> <端口>
   
   # Linux
   ping <设备IP>
   nc -zv <设备IP> <端口>
   ```

2. **设备配置检查**
   ```json
   {
     "connectionId": "device_001",
     "type": "ModbusTcp",
     "connection": {
       "host": "192.168.1.100",
       "port": 502,
       "timeoutMs": 5000,
       "station": 1
     }
   }
   ```

3. **防火墙设置**
   - 确保端口未被防火墙阻止
   - 检查网络安全策略

### 问题：设备频繁断开连接
**可能原因**：
- 网络不稳定
- 设备超时设置过短
- 并发连接过多

**解决方案**：
- 增加连接超时时间
- 实现重连机制
- 限制并发连接数

## 数据操作问题

### 问题：数据读取失败
**症状**：收到错误代码3001

**排查步骤**：
1. **验证地址格式**
   ```javascript
   // Modbus TCP 地址示例
   const addresses = [
     "40001",    // 保持寄存器
     "30001",    // 输入寄存器
     "00001",    // 线圈
     "10001"     // 离散输入
   ];
   ```

2. **检查数据类型**
   ```javascript
   // 确保数据类型正确
   const readRequest = {
     connectionId: "device_001",
     dataPoints: ["40001"],
     dataType: "int16"  // 确保与设备匹配
   };
   ```

3. **设备响应检查**
   - 使用Modbus工具直接测试设备
   - 检查设备是否支持该地址范围

### 问题：数据写入失败
**症状**：收到错误代码3002

**常见原因**：
- 写入只读寄存器
- 数据类型不匹配
- 值超出范围

**解决方案**：
```javascript
// 正确的写入请求
const writeRequest = {
  connectionId: "device_001",
  dataPoints: [{
    address: "40001",
    value: 100,
    dataType: "int16"
  }]
};

// 检查数据范围
// int16: -32768 到 32767
// uint16: 0 到 65535
```

## 性能问题

### 问题：响应时间过长
**症状**：ResponseTimeMs > 1000ms

**优化建议**：
1. **批量操作**
   ```javascript
   // 使用批量读取减少请求次数
   const batchRead = {
     command: "readBatch",
     data: {
       connectionId: "device_001",
       dataPoints: ["40001", "40002", "40003", "40004"]
     }
   };
   ```

2. **连接复用**
   - 保持连接而不是频繁连接/断开
   - 使用连接池管理多个设备

3. **超时设置**
   ```javascript
   const connection = {
     host: "192.168.1.100",
     port: 502,
     timeoutMs: 3000,  // 根据网络情况调整
     station: 1
   };
   ```

### 问题：内存使用过高
**可能原因**：
- 连接未正确释放
- 大量并发请求
- 数据缓存过多

**解决方案**：
- 及时关闭不用的连接
- 实现连接超时机制
- 限制并发请求数量

## 协议相关问题

### 问题：协议版本不匹配
**症状**：收到错误代码1002

**解决方案**：
```javascript
// 检查协议版本
const request = {
  messageId: "msg_001",
  version: "1.0",  // 确保版本正确
  command: "connect",
  data: {...}
};
```

### 问题：JSON格式错误
**症状**：收到错误代码1003

**常见错误**：
```javascript
// 错误：缺少引号
{connectionId: device_001}

// 正确
{"connectionId": "device_001"}

// 错误：多余的逗号
{"connectionId": "device_001",}

// 正确
{"connectionId": "device_001"}
```

## 日志分析

### 启用详细日志
在配置文件中设置：
```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "HlsService": "Debug"
    }
  }
}
```

### 关键日志模式

**连接相关**：
```
[INFO] ConnectionController: Attempting to connect to device device_001
[ERROR] ConnectionController: Connection failed for device device_001: Connection timeout
```

**数据操作相关**：
```
[DEBUG] DataOperationController: Reading 4 data points from device device_001
[WARNING] DataOperationController: Failed to read address 40001: Invalid address
```

**性能相关**：
```
[INFO] MessageProcessor: Request msg_001 processed in 850ms
[WARNING] DeviceManager: Response time exceeded threshold: 1200ms
```

## 工具和调试

### 1. 协议测试工具
使用提供的测试脚本：
```bash
node test_protocol_v1.js
```

### 2. Modbus调试工具
推荐工具：
- Modbus Poll（Windows）
- QModMaster（跨平台）
- ModbusDoctor（Web）

### 3. 网络抓包
使用Wireshark分析Modbus TCP通信：
```
过滤器: tcp.port == 502
```

### 4. 性能监控
监控关键指标：
```javascript
// 响应时间监控
if (response.summary.totalProcessingTimeMs > 1000) {
  console.warn('Response time exceeded threshold');
}

// 成功率监控
const successRate = response.summary.successful / 
  (response.summary.successful + response.summary.failed);
if (successRate < 0.9) {
  console.warn('Success rate too low:', successRate);
}
```

## 常见问题FAQ

**Q: 为什么连接成功但读取数据失败？**
A: 检查地址格式和设备地址映射。不同设备的地址规则可能不同。

**Q: 如何处理网络不稳定的环境？**
A: 实现重试机制，增加超时时间，使用心跳检测。

**Q: 批量操作的最大数量是多少？**
A: 默认最大批量大小为100，可以在ProtocolConstants中调整。

**Q: 如何优化大量设备的连接管理？**
A: 使用连接池，实现负载均衡，定期清理无效连接。

**Q: 数据类型转换失败怎么办？**
A: 确保客户端发送的数据类型与设备期望的类型一致，使用正确的数据类型标识符。

---

如需更多帮助，请查看API参考文档或联系技术支持。