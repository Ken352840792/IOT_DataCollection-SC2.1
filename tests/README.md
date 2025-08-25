# 测试目录

本目录包含项目的所有测试代码。

## 目录结构

- **unit/** - 单元测试
  - 测试单个函数和类的功能
  - 使用模拟对象隔离外部依赖
  - 目标覆盖率：>80%

- **integration/** - 集成测试
  - 测试组件间的集成
  - 测试与外部服务的交互
  - 端到端功能测试

## 测试框架

- **Node.js项目：** Jest + Supertest
- **C#项目：** xUnit + Moq
- **持续集成：** GitHub Actions

## 运行测试

```bash
# 运行所有测试
npm test

# 运行单元测试
npm run test:unit

# 运行集成测试
npm run test:integration

# 生成覆盖率报告
npm run test:coverage
```