module.exports = {
  extends: ['@commitlint/config-conventional'],
  rules: {
    // 类型规则
    'type-enum': [
      2,
      'always',
      [
        'feat', // 新功能
        'fix', // 修复bug
        'docs', // 文档更新
        'style', // 格式化、缺失分号等，不影响代码含义的修改
        'refactor', // 重构（既不修复bug也不添加新功能的代码更改）
        'perf', // 性能改进
        'test', // 添加缺失的测试或修正现有的测试
        'build', // 影响构建系统或外部依赖的更改
        'ci', // CI配置文件和脚本的更改
        'chore', // 其他不修改src或test文件的更改
        'revert', // 回滚先前的提交
      ],
    ],

    // 主题规则
    'subject-case': [2, 'never', ['pascal-case', 'upper-case', 'start-case']],
    'subject-empty': [2, 'never'],
    'subject-full-stop': [2, 'never', '.'],
    'subject-max-length': [2, 'always', 72],
    'subject-min-length': [2, 'always', 3],

    // 类型规则
    'type-case': [2, 'always', 'lower-case'],
    'type-empty': [2, 'never'],

    // 范围规则
    'scope-case': [2, 'always', 'lower-case'],
    'scope-empty': [0, 'never'],
    'scope-enum': [
      2,
      'always',
      [
        'core', // 核心功能
        'ui', // 用户界面
        'api', // API相关
        'db', // 数据库
        'config', // 配置
        'docs', // 文档
        'tests', // 测试
        'deps', // 依赖管理
        'build', // 构建系统
        'ci', // CI/CD
        'hls-service', // HLS-Communication服务
        'node-red', // Node-RED相关
        'ipc', // 进程间通信
        'protocols', // 协议相关（Modbus等）
      ],
    ],

    // 头部规则
    'header-max-length': [2, 'always', 100],

    // 正文规则
    'body-leading-blank': [2, 'always'],
    'body-max-line-length': [2, 'always', 100],

    // 脚注规则
    'footer-leading-blank': [2, 'always'],
    'footer-max-line-length': [2, 'always', 100],
  },
};
