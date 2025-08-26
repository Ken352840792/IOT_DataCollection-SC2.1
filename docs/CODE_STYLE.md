# 代码风格指南

本文档规定了SS_SC_IOT项目的代码风格和编码规范。

## 1. 总体原则

### 1.1 一致性

- 保持代码风格在整个项目中的一致性
- 遵循现有代码的约定和模式
- 使用自动化工具确保一致性

### 1.2 可读性

- 代码应该是自文档化的
- 使用有意义的变量和函数名
- 添加必要的注释说明复杂逻辑

### 1.3 简洁性

- 避免过度复杂的实现
- 遵循DRY（Don't Repeat Yourself）原则
- 保持函数和类的单一职责

## 2. JavaScript/Node.js代码规范

### 2.1 基础格式化

项目使用Prettier进行代码格式化，配置如下：

```json
{
  "semi": true,
  "singleQuote": true,
  "quoteProps": "as-needed",
  "trailingComma": "es5",
  "tabWidth": 2,
  "useTabs": false,
  "printWidth": 100,
  "bracketSpacing": true,
  "arrowParens": "avoid"
}
```

### 2.2 命名规范

#### 变量和函数

```javascript
// 使用camelCase
const userName = 'admin';
const deviceCount = 10;

// 函数名应该是动词或动词短语
function fetchDeviceData() {}
function validateConfiguration() {}
function isConnected() {}
```

#### 常量

```javascript
// 使用UPPER_SNAKE_CASE
const MAX_RETRY_COUNT = 3;
const DEFAULT_TIMEOUT = 5000;
const API_ENDPOINTS = {
  DEVICES: '/api/devices',
  PROTOCOLS: '/api/protocols',
};
```

#### 类和构造函数

```javascript
// 使用PascalCase
class DeviceManager {}
class ModbusClient {}
function ConnectionPool() {}
```

### 2.3 代码组织

#### 文件结构

```javascript
// 1. Node.js内置模块
const fs = require('fs');
const path = require('path');

// 2. 第三方依赖
const express = require('express');
const lodash = require('lodash');

// 3. 本地模块
const deviceManager = require('./device-manager');
const config = require('../config');

// 4. 常量定义
const PORT = 3000;
const TIMEOUT = 5000;

// 5. 主要逻辑
class Application {
  // 实现...
}

// 6. 导出
module.exports = Application;
```

#### 函数编写

```javascript
// 好的示例：函数职责单一，命名清晰
async function connectToDevice(deviceConfig) {
  validateDeviceConfig(deviceConfig);

  const client = createClient(deviceConfig);
  await client.connect();

  return client;
}

// 避免：函数过长，职责不清
function processDeviceData(device) {
  // 避免一个函数做太多事情
}
```

### 2.4 异步编程

#### 使用async/await

```javascript
// 推荐：使用async/await
async function fetchDeviceStatus(deviceId) {
  try {
    const device = await deviceService.getDevice(deviceId);
    const status = await device.getStatus();
    return status;
  } catch (error) {
    logger.error('Failed to fetch device status:', error);
    throw error;
  }
}

// 避免：回调地狱
function fetchDeviceStatus(deviceId, callback) {
  deviceService.getDevice(deviceId, (err, device) => {
    if (err) return callback(err);
    // 嵌套回调...
  });
}
```

#### 错误处理

```javascript
// 明确的错误处理
async function processData(data) {
  if (!data || !data.deviceId) {
    throw new ValidationError('Device ID is required');
  }

  try {
    const result = await externalService.process(data);
    return result;
  } catch (error) {
    // 记录错误但重新抛出
    logger.error('Data processing failed:', error);
    throw new ProcessingError('Failed to process data', { cause: error });
  }
}
```

### 2.5 注释规范

#### JSDoc注释

```javascript
/**
 * 连接到指定的设备并返回客户端实例
 * @param {Object} deviceConfig - 设备配置对象
 * @param {string} deviceConfig.host - 设备IP地址
 * @param {number} deviceConfig.port - 设备端口号
 * @param {string} deviceConfig.protocol - 通信协议类型
 * @returns {Promise<DeviceClient>} 设备客户端实例
 * @throws {ValidationError} 当配置参数无效时
 * @throws {ConnectionError} 当连接失败时
 */
async function connectToDevice(deviceConfig) {
  // 实现...
}
```

#### 内联注释

```javascript
// 解释复杂的业务逻辑
const retryDelay = Math.min(1000 * Math.pow(2, attempt), 30000); // 指数退避，最大30秒

// 解释魔法数字
if (response.statusCode === 502) {
  // Bad Gateway - 服务暂时不可用
  // 处理逻辑...
}
```

## 3. C#代码规范

### 3.1 命名规范

#### 类型和成员

```csharp
// 类、接口、枚举使用PascalCase
public class DeviceManager { }
public interface IProtocolHandler { }
public enum ConnectionStatus { }

// 方法使用PascalCase
public async Task<bool> ConnectAsync() { }
public void ValidateConfiguration() { }

// 属性使用PascalCase
public string DeviceId { get; set; }
public int Port { get; private set; }

// 字段使用camelCase，私有字段以_开头
private readonly string _connectionString;
private static readonly int _maxRetryCount = 3;

// 常量使用PascalCase
public const int DefaultTimeout = 5000;
private const string ConfigFileName = "device.json";
```

#### 参数和局部变量

```csharp
public async Task ProcessDeviceData(string deviceId, byte[] rawData)
{
    var processedData = await ProcessAsync(rawData);
    var validationResult = ValidateData(processedData);

    if (!validationResult.IsValid)
    {
        throw new ValidationException(validationResult.Errors);
    }
}
```

### 3.2 代码组织

#### 文件结构

