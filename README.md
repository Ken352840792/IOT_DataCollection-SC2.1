# 滠水采集服务 (SS_SC_IOT)

数采盒子硬件及其配套软件系统，解决制造企业中设备数据协议多样化的问题，实现数据的统一采集、处理与传输。

## 项目概述

本项目开发一个"数采盒子"及其内部软件，通过集成hlscommunication框架和Node-RED平台，为工业设备提供：

- **设备数据采集** - 支持多种工业协议的数据采集
- **数据预处理与编排** - 使用Node-RED进行数据流编排和预处理  
- **设备反向控制** - 通过Node-RED实现设备的远程控制
- **跨语言通信** - Node.js与C#/Java的进程间通信

## 技术架构

### 双进程架构
- **HLS-Communication核心服务** - 独立后台进程（C#/.NET Core）
  - 处理底层设备协议通信
  - 动态设备配置管理
  - 数据点批量读写操作

- **Node-RED环境与定制节点** - 数据流编排平台
  - hls-read节点：数据采集
  - hls-write节点：设备控制
  - 通过IPC与核心服务通信

### 通信机制
- **IPC通信：** TCP Socket本地通信
- **数据格式：** JSON消息协议
- **连接管理：** 多客户端并发支持

## 快速开始

### 环境要求
- Node.js 18.x LTS+
- .NET Core 6.0+
- Git

### 安装步骤

1. **克隆项目**
   ```bash
   git clone <repository-url>
   cd SC2.1
   ```

2. **安装Node.js依赖**
   ```bash
   npm install
   ```

3. **配置环境**
   ```bash
   cp config/.env.example .env
   # 编辑.env文件，设置必要的配置
   ```

4. **启动Node-RED**
   ```bash
   npm run start:nodered
   ```

5. **启动HLS服务**
   ```bash
   npm run start:hls-service
   ```

### 开发模式

```bash
# 开发模式启动
npm run dev

# 运行测试
npm test

# 代码格式化
npm run format

# 代码质量检查
npm run lint
```

## 项目结构

```
SC2.1/
├── src/                    # 源代码
│   ├── hls-service/       # HLS-Communication服务
│   └── nodes/             # Node-RED定制节点
├── tests/                 # 测试文件
│   ├── unit/              # 单元测试
│   └── integration/       # 集成测试
├── config/                # 配置文件
├── docs/                  # 项目文档
├── logs/                  # 日志文件
└── package.json           # 项目配置
```

## 支持的设备协议

- **Modbus TCP/RTU** - 工业标准协议
- **Siemens S7** - 西门子PLC协议
- **Omron FINS** - 欧姆龙PLC协议  
- **Mitsubishi MC** - 三菱PLC协议

## 开发指南

详细的开发指南请参考：
- [贡献指南](CONTRIBUTING.md)
- [API文档](docs/api/)
- [架构文档](docs/architecture.md)

## 许可证

[MIT License](LICENSE)

## 联系方式

- 项目负责人：Sarah (PO)
- 技术支持：请创建Issue

---
*最后更新：2025-08-25*