/**
 * 测试标准化协议v1.0 - 连接管理API
 * 此文件演示新的标准化协议功能
 */

const net = require('net');
const { v4: uuidv4 } = require('uuid');

// HLS客户端类
class HLSProtocolClient {
    constructor(options = {}) {
        this.host = options.host || 'localhost';
        this.port = options.port || 8888;
        this.timeout = options.timeout || 10000;
        this.socket = null;
    }

    // 连接到HLS服务器
    async connect() {
        return new Promise((resolve, reject) => {
            this.socket = net.createConnection(this.port, this.host);
            this.socket.setTimeout(this.timeout);

            this.socket.on('connect', () => {
                console.log(`✓ 连接到HLS服务器 ${this.host}:${this.port}`);
                resolve();
            });

            this.socket.on('error', (err) => {
                console.error('❌ 连接失败:', err.message);
                reject(err);
            });

            this.socket.on('timeout', () => {
                console.error('❌ 连接超时');
                this.socket.destroy();
                reject(new Error('Connection timeout'));
            });
        });
    }

    // 发送标准化协议消息
    async sendMessage(command, data = {}) {
        const request = {
            version: "1.0",
            messageId: uuidv4(),
            timestamp: new Date().toISOString(),
            command: command,
            data: data
        };

        console.log(`📤 发送命令: ${command}`, JSON.stringify(request, null, 2));

        return new Promise((resolve, reject) => {
            const messageJson = JSON.stringify(request) + '\n';
            
            this.socket.write(messageJson, (err) => {
                if (err) {
                    console.error('❌ 发送消息失败:', err.message);
                    reject(err);
                    return;
                }
            });

            // 监听响应
            this.socket.once('data', (data) => {
                try {
                    const responseJson = data.toString().trim();
                    const response = JSON.parse(responseJson);
                    
                    console.log(`📥 收到响应:`, JSON.stringify(response, null, 2));
                    
                    if (response.success) {
                        resolve(response);
                    } else {
                        reject(new Error(`Command failed: ${JSON.stringify(response.error)}`));
                    }
                } catch (parseError) {
                    console.error('❌ 响应解析失败:', parseError.message);
                    reject(parseError);
                }
            });
        });
    }

    // 断开连接
    disconnect() {
        if (this.socket) {
            this.socket.end();
            console.log('🔌 已断开连接');
        }
    }
}

