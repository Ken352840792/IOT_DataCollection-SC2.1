# 贡献指南

欢迎为滠水采集服务项目做出贡献！本文档提供了参与项目开发的指南。

## 开发工作流

### 1. 开发环境设置

1. **安装必要软件**
   - Node.js 18.x LTS+
   - .NET Core 6.0+
   - Git
   - Visual Studio Code（推荐）

2. **克隆和设置项目**
   ```bash
   git clone <repository-url>
   cd SC2.1
   npm install
   ```

3. **配置开发环境**
   ```bash
   cp config/.env.example .env
   # 根据需要编辑环境变量
   ```

### 2. 分支策略

我们使用Git Flow工作流：

- **main** - 生产发布分支，仅包含稳定版本
- **develop** - 开发集成分支，包含最新开发功能
- **feature/<功能名>** - 功能开发分支
- **hotfix/<修复名>** - 紧急修复分支
- **release/<版本号>** - 发布准备分支

### 3. 开发流程

1. **创建功能分支**
   ```bash
   git checkout develop
   git pull origin develop
   git checkout -b feature/your-feature-name
   ```

2. **进行开发**
   - 遵循代码规范
   - 编写测试用例
   - 更新相关文档

3. **提交代码**
   ```bash
   git add .
   git commit -m "feat: add your feature description"
   ```

4. **创建Pull Request**
   - 推送分支到远程仓库
   - 创建PR到develop分支
   - 等待代码审查

## 代码规范

### 提交信息规范

使用Conventional Commits规范：

```
<type>[optional scope]: <description>

[optional body]

[optional footer(s)]
```

**类型说明：**
- `feat`: 新功能
- `fix`: 错误修复
- `docs`: 文档更新
- `style`: 代码格式化（不影响代码运行）
- `refactor`: 代码重构
- `test`: 添加或修改测试
- `chore`: 构建过程或辅助工具的变动

**示例：**
```
feat(hls-service): add device connection pooling

Implement connection pool for better resource management
and improved performance when handling multiple devices.

Closes #123
```

### 代码风格

#### JavaScript/Node.js
- 使用ESLint和Prettier进行代码格式化
- 使用2个空格缩进
- 使用单引号
- 行尾分号
- 最大行长度120字符

#### C#/.NET
- 遵循Microsoft C#编码规范
- 使用PascalCase命名公共成员
- 使用camelCase命名私有成员
- 适当的XML文档注释

### 文件命名规范

- **JavaScript文件：** kebab-case (例: `device-manager.js`)
- **C#文件：** PascalCase (例: `DeviceManager.cs`)
- **配置文件：** kebab-case (例: `app-config.json`)
- **测试文件：** 与被测试文件同名，添加`.test`或`.spec`后缀

## 测试要求

### 测试覆盖率
- 单元测试覆盖率 > 80%
- 所有公共API需要集成测试
- 重要功能需要端到端测试

### 运行测试
```bash
# 运行所有测试
npm test

# 运行特定类型的测试
npm run test:unit
npm run test:integration

# 生成覆盖率报告
npm run test:coverage

# 监听模式运行测试
npm run test:watch
```

### 测试编写指南
- 每个功能都应该有对应的单元测试
- 使用描述性的测试名称
- 测试应该独立且可重复运行
- 使用适当的Mock和Stub

## 文档要求

### API文档
- 所有公共API都需要详细文档
- 包含请求/响应示例
- 说明错误情况和处理方式

### 代码注释
- 复杂逻辑需要注释说明
- 公共接口需要完整的文档注释
- 重要的算法和业务逻辑需要说明

### README维护
- 新功能需要更新README
- 配置变更需要更新文档
- 重要的架构变更需要更新架构文档

## 问题报告

### Bug报告
创建Issue时请包含：
- 问题的详细描述
- 重现步骤
- 期望行为vs实际行为
- 环境信息（操作系统、Node.js版本等）
- 相关日志和错误信息

### 功能请求
请包含：
- 功能的详细描述
- 使用场景和需求背景
- 可能的实现方案
- 相关的设计考虑

## 代码审查

### 审查要点
- 代码逻辑正确性
- 性能影响
- 安全考虑
- 测试覆盖
- 文档完整性
- 编码规范遵循

### 审查流程
1. 自检：提交前自己审查代码
2. 自动检查：CI/CD自动运行测试和质量检查
3. 同行评审：至少一个团队成员审查
4. 最终审查：维护者最终审查和合并

## 发布流程

1. **创建发布分支**
   ```bash
   git checkout -b release/v1.0.0
   ```

2. **版本号更新**
   - 更新package.json中的版本号
   - 更新CHANGELOG.md

3. **测试和修复**
   - 运行完整测试套件
   - 修复发现的问题

4. **合并和标记**
   - 合并到main分支
   - 创建版本标签
   - 合并回develop分支

## 联系方式

如有任何问题，请：
- 创建GitHub Issue
- 联系项目维护者
- 参与项目讨论

感谢您的贡献！