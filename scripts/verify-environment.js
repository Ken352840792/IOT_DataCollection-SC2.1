#!/usr/bin/env node

/**
 * 环境验证脚本
 * 检查Node.js、npm和Node-RED环境是否正确配置
 */

const { execSync } = require('child_process');
const fs = require('fs');
const path = require('path');

console.log('🔍 环境验证脚本开始...\n');

// 颜色输出
const colors = {
  green: '\x1b[32m',
  red: '\x1b[31m',
  yellow: '\x1b[33m',
  blue: '\x1b[34m',
  reset: '\x1b[0m'
};

function logSuccess(message) {
  console.log(`${colors.green}✅ ${message}${colors.reset}`);
}

function logError(message) {
  console.log(`${colors.red}❌ ${message}${colors.reset}`);
}

function logWarning(message) {
  console.log(`${colors.yellow}⚠️  ${message}${colors.reset}`);
}

function logInfo(message) {
  console.log(`${colors.blue}ℹ️  ${message}${colors.reset}`);
}

let errors = 0;
let warnings = 0;

// 1. 检查Node.js版本
try {
  const nodeVersion = process.version;
  const majorVersion = parseInt(nodeVersion.slice(1).split('.')[0]);
  
  logInfo(`Node.js版本: ${nodeVersion}`);
  
  if (majorVersion >= 18) {
    logSuccess('Node.js版本满足要求 (>=18.0.0)');
  } else {
    logError('Node.js版本过低，需要18.0.0或更高版本');
    errors++;
  }
} catch (err) {
  logError(`检查Node.js版本失败: ${err.message}`);
  errors++;
}

// 2. 检查npm版本
try {
  const npmVersion = execSync('npm --version', { encoding: 'utf8' }).trim();
  const majorVersion = parseInt(npmVersion.split('.')[0]);
  
  logInfo(`npm版本: ${npmVersion}`);
  
  if (majorVersion >= 8) {
    logSuccess('npm版本满足要求 (>=8.0.0)');
  } else {
    logError('npm版本过低，需要8.0.0或更高版本');
    errors++;
  }
} catch (err) {
  logError(`检查npm版本失败: ${err.message}`);
  errors++;
}

// 3. 检查package.json
try {
  const packagePath = path.join(process.cwd(), 'package.json');
  if (fs.existsSync(packagePath)) {
    const packageJson = JSON.parse(fs.readFileSync(packagePath, 'utf8'));
    logSuccess('package.json文件存在');
    
    // 检查必要的依赖
    if (packageJson.dependencies && packageJson.dependencies['node-red']) {
      logSuccess('Node-RED依赖已配置');
    } else {
      logWarning('Node-RED依赖未配置在package.json中');
      warnings++;
    }
  } else {
    logError('package.json文件不存在');
    errors++;
  }
} catch (err) {
  logError(`检查package.json失败: ${err.message}`);
  errors++;
}

// 4. 检查环境变量配置
try {
  const envExamplePath = path.join(process.cwd(), '.env.example');
  if (fs.existsSync(envExamplePath)) {
    logSuccess('.env.example文件存在');
  } else {
    logWarning('.env.example文件不存在');
    warnings++;
  }
} catch (err) {
  logError(`检查环境变量配置失败: ${err.message}`);
  errors++;
}

// 5. 检查项目目录结构
const requiredDirs = ['src', 'tests', 'config', 'logs'];
requiredDirs.forEach(dir => {
  const dirPath = path.join(process.cwd(), dir);
  if (fs.existsSync(dirPath)) {
    logSuccess(`目录 ${dir}/ 存在`);
  } else {
    logError(`目录 ${dir}/ 不存在`);
    errors++;
  }
});

// 6. 尝试检查Node-RED是否可用
try {
  execSync('node-red --version', { encoding: 'utf8', stdio: 'pipe' });
  logSuccess('Node-RED全局安装可用');
} catch (err) {
  try {
    // 尝试本地安装
    const nodeRedPath = path.join(process.cwd(), 'node_modules', '.bin', 'node-red');
    if (fs.existsSync(nodeRedPath)) {
      logSuccess('Node-RED本地安装可用');
    } else {
      logWarning('Node-RED未安装或不可用');
      warnings++;
    }
  } catch (localErr) {
    logWarning('Node-RED未安装或不可用');
    warnings++;
  }
}

// 输出总结
console.log('\n📊 验证结果总结:');
console.log(`   错误: ${errors}`);
console.log(`   警告: ${warnings}`);

if (errors === 0) {
  if (warnings === 0) {
    logSuccess('✨ 环境验证完全通过！');
    process.exit(0);
  } else {
    logWarning(`⚠️  环境基本可用，但有 ${warnings} 个警告`);
    process.exit(0);
  }
} else {
  logError(`💥 环境验证失败，发现 ${errors} 个错误`);
  process.exit(1);
}