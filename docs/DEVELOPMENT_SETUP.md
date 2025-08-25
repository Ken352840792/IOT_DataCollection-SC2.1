# 开发环境设置指南

本文档提供SS_SC_IOT项目的完整开发环境设置指南。

## 环境要求

### 必需软件
- **Node.js**: 18.x LTS或更高版本
- **npm**: 8.0.0或更高版本  
- **.NET Core**: 6.0+（用于HLS-Communication服务）
- **Git**: 版本控制

### 推荐工具
- **Visual Studio Code**: 主要IDE
- **Node-RED**: 数据流编排平台
- **Postman**: API测试工具

## 快速开始

### 1. 克隆项目
```bash
git clone <repository-url>
cd SS_SC_IOT
```

### 2. 环境验证
```bash
# 检查基础环境
npm run verify-env

# 如果验证失败，请根据提示安装缺失的软件
```

### 3. 安装依赖
```bash
# 安装主项目依赖
npm install

# 安装定制节点依赖
npm run install:nodes
```

### 4. 环境配置
```bash
# 复制环境变量模板
cp .env.example .env

# 编辑环境变量（根据需要）
nano .env
```

## Node-RED开发环境

### 启动Node-RED
```bash
# 生产模式
npm run start:nodered

# 开发模式（详细日志）
npm run start:nodered-dev
```

### 访问Node-RED界面
- **URL**: http://localhost:1880/admin
- **API**: http://localhost:1880/api
- **用户目录**: ./nodered-data/

### Node-RED配置

**重要配置文件：**
- `nodered-data/settings.js` - 主要配置
- `nodered-data/package.json` - Node-RED依赖
- `src/nodes/` - 定制节点目录

**已配置功能：**
- 项目支持（启用）
- 调色板编辑（启用）
- 文件日志（自动轮转）
- 本地文件系统存储
- 定制节点加载路径

## 定制节点开发

### 节点结构
```
src/nodes/
├── package.json          # 节点包配置
├── hls-read/             # 读取节点
│   ├── hls-read.js       # 节点逻辑
│   └── hls-read.html     # 节点UI
├── hls-write/            # 写入节点
│   ├── hls-write.js      # 节点逻辑
│   └── hls-write.html    # 节点UI
└── test/                 # 节点测试
```

### 开发工作流
```bash
# 启动Node-RED开发模式
npm run start:nodered-dev

# 在另一个终端中运行节点测试
npm run test:nodes

# 修改节点后需要重启Node-RED加载变更
```

### 节点注册
定制节点会自动注册到"HLS通信"分类中：
- **hls-read**: 蓝绿色，用于数据读取
- **hls-write**: 橙色，用于数据写入

## 开发脚本

### 验证和设置
```bash
npm run verify-env        # 环境验证
npm run setup            # 完整设置（验证+安装）
```

### Node-RED相关
```bash
npm run start:nodered    # 启动Node-RED（生产模式）
npm run start:nodered-dev # 启动Node-RED（开发模式）
```

### 测试
```bash
npm test                 # 运行所有测试
npm run test:nodes       # 运行定制节点测试
npm run test:coverage    # 生成覆盖率报告
```

### 代码质量
```bash
npm run lint            # 代码质量检查
npm run lint:fix        # 自动修复问题
npm run format          # 代码格式化
```

## 环境变量配置

### 核心配置
```env
NODE_ENV=development
HLS_SERVICE_HOST=127.0.0.1
HLS_SERVICE_PORT=8888
NODERED_PORT=1880
LOG_LEVEL=info
```

### 开发特定
```env
DEBUG=ss-sc-iot:*
DEVELOPMENT_MODE=true
```

## 目录结构说明

```
SS_SC_IOT/
├── src/                 # 源代码
│   ├── hls-service/    # HLS通信服务（C#）
│   └── nodes/          # Node-RED定制节点
├── tests/              # 测试文件
├── config/             # 配置文件
├── docs/               # 项目文档
├── nodered-data/       # Node-RED用户数据
├── scripts/            # 工具脚本
└── logs/               # 日志文件
```

## 常见问题

### Q: Node-RED无法加载定制节点？
**A**: 
1. 检查`src/nodes/package.json`中的节点注册配置
2. 重启Node-RED服务
3. 查看Node-RED启动日志中的错误信息

### Q: 环境验证失败？
**A**: 
1. 检查Node.js版本是否>=18.0.0
2. 确保npm版本>=8.0.0
3. 运行`npm install`安装依赖

### Q: 定制节点测试失败？
**A**:
1. 确保已安装测试依赖：`npm run install:nodes`
2. 检查测试文件中的节点路径是否正确
3. 查看测试输出中的详细错误信息

## 下一步

环境设置完成后，你可以：

1. **开发HLS-Communication服务**（史诗2）
2. **完善定制节点功能**（史诗3）
3. **创建数据流模板**（史诗4）

## 获取帮助

- 查看项目文档：`docs/`
- 运行环境验证：`npm run verify-env`
- 查看Node-RED日志：`logs/node-red.log`
- 创建Issue报告问题