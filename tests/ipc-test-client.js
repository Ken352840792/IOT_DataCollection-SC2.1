const net = require('net');

/**
 * 测试IPC通信的Node.js客户端
 * 用于验证与HLS-Communication服务的TCP Socket连接
 */

const SERVER_HOST = '127.0.0.1';
const SERVER_PORT = 8888;

class IPCTestClient {
  constructor() {
    this.client = null;
    this.connected = false;
  }

  /**
   * 连接到HLS-Communication服务
   */
  async connect() {
    return new Promise((resolve, reject) => {
      this.client = new net.Socket();

      this.client.connect(SERVER_PORT, SERVER_HOST, () => {
        console.log('✓ 已连接到HLS-Communication服务');
        this.connected = true;
        resolve();
      });

      this.client.on('data', data => {
        const response = data.toString();
        console.log('收到响应:', response);
      });

      this.client.on('error', err => {
        console.error('连接错误:', err.message);
        this.connected = false;
        reject(err);
      });

      this.client.on('close', () => {
        console.log('连接已关闭');
        this.connected = false;
      });
    });
  }

  /**
   * 发送JSON消息
   */
  async sendMessage(command, data = {}) {
    if (!this.connected) {
      throw new Error('未连接到服务器');
    }

    const message = {
      messageId: `test-${Date.now()}`,
      command,
      timestamp: new Date().toISOString(),
      ...data,
    };

    const jsonMessage = JSON.stringify(message);
    console.log(`发送命令: ${command}`);
    console.log(`消息内容: ${jsonMessage}`);

    this.client.write(jsonMessage);
  }

  /**
   * 断开连接
   */
  disconnect() {
    if (this.client) {
      this.client.end();
    }
  }

  /**
   * 等待指定时间
   */
  wait(ms) {
    return new Promise(resolve => setTimeout(resolve, ms));
  }
}

/**
 * 执行IPC通信测试套件
 */
async function runTests() {
  console.log('=== HLS-Communication IPC 通信测试 ===\n');

  const client = new IPCTestClient();

  try {
    // 测试1: 连接测试
    console.log('测试1: 服务连接...');
    await client.connect();
    await client.wait(100);

    // 测试2: Ping命令
    console.log('\n测试2: Ping命令...');
    await client.sendMessage('ping');
    await client.wait(500);

    // 测试3: 状态查询
    console.log('\n测试3: 状态查询...');
    await client.sendMessage('status');
    await client.wait(500);

    // 测试4: Modbus测试命令
    console.log('\n测试4: Modbus测试...');
    await client.sendMessage('test_modbus');
    await client.wait(500);

    // 测试5: 未知命令（错误处理测试）
    console.log('\n测试5: 未知命令测试...');
    await client.sendMessage('unknown_command');
    await client.wait(500);

    console.log('\n✓ 所有测试完成');
  } catch (error) {
    console.error('测试失败:', error.message);
    process.exit(1);
  } finally {
    client.disconnect();
    // 等待连接关闭
    setTimeout(() => {
      console.log('\n测试客户端已退出');
      process.exit(0);
    }, 200);
  }
}

// 如果直接运行此脚本则执行测试
if (require.main === module) {
  runTests().catch(console.error);
}

module.exports = IPCTestClient;
