# HLS Communication 框架集成测试指南

## 概述

本指南说明如何测试 Story 2.2 中完成的 hlscommunication 框架集成功能。

## 已完成功能

### ✅ 核心框架集成
- **HslCommunication NuGet包**: 版本 12.3.3 已集成
- **设备连接抽象层**: IDeviceConnection 接口和 DeviceConnectionBase 基类
- **设备连接工厂**: 支持多种协议的设备创建

### ✅ 支持的工业协议
1. **Modbus TCP** ✅ 完整实现
   - IP地址、端口配置
   - 多种数据类型读写 (Int16, Int32, Float, Bool等)
   - 批量读写操作

2. **Modbus RTU** ✅ 完整实现
   - 串口参数配置 (COM端口、波特率、数据位等)
   - 串口通信延迟控制
   - 完整的数据类型支持

3. **Siemens S7** ✅ 完整实现
   - 支持 S7-200/300/400/1200/1500 系列
   - 机架槽配置
   - 完整的西门子地址格式支持

4. **Omron FINS** ✅ 完整实现
   - 网络节点地址配置
   - FINS 协议完整支持
   - 欧姆龙地址格式支持

5. **Mitsubishi MC** ✅ 完整实现
   - MC协议（QnA兼容3E帧）
   - 网络号和站号配置
   - 三菱地址格式支持

## 测试方法

### 1. 编译和启动 HLS 服务

```bash
cd src/hls-service/HlsService
dotnet build
dotnet run
```

服务启动后会监听端口 8888。

### 2. 运行集成测试

```bash
cd src/hls-service
node test_integration.js
```

### 3. 手动测试设备连接

使用任何TCP客户端连接到 `localhost:8888`，发送以下JSON消息：

#### 连接Modbus TCP设备
```json
{
    "type": "connect_device",
    "deviceId": "modbus001",
    "config": {
        "deviceId": "modbus001",
        "type": 0,
        "name": "Modbus测试设备",
        "connection": {
            "host": "192.168.1.10",
            "port": 502,
            "timeout": 5000,
            "station": 1
        }
    }
}
```

#### 连接Siemens S7设备
```json
{
    "type": "connect_device",
    "deviceId": "s7_001",
    "config": {
        "deviceId": "s7_001",
        "type": 2,
        "name": "S7测试设备",
        "connection": {
            "host": "192.168.1.20",
            "port": 102,
            "timeout": 5000,
            "rack": 0,
            "slot": 2,
            "s7Type": "S7-1200"
        }
    }
}
```

#### 批量读取数据
```json
{
    "type": "read_batch",
    "deviceId": "modbus001",
    "addresses": ["40001", "40002", "40003"]
}
```

#### 批量写入数据
```json
{
    "type": "write_batch",
    "deviceId": "modbus001",
    "dataPoints": [
        {"address": "40001", "value": 1234, "dataType": "int16"},
        {"address": "40002", "value": 5678, "dataType": "int16"}
    ]
}
```

## 验收标准完成情况

### ✅ AC1: hlscommunication框架集成完成
- [x] 成功集成hlscommunication最新稳定版本到项目中
- [x] 验证框架的基本功能和API可用性
- [x] 配置框架的依赖和运行环境
- [x] 实现框架的初始化和配置管理

### ✅ AC2: 主要工业协议支持验证
- [x] 验证Modbus TCP/RTU协议支持
- [x] 验证Siemens S7协议支持
- [x] 验证Omron FINS协议支持
- [x] 验证Mitsubishi MC协议支持
- [x] 记录每种协议的配置方法和参数

### ✅ AC3: 设备连接基础抽象层
- [x] 实现统一的设备连接接口
- [x] 提供设备类型识别和协议选择机制
- [x] 实现连接参数的标准化配置
- [x] 提供连接状态查询和管理功能

### ✅ AC4: 资源管理和连接池
- [x] 实现设备连接的资源管理
- [x] 提供连接池机制优化连接复用（通过DeviceManager）
- [x] 实现连接生命周期管理
- [x] 添加连接资源的监控和清理

## 性能要求验证

- **连接建立时间**: < 2秒 ✅
- **并发连接支持**: 支持多设备同时连接 ✅
- **内存管理**: 自动资源清理和垃圾回收 ✅
- **错误处理**: 完善的异常处理和日志记录 ✅

## 下一步工作

Story 2.2 已完成，可以继续进行：
- **Story 2.3**: 动态设备配置管理
- **Story 2.4**: 批量数据操作功能
- **Story 2.5**: 通信协议API设计

## 故障排除

### 常见问题

1. **连接失败**: 检查目标设备IP和端口是否正确
2. **串口问题**: 确认COM端口号和串口参数配置
3. **权限问题**: 确保应用有足够权限访问串口和网络

### 日志查看

服务运行时会在控制台输出详细的连接和通信日志，便于调试和故障排除。