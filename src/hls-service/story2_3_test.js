const net = require('net');

class Story23Tester {
    constructor() {
        this.client = null;
        this.messageId = 1;
    }

    async connect() {
        return new Promise((resolve, reject) => {
            this.client = new net.Socket();
            
            this.client.connect(8888, '127.0.0.1', () => {
                console.log('[测试] 已连接到 HLS 服务');
                resolve();
            });
            
            this.client.on('error', (error) => {
                console.error('[测试] 连接错误:', error.message);
                reject(error);
            });
            
            this.client.on('close', () => {
                console.log('[测试] 连接已关闭');
            });
        });
    }

    async sendMessage(command, data = {}) {
        return new Promise((resolve, reject) => {
            const message = {
                messageId: `msg_${this.messageId++}`,
                command: command,
                data: data,
                timestamp: new Date().toISOString()
            };

            let responseData = '';
            
            const onData = (data) => {
                responseData += data.toString();
                try {
                    const response = JSON.parse(responseData);
                    this.client.removeListener('data', onData);
                    resolve(response);
                } catch (e) {
                    // 数据可能还没接收完整
                }
            };

            this.client.on('data', onData);

            console.log(`[测试] 发送命令: ${command}`, JSON.stringify(data, null, 2));
            this.client.write(JSON.stringify(message) + '\n');

            // 超时处理
            setTimeout(() => {
                this.client.removeListener('data', onData);
                reject(new Error('命令执行超时'));
            }, 5000);
        });
    }

    async testGetSchemas() {
        console.log('\n=== 测试 get_schemas 命令 ===');
        try {
            const response = await this.sendMessage('get_schemas', { 
                schemaType: 'device' 
            });
            console.log('[响应]', JSON.stringify(response, null, 2));
            
            if (response.success && response.data) {
                console.log('✅ get_schemas 命令成功');
                return true;
            } else {
                console.log('❌ get_schemas 命令失败');
                return false;
            }
        } catch (error) {
            console.error('❌ get_schemas 测试异常:', error.message);
            return false;
        }
    }

    async testValidateConfiguration() {
        console.log('\n=== 测试 validate_configuration 命令 ===');
        
        const testConfig = {
            deviceId: "test-device-01",
            type: 0, // ModbusTcp
            name: "测试设备",
            connection: {
                host: "192.168.1.100",
                port: 502,
                timeoutMs: 5000,
                station: 1
            }
        };

        try {
            const response = await this.sendMessage('validate_configuration', {
                configurationType: 'device',
                configuration: testConfig
            });
            console.log('[响应]', JSON.stringify(response, null, 2));
            
            if (response.success !== undefined) {
                console.log('✅ validate_configuration 命令成功');
                return true;
            } else {
                console.log('❌ validate_configuration 命令失败');
                return false;
            }
        } catch (error) {
            console.error('❌ validate_configuration 测试异常:', error.message);
            return false;
        }
    }

    async testConfigureDatapoints() {
        console.log('\n=== 测试 configure_datapoints 命令 ===');
        
        const datapointConfig = {
            deviceId: "test-device-01",
            version: "1.0",
            groups: [
                {
                    groupId: "group-001",
                    groupName: "温度传感器组",
                    description: "车间温度监控点位",
                    scanIntervalMs: 2000,
                    enabled: true,
                    dataPoints: [
                        {
                            address: "40001",
                            dataType: "Int16",
                            name: "温度1",
                            description: "1号温度传感器",
                            accessMode: "Read",
                            unit: "°C",
                            scaleFactor: 0.1,
                            offset: 0.0,
                            enabled: true
                        },
                        {
                            address: "40002",
                            dataType: "Int16", 
                            name: "温度2",
                            description: "2号温度传感器",
                            accessMode: "Read",
                            unit: "°C",
                            scaleFactor: 0.1,
                            offset: 0.0,
                            enabled: true
                        }
                    ]
                }
            ],
            standalonePoints: [
                {
                    address: "40101",
                    dataType: "Bool",
                    name: "运行状态",
                    description: "设备运行状态指示",
                    accessMode: "Read",
                    enabled: true
                }
            ]
        };

        try {
            const response = await this.sendMessage('configure_datapoints', datapointConfig);
            console.log('[响应]', JSON.stringify(response, null, 2));
            
            if (response.success !== undefined) {
                console.log('✅ configure_datapoints 命令成功');
                return true;
            } else {
                console.log('❌ configure_datapoints 命令失败');
                return false;
            }
        } catch (error) {
            console.error('❌ configure_datapoints 测试异常:', error.message);
            return false;
        }
    }

