# 新开发者入门指南

欢迎加入SS_SC_IOT项目！本指南将帮助您快速了解项目并开始开发工作。

## 1. 项目概述

### 1.1 项目简介

SS_SC_IOT（滠水采集服务）是一个工业数据采集系统，提供：

- 多协议设备通信（Modbus、Siemens S7、Omron FINS等）
- 可视化数据流编排（基于Node-RED）
- 实时数据采集和处理
- 跨平台部署支持

### 1.2 技术栈

- **前端/编排**：Node-RED (Node.js)
- **通信服务**：.NET 9.0 + HslCommunication
- **数据库**：待定（SQLite/PostgreSQL）
- **容器化**：Docker（计划）
- **通信协议**：TCP Socket + JSON

### 1.3 架构概览

```
┌─────────────────┐    IPC Socket    ┌──────────────────────┐
│   Node-RED      │ ←──────────────→ │  HLS-Communication   │
│  (数据编排)      │    JSON 消息     │      服务            │
│  Port: 1880     │                  │    Port: 8888        │
└─────────────────┘                  └──────────────────────┘
        │                                       │
        ▼                                       ▼
┌─────────────────┐                  ┌──────────────────────┐
│    Web UI       │                  │    工业设备           │
│   (管理界面)     │                  │  (PLC/DCS/仪表)      │
└─────────────────┘                  └──────────────────────┘
```

## 2. 开发环境设置

### 2.1 前置条件

- **操作系统**：Windows 10/11, macOS 10.15+, Ubuntu 20.04+
- **Git**：版本控制
- **VS Code**：推荐IDE

### 2.2 必需软件

#### Node.js环境

```bash
# 安装Node.js 18+ LTS
# 推荐使用nvm进行版本管理
curl -o- https://raw.githubusercontent.com/nvm-sh/nvm/v0.39.4/install.sh | bash
nvm install 22
nvm use 22

# 验证安装
node --version  # 应该显示 v22.x.x
npm --version   # 应该显示 10.x.x
```

#### .NET环境

```bash
# 安装.NET 9.0 SDK
# Windows: 从官网下载安装包
# macOS: brew install --cask dotnet
# Ubuntu: 参考Microsoft官方文档

# 验证安装
dotnet --version  # 应该显示 9.0.x
```

### 2.3 项目克隆和设置

```bash
# 1. 克隆项目
git clone [项目地址]
cd ss-sc-iot

# 2. 安装Node.js依赖
npm install

# 3. 验证环境
npm run verify-env

# 4. 初始化Git钩子
npm run prepare
```

### 2.4 IDE配置

#### VS Code扩展（推荐安装）

```json
{
  "recommendations": [
    "ms-vscode.vscode-json",
    "esbenp.prettier-vscode",
    "dbaeumer.vscode-eslint",
    "ms-dotnettools.csharp",
    "ms-dotnettools.vscode-dotnet-runtime",
    "bradlc.vscode-tailwindcss",
    "humao.rest-client",
    "ms-vscode.vscode-typescript-next"
  ]
}
```

#### VS Code工作区设置

```json
{
  "editor.formatOnSave": true,
  "editor.codeActionsOnSave": {
    "source.fixAll.eslint": true
  },
  "eslint.workingDirectories": ["./"],
  "prettier.configPath": "./.prettierrc"
}
```

## 3. 项目结构理解

### 3.1 目录结构

```
ss-sc-iot/
├── .github/                 # GitHub模板和CI/CD
│   ├── workflows/           # GitHub Actions
│   └── ISSUE_TEMPLATE/      # Issue模板
├── .husky/                  # Git钩子
├── config/                  # 配置文件
├── docs/                    # 项目文档
│   ├── stories/             # 用户故事
│   ├── architecture/        # 架构文档
│   └── prd/                 # 产品需求文档
├── logs/                    # 日志文件
├── nodered-data/            # Node-RED数据和配置
├── scripts/                 # 构建和工具脚本
├── src/                     # 源代码
│   ├── hls-service/         # HLS-Communication服务(.NET)
│   └── nodes/               # Node-RED自定义节点
├── tests/                   # 测试文件
│   ├── unit/                # 单元测试
│   ├── integration/         # 集成测试
│   └── e2e/                 # 端到端测试
└── web-bundles/             # Web资源包
```

