# 配置文件目录

本目录包含项目的所有配置文件。

## 配置文件说明

- **development.json** - 开发环境配置
- **production.json** - 生产环境配置
- **devices.json** - 设备连接配置模板
- **logging.json** - 日志配置

## 环境变量

项目使用以下环境变量：

- `NODE_ENV` - 运行环境（development/production）
- `HLS_SERVICE_PORT` - HLS服务监听端口（默认：8888）
- `LOG_LEVEL` - 日志级别（debug/info/warn/error）

## 配置优先级

1. 环境变量
2. 环境特定配置文件
3. 默认配置

## 安全注意事项

- 敏感信息（密码、密钥）使用环境变量
- 配置文件中使用占位符，避免硬编码
- 生产环境配置文件需要额外的访问控制