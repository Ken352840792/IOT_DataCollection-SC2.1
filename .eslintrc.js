module.exports = {
  env: {
    browser: true,
    es2021: true,
    node: true,
    jest: true,
  },
  extends: ['eslint:recommended', 'plugin:prettier/recommended'],
  parserOptions: {
    ecmaVersion: 'latest',
    sourceType: 'module',
  },
  plugins: ['prettier'],
  rules: {
    // Prettier规则
    'prettier/prettier': 'error',

    // ESLint推荐规则的定制化
    'no-unused-vars': [
      'error',
      {
        varsIgnorePattern: '^_',
        argsIgnorePattern: '^_',
      },
    ],
    'no-console': ['warn', { allow: ['warn', 'error'] }],
    'no-debugger': 'error',

    // 代码质量规则
    'prefer-const': 'error',
    'no-var': 'error',
    'object-shorthand': 'error',
    'prefer-arrow-callback': 'error',
    'prefer-template': 'error',

    // 最佳实践
    eqeqeq: ['error', 'always'],
    curly: ['error', 'all'],
    'no-eval': 'error',
    'no-implied-eval': 'error',
    'no-new-func': 'error',
    'no-script-url': 'error',

    // 风格规则（与Prettier配合）
    'max-len': [
      'error',
      {
        code: 100,
        ignoreUrls: true,
        ignoreStrings: true,
        ignoreTemplateLiterals: true,
      },
    ],
    'max-depth': ['error', 4],
    complexity: ['warn', 10],
  },

  // 忽略特定文件
  ignorePatterns: [
    'node_modules/',
    'dist/',
    'build/',
    'coverage/',
    '*.min.js',
    'src/hls-service/', // C#项目目录
  ],

  // 针对不同文件类型的特殊配置
  overrides: [
    {
      files: ['**/*.test.js', '**/*.spec.js'],
      env: {
        jest: true,
      },
      rules: {
        'no-console': 'off', // 测试文件允许console
      },
    },
    {
      files: ['scripts/**/*.js'],
      rules: {
        'no-console': 'off', // 脚本文件允许console
      },
    },
    {
      files: ['config/**/*.js', '*.config.js'],
      rules: {
        'no-console': 'off', // 配置文件允许console
      },
    },
  ],
};