    async testBatchDatapointOperation() {
        console.log('\n=== 测试 batch_datapoint_operation 命令 ===');
        
        const batchRequest = {
            deviceId: "test-device-01",
            operation: "Read",
            parallel: true,
            timeoutMs: 5000,
            dataPoints: [
                {
                    address: "40001",
                    dataType: "Int16",
                    name: "温度1",
                    accessMode: "Read"
                },
                {
                    address: "40002",
                    dataType: "Int16",
                    name: "温度2", 
                    accessMode: "Read"
                }
            ]
        };

        try {
            const response = await this.sendMessage('batch_datapoint_operation', batchRequest);
            console.log('[响应]', JSON.stringify(response, null, 2));
            
            if (response.success !== undefined) {
                console.log('✅ batch_datapoint_operation 命令成功');
                return true;
            } else {
                console.log('❌ batch_datapoint_operation 命令失败');
                return false;
            }
        } catch (error) {
            console.error('❌ batch_datapoint_operation 测试异常:', error.message);
            return false;
        }
    }

    async testInvalidConfiguration() {
        console.log('\n=== 测试无效配置验证 ===');
        
        const invalidConfig = {
            // 缺少必需的 deviceId
            type: 0,
            connection: {
                host: "", // 空的主机地址
                port: -1, // 无效端口
                timeoutMs: 100 // 超时时间太短
            }
        };

        try {
            const response = await this.sendMessage('validate_configuration', {
                configurationType: 'device',
                configuration: invalidConfig
            });
            console.log('[响应]', JSON.stringify(response, null, 2));
            
            if (response.success === false && response.error) {
                console.log('✅ 无效配置验证正常工作');
                return true;
            } else {
                console.log('❌ 无效配置验证未正常工作');
                return false;
            }
        } catch (error) {
            console.error('❌ 无效配置验证测试异常:', error.message);
            return false;
        }
    }

    async runAllTests() {
        console.log('=== Story 2.3 数据点位配置管理测试 ===\n');
        
        let passedTests = 0;
        let totalTests = 5;

        try {
            await this.connect();
            
            // 执行所有测试
            if (await this.testGetSchemas()) passedTests++;
            if (await this.testValidateConfiguration()) passedTests++;
            if (await this.testConfigureDatapoints()) passedTests++;
            if (await this.testBatchDatapointOperation()) passedTests++;
            if (await this.testInvalidConfiguration()) passedTests++;
            
            console.log(`\n=== 测试结果 ===`);
            console.log(`通过: ${passedTests}/${totalTests}`);
            console.log(`成功率: ${(passedTests/totalTests*100).toFixed(1)}%`);
            
            if (passedTests === totalTests) {
                console.log('🎉 所有Story 2.3功能测试通过！');
            } else {
                console.log('⚠️  部分测试失败，需要检查实现');
            }
            
        } catch (error) {
            console.error('测试执行异常:', error.message);
        } finally {
            if (this.client) {
                this.client.end();
            }
        }
    }

    disconnect() {
        if (this.client) {
            this.client.end();
        }
    }
}

// 运行测试
const tester = new Story23Tester();
tester.runAllTests().then(() => {
    console.log('测试完成');
    process.exit(0);
}).catch(error => {
    console.error('测试失败:', error);
    process.exit(1);
});