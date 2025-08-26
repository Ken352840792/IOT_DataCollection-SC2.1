const net = require('net');

/**
 * 全面测试升级后的IPC服务器功能
 * 验证故事2.1的所有验收标准
 */

const SERVER_HOST = '127.0.0.1';
const SERVER_PORT = 8888;

class IpcServerTestSuite {
  constructor() {
    this.clients = [];
    this.testResults = [];
  }

  /**
   * 创建测试客户端
   */
  createTestClient(clientId) {
    return new Promise((resolve, reject) => {
      const client = new net.Socket();
      const clientInfo = {
        id: clientId,
        socket: client,
        connected: false,
        messages: [],
        errors: []
      };

      client.connect(SERVER_PORT, SERVER_HOST, () => {
        clientInfo.connected = true;
        console.log(`✓ 客户端 ${clientId} 连接成功`);
        resolve(clientInfo);
      });

      client.on('data', data => {
        const response = data.toString();
        try {
          const parsed = JSON.parse(response);
          clientInfo.messages.push(parsed);
          
          if (parsed.messageId) {
            console.log(`📨 客户端 ${clientId} 收到响应: ${parsed.messageId} (${parsed.success ? '成功' : '失败'})`);
          }
        } catch (e) {
          console.log(`📨 客户端 ${clientId} 收到原始响应: ${response}`);
        }
      });

      client.on('error', err => {
        clientInfo.errors.push(err);
        console.error(`❌ 客户端 ${clientId} 错误: ${err.message}`);
        reject(err);
      });

      client.on('close', () => {
        clientInfo.connected = false;
        console.log(`🔌 客户端 ${clientId} 连接已关闭`);
      });

      this.clients.push(clientInfo);
    });
  }

  /**
   * 发送消息
   */
  async sendMessage(client, command, data = {}) {
    const message = {
      messageId: `test-${Date.now()}-${Math.random().toString(36).substr(2, 9)}`,
      version: '1.0',
      command: command,
      data: data,
      timestamp: new Date().toISOString()
    };

    const jsonMessage = JSON.stringify(message);
    client.socket.write(jsonMessage);
    
    console.log(`📤 客户端 ${client.id} 发送: ${command} (${message.messageId})`);
    
    // 等待响应
    await new Promise(resolve => setTimeout(resolve, 100));
    return message.messageId;
  }

  /**
   * 测试1: 基本连通性测试
   */
  async testBasicConnectivity() {
    console.log('\n=== 测试1: 基本连通性测试 ===');
    
    try {
      const client = await this.createTestClient('connectivity-test');
      await this.sendMessage(client, 'ping');
      
      // 验证响应
      const response = client.messages.find(m => m.command === 'ping' || m.data?.message === 'pong');
      if (response && response.success) {
        this.testResults.push({ test: 'basic-connectivity', result: 'PASS', details: 'Ping-pong successful' });
        console.log('✅ 基本连通性测试通过');
      } else {
        throw new Error('未收到有效的ping响应');
      }
      
      client.socket.end();
    } catch (error) {
      this.testResults.push({ test: 'basic-connectivity', result: 'FAIL', error: error.message });
      console.log(`❌ 基本连通性测试失败: ${error.message}`);
    }
  }

  /**
   * 测试2: JSON协议验证
   */
  async testJsonProtocol() {
    console.log('\n=== 测试2: JSON协议验证 ===');
    
    try {
      const client = await this.createTestClient('json-protocol-test');
      
      // 测试各种命令
      const commands = ['ping', 'status', 'server_info', 'version', 'health_check'];
      
      for (const command of commands) {
        await this.sendMessage(client, command);
      }
      
      // 等待所有响应
      await new Promise(resolve => setTimeout(resolve, 500));
      
      // 验证响应格式
      let validResponses = 0;
      for (const msg of client.messages) {
        if (msg.messageId && msg.hasOwnProperty('success') && msg.timestamp && msg.version) {
          validResponses++;
        }
      }
      
      if (validResponses >= commands.length) {
        this.testResults.push({ test: 'json-protocol', result: 'PASS', details: `${validResponses} valid responses` });
        console.log(`✅ JSON协议验证通过 (${validResponses}个有效响应)`);
      } else {
        throw new Error(`只收到 ${validResponses} 个有效响应，期望 ${commands.length} 个`);
      }
      
      client.socket.end();
    } catch (error) {
      this.testResults.push({ test: 'json-protocol', result: 'FAIL', error: error.message });
      console.log(`❌ JSON协议验证失败: ${error.message}`);
    }
  }

  /**
   * 测试3: 多客户端并发连接
   */
  async testConcurrentConnections() {
    console.log('\n=== 测试3: 多客户端并发连接测试 ===');
    
    try {
      const clientCount = 5;
      const concurrentClients = [];
      
      // 创建多个并发连接
      for (let i = 1; i <= clientCount; i++) {
        const clientPromise = this.createTestClient(`concurrent-${i}`);
        concurrentClients.push(clientPromise);
      }
      
      const clients = await Promise.all(concurrentClients);
      console.log(`✓ 成功创建 ${clients.length} 个并发连接`);
      
      // 每个客户端发送消息
      const messagePromises = [];
      for (const client of clients) {
        messagePromises.push(this.sendMessage(client, 'status'));
      }
      
      await Promise.all(messagePromises);
      console.log('✓ 所有客户端消息发送完成');
      
      // 等待响应
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // 验证所有客户端都收到响应
      let successfulClients = 0;
      for (const client of clients) {
        if (client.messages.some(m => m.success && m.data)) {
          successfulClients++;
        }
        client.socket.end();
      }
      
      if (successfulClients === clientCount) {
        this.testResults.push({ test: 'concurrent-connections', result: 'PASS', details: `${successfulClients} clients successful` });
        console.log(`✅ 并发连接测试通过 (${successfulClients}/${clientCount})`);
      } else {
        throw new Error(`只有 ${successfulClients}/${clientCount} 个客户端成功`);
      }
    } catch (error) {
      this.testResults.push({ test: 'concurrent-connections', result: 'FAIL', error: error.message });
      console.log(`❌ 并发连接测试失败: ${error.message}`);
    }
  }