### 3.2 关键文件说明

- `package.json` - Node.js项目配置和脚本
- `.eslintrc.js` - 代码质量规则
- `.prettierrc` - 代码格式化配置
- `docs/TECHNICAL_DECISIONS.md` - 技术决策记录
- `docs/CODE_STYLE.md` - 代码风格指南

## 4. 开发工作流

### 4.1 分支策略

```
main          # 生产分支，受保护
  └── develop # 开发集成分支
      ├── feature/xxx-new-feature  # 功能分支
      ├── bugfix/xxx-fix-issue     # 修复分支
      └── hotfix/xxx-urgent-fix    # 紧急修复
```

### 4.2 典型开发流程

```bash
# 1. 创建功能分支
git checkout develop
git pull origin develop
git checkout -b feature/add-siemens-s7-support

# 2. 进行开发
# 编写代码...
# 运行测试：npm test
# 检查代码质量：npm run lint

# 3. 提交代码
git add .
git commit -m "feat(protocols): add Siemens S7 protocol support

- Implement S7Client wrapper class
- Add S7 connection configuration
- Add unit tests for S7 operations

Closes #45"

# 4. 推送并创建PR
git push origin feature/add-siemens-s7-support
# 在GitHub上创建Pull Request
```

### 4.3 提交消息规范