```csharp
// 1. using指令（系统命名空间在前）
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

// 2. 第三方库
using HslCommunication.ModBus;
using Newtonsoft.Json;

// 3. 本地命名空间
using HlsService.Models;
using HlsService.Services;

namespace HlsService
{
    /// <summary>
    /// 设备通信管理器
    /// </summary>
    public class DeviceManager
    {
        // 1. 常量
        private const int DefaultTimeout = 5000;

        // 2. 字段
        private readonly ILogger _logger;
        private readonly Dictionary<string, IDeviceClient> _clients;

        // 3. 构造函数
        public DeviceManager(ILogger logger)
        {
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
            _clients = new Dictionary<string, IDeviceClient>();
        }

        // 4. 属性
        public int ActiveConnections => _clients.Count;

        // 5. 公共方法
        public async Task<bool> ConnectAsync(DeviceConfig config)
        {
            // 实现...
        }

        // 6. 私有方法
        private void ValidateConfig(DeviceConfig config)
        {
            // 实现...
        }
    }
}
```

### 3.3 异步编程

```csharp
// 使用ConfigureAwait(false)避免死锁
public async Task<string> ReadDataAsync(string address)
{
    var result = await _client.ReadStringAsync(address).ConfigureAwait(false);
    return result.IsSuccess ? result.Content : null;
}

// 异步方法命名以Async结尾
public async Task<bool> ValidateConnectionAsync()
{
    return await _client.ConnectAsync().ConfigureAwait(false);
}
```

## 4. JSON和配置文件

### 4.1 JSON格式

```json
{
  "deviceConfigs": [
    {
      "deviceId": "plc001",
      "name": "生产线PLC #1",
      "protocol": "modbus_tcp",
      "connection": {
        "host": "192.168.1.100",
        "port": 502,
        "timeout": 5000
      },
      "dataPoints": [
        {
          "name": "temperature",
          "address": "40001",
          "dataType": "float",
          "unit": "°C"
        }
      ]
    }
  ]
}
```

### 4.2 配置文件命名

- `*.config.js` - JavaScript配置文件
- `*.config.json` - JSON配置文件
- `appsettings.json` - .NET应用配置
- `.env` - 环境变量配置

## 5. 文档和注释

### 5.1 README文件

每个模块都应该有README.md文件，包含：

- 模块用途和功能
- 安装和配置说明
- 使用示例
- API文档链接

### 5.2 API文档

对于公开的API，使用以下格式：

#### JavaScript

```javascript
/**
 * @api {post} /api/devices/:id/connect 连接设备
 * @apiName ConnectDevice
 * @apiGroup Device
 * @apiVersion 1.0.0
 *
 * @apiParam {String} id 设备ID
 * @apiBody {Object} config 连接配置
 * @apiBody {String} config.protocol 通信协议
 * @apiBody {Object} config.connection 连接参数
 *
 * @apiSuccess {Boolean} success 操作是否成功
 * @apiSuccess {String} message 响应消息
 * @apiSuccess {Object} data 响应数据
 *
 * @apiError {String} error 错误信息
 */
```

#### C#

```csharp
/// <summary>
/// 连接到指定设备
/// </summary>
/// <param name="deviceId">设备唯一标识符</param>
/// <param name="config">设备连接配置</param>
/// <returns>连接是否成功</returns>
/// <exception cref="ArgumentNullException">当deviceId或config为null时抛出</exception>
/// <exception cref="InvalidOperationException">当设备已连接时抛出</exception>
/// <exception cref="ConnectionException">当连接失败时抛出</exception>
```

## 6. 测试代码规范

### 6.1 测试文件命名

- 单元测试：`*.test.js` 或 `*.spec.js`
- 集成测试：`*.integration.test.js`
- E2E测试：`*.e2e.test.js`

### 6.2 测试结构

```javascript
describe('DeviceManager', () => {
  describe('connectToDevice', () => {
    it('should connect successfully with valid config', async () => {
      // Arrange
      const config = createValidConfig();
      const manager = new DeviceManager();

      // Act
      const result = await manager.connectToDevice(config);

      // Assert
      expect(result.isConnected).toBe(true);
    });

    it('should throw ValidationError with invalid config', async () => {
      // Arrange
      const invalidConfig = {};
      const manager = new DeviceManager();

      // Act & Assert
      await expect(manager.connectToDevice(invalidConfig)).rejects.toThrow(ValidationError);
    });
  });
});
```

## 7. Git提交规范

### 7.1 提交消息格式

```
type(scope): subject

body

footer
```

### 7.2 类型说明

- `feat`: 新功能
- `fix`: 修复bug
- `docs`: 文档更新
- `style`: 格式化、缺少分号等
- `refactor`: 重构
- `perf`: 性能改进
- `test`: 测试相关
- `build`: 构建系统
- `ci`: CI配置
- `chore`: 其他更改

### 7.3 示例

```
feat(hls-service): add Modbus TCP connection support

- Implement ModbusTcpClient class
- Add connection retry mechanism
- Add unit tests for connection logic

Closes #123
```

## 8. 代码审查检查清单

### 8.1 功能性

- [ ] 代码实现了所需的功能
- [ ] 错误处理得当
- [ ] 边界条件处理正确
- [ ] 性能考虑合理

### 8.2 代码质量

- [ ] 代码结构清晰
- [ ] 命名规范一致
- [ ] 注释充分且准确
- [ ] 无重复代码

### 8.3 测试覆盖

- [ ] 有相应的单元测试
- [ ] 测试覆盖主要执行路径
- [ ] 测试用例有意义

### 8.4 安全性

- [ ] 输入验证充分
- [ ] 无硬编码敏感信息
- [ ] 权限检查正确

---

_最后更新：2025-08-26_  
_维护者：开发团队_