// 测试函数集合
const tests = {
    // 测试协议信息查询
    async testProtocolInfo(client) {
        console.log('\n=== 测试协议信息查询 ===');
        try {
            const response = await client.sendMessage('protocolInfo');
            console.log('✓ 协议信息查询成功');
            return response;
        } catch (error) {
            console.error('❌ 协议信息查询失败:', error.message);
            throw error;
        }
    },

    // 测试设备连接建立
    async testDeviceConnect(client) {
        console.log('\n=== 测试设备连接建立 ===');
        try {
            const connectionRequest = {
                deviceConfig: {
                    type: "modbus-tcp",
                    host: "192.168.1.100",
                    port: 502,
                    timeout: 5000
                },
                dataPoints: [
                    {
                        name: "temperature",
                        address: "40001",
                        dataType: "float",
                        access: "read",
                        description: "温度传感器"
                    },
                    {
                        name: "humidity",
                        address: "40002", 
                        dataType: "float",
                        access: "read",
                        description: "湿度传感器"
                    }
                ]
            };

            const response = await client.sendMessage('connect', connectionRequest);
            console.log('✓ 设备连接建立成功');
            console.log('📋 连接信息:', {
                connectionId: response.data?.connectionId,
                status: response.data?.status,
                dataPointsConfigured: response.data?.dataPointsConfigured
            });
            return response.data?.connectionId;
        } catch (error) {
            console.error('❌ 设备连接建立失败:', error.message);
            throw error;
        }
    },

    // 测试连接状态查询
    async testConnectionStatus(client, connectionId) {
        console.log('\n=== 测试连接状态查询 ===');
        if (!connectionId) {
            console.log('⚠️ 跳过状态查询 - 没有有效的连接ID');
            return;
        }

        try {
            const response = await client.sendMessage('status', {
                connectionId: connectionId
            });
            console.log('✓ 连接状态查询成功');
            console.log('📊 状态信息:', {
                connectionId: response.data?.connectionId,
                status: response.data?.status,
                connectionHealth: response.data?.connectionHealth
            });
            return response;
        } catch (error) {
            console.error('❌ 连接状态查询失败:', error.message);
            throw error;
        }
    },

    // 测试连接参数验证
    async testConnectionValidation(client) {
        console.log('\n=== 测试连接参数验证 ===');
        try {
            const validationRequest = {
                deviceConfig: {
                    type: "modbus-tcp",
                    host: "192.168.1.100",
                    port: 502,
                    timeout: 5000
                },
                dataPoints: [
                    {
                        name: "test_point",
                        address: "40001",
                        dataType: "int16",
                        access: "read"
                    }
                ]
            };

            const response = await client.sendMessage('validateConnection', validationRequest);
            console.log('✓ 连接参数验证成功');
            console.log('📋 验证结果:', {
                valid: response.data?.valid,
                errors: response.data?.errors,
                dataPointsCount: response.data?.dataPointsCount
            });
            return response;
        } catch (error) {
            console.error('❌ 连接参数验证失败:', error.message);
            throw error;
        }
    },

    // 测试连接列表查询
    async testListConnections(client) {
        console.log('\n=== 测试连接列表查询 ===');
        try {
            const response = await client.sendMessage('listConnections');
            console.log('✓ 连接列表查询成功');
            console.log('📋 连接统计:', {
                totalCount: response.data?.totalCount,
                activeCount: response.data?.activeCount,
                maxConnections: response.data?.maxConnections
            });
            
            if (response.data?.connections?.length > 0) {
                console.log('🔗 现有连接:');
                response.data.connections.forEach((conn, index) => {
                    console.log(`  ${index + 1}. ${conn.connectionId} (${conn.deviceType}) - ${conn.status}`);
                });
            }
            return response;
        } catch (error) {
            console.error('❌ 连接列表查询失败:', error.message);
            throw error;
        }
    },

    // 测试设备断开连接
    async testDeviceDisconnect(client, connectionId) {
        console.log('\n=== 测试设备断开连接 ===');
        if (!connectionId) {
            console.log('⚠️ 跳过断开连接 - 没有有效的连接ID');
            return;
        }

        try {
            const response = await client.sendMessage('disconnect', {
                connectionId: connectionId
            });
            console.log('✓ 设备断开连接成功');
            console.log('📋 断开信息:', {
                connectionId: response.data?.connectionId,
                status: response.data?.status,
                message: response.data?.message
            });
            return response;
        } catch (error) {
            console.error('❌ 设备断开连接失败:', error.message);
            throw error;
        }
    },

    // 测试服务器状态查询
    async testServerStatus(client) {
        console.log('\n=== 测试服务器状态查询 ===');
        try {
            const response = await client.sendMessage('serverStatus');
            console.log('✓ 服务器状态查询成功');
            console.log('📊 服务器信息:', {
                status: response.data?.status,
                uptime: response.data?.uptime,
                activeConnections: response.data?.activeConnections,
                messagesProcessed: response.data?.messagesProcessed,
                memoryUsageMB: response.data?.memoryUsageMB
            });
            return response;
        } catch (error) {
            console.error('❌ 服务器状态查询失败:', error.message);
            throw error;
        }
    }
};

// 主测试流程
async function runProtocolTests() {
    console.log('🚀 开始标准化协议v1.0测试');
    console.log('==========================================');
    
    const client = new HLSProtocolClient();
    let connectionId = null;

    try {
        // 1. 连接到服务器
        await client.connect();

        // 2. 测试协议信息查询
        await tests.testProtocolInfo(client);

        // 3. 测试服务器状态
        await tests.testServerStatus(client);

        // 4. 测试连接参数验证
        await tests.testConnectionValidation(client);

        // 5. 测试设备连接建立
        connectionId = await tests.testDeviceConnect(client);

        // 6. 测试连接状态查询
        await tests.testConnectionStatus(client, connectionId);

        // 7. 测试连接列表查询
        await tests.testListConnections(client);

        // 8. 测试设备断开连接
        await tests.testDeviceDisconnect(client, connectionId);

        // 9. 再次查询连接列表确认断开
        await tests.testListConnections(client);

        console.log('\n🎉 所有测试完成！');
        console.log('==========================================');

    } catch (error) {
        console.error('\n💥 测试失败:', error.message);
        console.log('==========================================');
    } finally {
        // 清理连接
        client.disconnect();
    }
}

// 错误处理
process.on('unhandledRejection', (reason, promise) => {
    console.error('未处理的Promise拒绝:', reason);
    process.exit(1);
});

process.on('uncaughtException', (error) => {
    console.error('未捕获的异常:', error);
    process.exit(1);
});

// 启动测试
if (require.main === module) {
    runProtocolTests().then(() => {
        console.log('✅ 测试脚本执行完成');
        process.exit(0);
    }).catch((error) => {
        console.error('❌ 测试脚本执行失败:', error);
        process.exit(1);
    });
}

module.exports = { HLSProtocolClient, tests };