  /**
   * 测试4: 异常处理和恢复
   */
  async testExceptionHandling() {
    console.log('\n=== 测试4: 异常处理和恢复测试 ===');
    
    try {
      const client = await this.createTestClient('exception-test');
      
      // 测试无效JSON
      console.log('🔍 测试无效JSON处理...');
      client.socket.write('这不是有效的JSON');
      
      // 测试缺少必需字段
      console.log('🔍 测试缺少必需字段...');
      client.socket.write(JSON.stringify({ command: 'ping' })); // 缺少 messageId
      
      // 测试未知命令
      console.log('🔍 测试未知命令...');
      await this.sendMessage(client, 'unknown_command_xyz');
      
      // 等待响应
      await new Promise(resolve => setTimeout(resolve, 1000));
      
      // 验证错误处理
      const errorResponses = client.messages.filter(m => !m.success || m.error);
      
      if (errorResponses.length >= 2) {
        this.testResults.push({ test: 'exception-handling', result: 'PASS', details: `${errorResponses.length} error responses` });
        console.log(`✅ 异常处理测试通过 (收到${errorResponses.length}个错误响应)`);
      } else {
        throw new Error(`期望收到错误响应，但只收到 ${errorResponses.length} 个`);
      }
      
      client.socket.end();
    } catch (error) {
      this.testResults.push({ test: 'exception-handling', result: 'FAIL', error: error.message });
      console.log(`❌ 异常处理测试失败: ${error.message}`);
    }
  }

  /**
   * 测试5: 性能和响应时间
   */
  async testPerformance() {
    console.log('\n=== 测试5: 性能和响应时间测试 ===');
    
    try {
      const client = await this.createTestClient('performance-test');
      const messageCount = 10;
      const responseTimes = [];
      
      for (let i = 0; i < messageCount; i++) {
        const startTime = Date.now();
        await this.sendMessage(client, 'ping');
        
        // 等待响应
        await new Promise(resolve => setTimeout(resolve, 50));
        
        const endTime = Date.now();
        const responseTime = endTime - startTime;
        responseTimes.push(responseTime);
        
        console.log(`⏱️  消息 ${i + 1} 响应时间: ${responseTime}ms`);
      }
      
      const avgResponseTime = responseTimes.reduce((a, b) => a + b, 0) / responseTimes.length;
      const maxResponseTime = Math.max(...responseTimes);
      
      console.log(`📊 平均响应时间: ${avgResponseTime.toFixed(2)}ms`);
      console.log(`📊 最大响应时间: ${maxResponseTime}ms`);
      
      if (avgResponseTime < 100 && maxResponseTime < 200) {
        this.testResults.push({ 
          test: 'performance', 
          result: 'PASS', 
          details: `avg: ${avgResponseTime.toFixed(2)}ms, max: ${maxResponseTime}ms` 
        });
        console.log('✅ 性能测试通过');
      } else {
        throw new Error(`响应时间超出预期: avg=${avgResponseTime.toFixed(2)}ms, max=${maxResponseTime}ms`);
      }
      
      client.socket.end();
    } catch (error) {
      this.testResults.push({ test: 'performance', result: 'FAIL', error: error.message });
      console.log(`❌ 性能测试失败: ${error.message}`);
    }
  }

  /**
   * 运行所有测试
   */
  async runAllTests() {
    console.log('🚀 开始IPC服务器全面测试...\n');
    
    await this.testBasicConnectivity();
    await this.testJsonProtocol();
    await this.testConcurrentConnections();
    await this.testExceptionHandling();
    await this.testPerformance();
    
    // 显示测试结果汇总
    console.log('\n📊 === 测试结果汇总 ===');
    
    const passCount = this.testResults.filter(r => r.result === 'PASS').length;
    const failCount = this.testResults.filter(r => r.result === 'FAIL').length;
    
    this.testResults.forEach(result => {
      const emoji = result.result === 'PASS' ? '✅' : '❌';
      const details = result.details || result.error || '';
      console.log(`${emoji} ${result.test}: ${result.result} ${details}`);
    });
    
    console.log(`\n总结: ${passCount} 个测试通过, ${failCount} 个测试失败`);
    
    if (failCount === 0) {
      console.log('🎉 所有测试通过！IPC服务器功能正常。');
    } else {
      console.log('⚠️  存在失败的测试，需要进一步检查。');
    }
    
    // 清理所有连接
    this.clients.forEach(client => {
      if (client.socket && !client.socket.destroyed) {
        client.socket.end();
      }
    });
    
    return { passCount, failCount, results: this.testResults };
  }
}

// 运行测试
async function runTests() {
  const testSuite = new IpcServerTestSuite();
  
  try {
    const results = await testSuite.runAllTests();
    process.exit(results.failCount > 0 ? 1 : 0);
  } catch (error) {
    console.error('测试执行失败:', error);
    process.exit(1);
  }
}

if (require.main === module) {
  runTests();
}

module.exports = IpcServerTestSuite;