使用[Conventional Commits](https://www.conventionalcommits.org/)格式：

```
<type>(<scope>): <description>

[optional body]

[optional footer(s)]
```

**类型（type）**：

- `feat` - 新功能
- `fix` - 修复bug
- `docs` - 文档更新
- `style` - 代码格式化
- `refactor` - 重构
- `test` - 测试相关
- `chore` - 维护任务

**范围（scope）**：

- `core` - 核心功能
- `hls-service` - HLS通信服务
- `node-red` - Node-RED相关
- `protocols` - 通信协议
- `config` - 配置相关

## 5. 本地开发和测试

### 5.1 启动开发环境

```bash
# 终端1: 启动HLS-Communication服务
npm run start:hls-service

# 终端2: 启动Node-RED
npm run start:nodered-dev

# 终端3: 运行测试（可选）
npm run test:watch
```

### 5.2 访问地址

- **Node-RED编辑器**：http://localhost:1880
- **Node-RED管理界面**：http://localhost:1880/admin
- **HLS服务**：127.0.0.1:8888 (TCP Socket)

### 5.3 常用开发命令

```bash
# 代码检查和格式化
npm run lint          # 检查代码质量
npm run lint:fix      # 自动修复可修复的问题
npm run format        # 格式化代码
npm run format:check  # 检查代码格式

# 测试命令
npm test              # 运行所有测试
npm run test:unit     # 运行单元测试
npm run test:integration  # 运行集成测试
npm run test:coverage # 生成覆盖率报告

# IPC通信测试
node tests/ipc-test-client.js  # 测试Node.js与.NET的通信
```

## 6. 调试指南

### 6.1 Node.js调试

在VS Code中配置调试：

```json
{
  "type": "node",
  "request": "launch",
  "name": "Debug Node-RED",
  "program": "${workspaceFolder}/node_modules/.bin/node-red",
  "args": ["-u", "./nodered-data", "--verbose"],
  "console": "integratedTerminal"
}
```

### 6.2 .NET调试

```json
{
  "type": "dotnet",
  "request": "launch",
  "name": "Debug HLS Service",
  "program": "${workspaceFolder}/src/hls-service/HlsService/bin/Debug/net9.0/HlsService.dll",
  "cwd": "${workspaceFolder}/src/hls-service/HlsService",
  "console": "integratedTerminal"
}
```

### 6.3 常见问题排查

#### HLS服务连接失败

```bash
# 检查端口是否被占用
netstat -an | grep 8888

# 检查防火墙设置
# Windows: 确保8888端口允许本地连接
# Linux: sudo ufw allow 8888
```

#### Node-RED无法访问

```bash
# 检查Node-RED日志
tail -f logs/node-red.log

# 重置Node-RED数据
rm -rf nodered-data/flows.json
npm run start:nodered
```

## 7. 测试指南

### 7.1 测试策略

- **单元测试**：测试独立的函数和类
- **集成测试**：测试组件间的交互
- **端到端测试**：测试完整的用户场景

### 7.2 编写测试

#### JavaScript单元测试示例

```javascript
// tests/unit/device-manager.test.js
const DeviceManager = require('../../src/core/device-manager');

describe('DeviceManager', () => {
  let manager;

  beforeEach(() => {
    manager = new DeviceManager();
  });

  describe('addDevice', () => {
    it('should add a valid device configuration', () => {
      const config = {
        deviceId: 'test001',
        protocol: 'modbus_tcp',
        host: '192.168.1.100',
      };

      const result = manager.addDevice(config);

      expect(result.success).toBe(true);
      expect(manager.getDeviceCount()).toBe(1);
    });
  });
});
```

#### C#单元测试示例

```csharp
// 如果创建了.NET测试项目
[TestClass]
public class DeviceManagerTests
{
    [TestMethod]
    public async Task ConnectAsync_ValidConfig_ReturnsTrue()
    {
        // Arrange
        var config = new DeviceConfig { Host = "127.0.0.1", Port = 502 };
        var manager = new DeviceManager();

        // Act
        var result = await manager.ConnectAsync(config);

        // Assert
        Assert.IsTrue(result);
    }
}
```

## 8. 常用资源和链接

### 8.1 技术文档

- [Node-RED官方文档](https://nodered.org/docs/)
- [HslCommunication文档](https://github.com/dathlin/HslCommunication)
- [.NET 9.0文档](https://docs.microsoft.com/dotnet/)

### 8.2 项目文档

- [架构设计文档](./docs/architecture/)
- [产品需求文档](./docs/prd/)
- [用户故事列表](./docs/stories/)
- [技术决策记录](./docs/TECHNICAL_DECISIONS.md)

### 8.3 开发工具

- [VS Code](https://code.visualstudio.com/)
- [Postman](https://www.postman.com/) - API测试
- [ModbusTools](https://github.com/AndreyNautilus/ModbusTools) - Modbus调试

## 9. 团队协作

### 9.1 沟通渠道

- **代码审查**：通过GitHub Pull Request
- **问题追踪**：使用GitHub Issues
- **文档更新**：直接提交到docs/目录

### 9.2 代码审查要点

在创建PR时，确保：

- [ ] 代码遵循项目风格指南
- [ ] 包含相应的测试用例
- [ ] 更新相关文档
- [ ] 提交消息格式正确
- [ ] CI/CD检查全部通过

### 9.3 获取帮助

如果遇到问题：

1. 查看项目文档和README
2. 搜索GitHub Issues中的类似问题
3. 创建新的Issue描述问题
4. 联系项目维护者

## 10. 下一步

现在您已经了解了项目的基本情况，建议按以下顺序开始：

1. **熟悉代码库**：浏览主要的源代码文件
2. **运行示例**：启动Node-RED和HLS服务，测试IPC通信
3. **阅读用户故事**：了解功能需求和验收标准
4. **选择任务**：从GitHub Issues中选择适合的任务开始
5. **贡献代码**：按照开发工作流提交您的首个PR

欢迎来到团队！如有任何问题，随时通过GitHub Issues联系我们。

---

_最后更新：2025-08-26_  
_维护者：开发团队_
