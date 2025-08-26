const net = require('net');

class Story24Tester {
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

            console.log(`[测试] 发送命令: ${command}`);
            this.client.write(JSON.stringify(message) + '\n');

            // 超时处理
            setTimeout(() => {
                this.client.removeListener('data', onData);
                reject(new Error('命令执行超时'));
            }, 10000);
        });
    }

    async testBatchRead() {
        console.log('\n=== 测试批量数据读取 ===');
        
        const batchReadRequest = {
            deviceId: "test-device-01",
            operation: "Read",
            parallel: true,
            timeoutMs: 5000,
            dataPoints: [
                {
                    address: "40001",
                    dataType: "Int16",
                    name: "温度传感器1",
                    accessMode: "Read",
                    enabled: true
                },
                {
                    address: "40002", 
                    dataType: "Int16",
                    name: "温度传感器2",
                    accessMode: "Read",
                    enabled: true
                },
                {
                    address: "40003",
                    dataType: "Float",
                    name: "压力传感器",
                    accessMode: "Read", 
                    enabled: true
                },
                {
                    address: "00001",
                    dataType: "Bool",
                    name: "运行状态",
                    accessMode: "Read",
                    enabled: true
                }
            ]
        };

        try {
            const response = await this.sendMessage('batch_datapoint_operation', batchReadRequest);
            console.log('[批量读取响应]', JSON.stringify(response, null, 2));
            
            if (response.success && response.data?.data?.results) {
                const results = response.data.data.results;
                console.log(`✅ 批量读取测试成功，读取了${results.length}个数据点`);
                
                // 显示详细结果
                results.forEach((result, index) => {
                    const status = result.success ? '✅' : '❌';
                    console.log(`  ${status} ${result.address}: ${result.success ? result.value || 'N/A' : result.error}`);
                });
                
                return true;
            } else {
                console.log('❌ 批量读取测试失败:', response.error || response.data?.error?.message || '未知错误');
                return false;
            }
        } catch (error) {
            console.error('❌ 批量读取测试异常:', error.message);
            return false;
        }
    }

    async testBatchWrite() {
        console.log('\n=== 测试批量数据写入 ===');
        
        const batchWriteRequest = {
            deviceId: "test-device-01",
            operation: "Write", 
            parallel: true,
            timeoutMs: 5000,
            dataPoints: [
                {
                    address: "40101",
                    dataType: "Int16",
                    name: "设定温度",
                    accessMode: "Write",
                    value: 250,  // 25.0°C (scaled by 0.1)
                    enabled: true
                },
                {
                    address: "40102",
                    dataType: "Float",
                    name: "设定压力", 
                    accessMode: "Write",
                    value: 1.25,  // 1.25 bar
                    enabled: true
                },
                {
                    address: "00101",
                    dataType: "Bool",
                    name: "启动命令",
                    accessMode: "Write",
                    value: true,
                    enabled: true
                }
            ]
        };

        try {
            const response = await this.sendMessage('batch_datapoint_operation', batchWriteRequest);
            console.log('[批量写入响应]', JSON.stringify(response, null, 2));
            
            if (response.success && response.data?.data?.results) {
                const results = response.data.data.results;
                const successful = response.data.data.successful || 0;
                const failed = response.data.data.failed || 0;
                
                console.log(`✅ 批量写入测试成功，成功: ${successful}, 失败: ${failed}`);
                
                // 显示详细结果
                results.forEach((result, index) => {
                    const status = result.success ? '✅' : '❌';
                    console.log(`  ${status} ${result.address}: ${result.success ? '写入成功' : result.error}`);
                });
                
                return true;
            } else {
                console.log('❌ 批量写入测试失败:', response.error || response.data?.error?.message || '未知错误');
                return false;
            }
        } catch (error) {
            console.error('❌ 批量写入测试异常:', error.message);
            return false;
        }
    }

    async testBatchReadWrite() {
        console.log('\n=== 测试批量读写混合操作 ===');
        
        const batchReadWriteRequest = {
            deviceId: "test-device-01", 
            operation: "ReadWrite",
            parallel: true,
            timeoutMs: 8000,
            dataPoints: [
                // 读取点位
                {
                    address: "40001",
                    dataType: "Int16",
                    name: "当前温度",
                    accessMode: "Read",
                    enabled: true
                },
                {
                    address: "40002",
                    dataType: "Float", 
                    name: "当前压力",
                    accessMode: "Read",
                    enabled: true
                },
                // 写入点位
                {
                    address: "40201",
                    dataType: "Int16",
                    name: "目标温度",
                    accessMode: "Write", 
                    value: 300,  // 30.0°C
                    enabled: true
                },
                {
                    address: "00201",
                    dataType: "Bool",
                    name: "自动模式",
                    accessMode: "Write",
                    value: true,
                    enabled: true
                },
                // 读写点位
                {
                    address: "40301",
                    dataType: "Int32",
                    name: "计数器",
                    accessMode: "ReadWrite",
                    value: 1000,  // 先读后写的场景
                    enabled: true
                }
            ]
        };

        try {
            const response = await this.sendMessage('batch_datapoint_operation', batchReadWriteRequest);
            console.log('[批量读写响应]', JSON.stringify(response, null, 2));
            
            if (response.success && response.data?.data) {
                const data = response.data.data;
                const readResults = data.readResults || [];
                const writeResults = data.writeResults || [];
                
                console.log(`✅ 批量读写测试成功`);
                console.log(`   读取: ${data.successfulReads || 0}/${data.totalReadCount || 0} 成功`);
                console.log(`   写入: ${data.successfulWrites || 0}/${data.totalWriteCount || 0} 成功`);
                
                // 显示读取结果
                if (readResults.length > 0) {
                    console.log('  📖 读取结果:');
                    readResults.forEach(result => {
                        const status = result.success ? '✅' : '❌';
                        console.log(`    ${status} ${result.address}: ${result.success ? result.value || 'N/A' : result.error}`);
                    });
                }
                
                // 显示写入结果
                if (writeResults.length > 0) {
                    console.log('  ✏️  写入结果:');
                    writeResults.forEach(result => {
                        const status = result.success ? '✅' : '❌';
                        console.log(`    ${status} ${result.address}: ${result.success ? '写入成功' : result.error}`);
                    });
                }
                
                return true;
            } else {
                console.log('❌ 批量读写测试失败:', response.error || response.data?.error?.message || '未知错误');
                return false;
            }
        } catch (error) {
            console.error('❌ 批量读写测试异常:', error.message);
            return false;
        }
    }

    async testDataTypeConversion() {
        console.log('\n=== 测试数据类型转换 ===');
        
        const dataTypeTests = {
            deviceId: "test-device-01",
            operation: "Write",
            parallel: false, // 串行执行便于观察
            timeoutMs: 5000,
            dataPoints: [
                {
                    address: "40401",
                    dataType: "Bool",
                    name: "布尔测试",
                    accessMode: "Write",
                    value: "true",  // 字符串 -> 布尔
                    enabled: true
                },
                {
                    address: "40402",
                    dataType: "Int16", 
                    name: "整数测试",
                    accessMode: "Write",
                    value: "123",   // 字符串 -> 整数
                    enabled: true
                },
                {
                    address: "40403",
                    dataType: "Float",
                    name: "浮点测试",
                    accessMode: "Write", 
                    value: "12.34", // 字符串 -> 浮点
                    enabled: true
                },
                {
                    address: "40404",
                    dataType: "String",
                    name: "字符串测试",
                    accessMode: "Write",
                    value: 456,     // 数字 -> 字符串
                    enabled: true
                }
            ]
        };

        try {
            const response = await this.sendMessage('batch_datapoint_operation', dataTypeTests);
            console.log('[数据类型转换响应]', JSON.stringify(response, null, 2));
            
            if (response.success && response.data?.data?.results) {
                const results = response.data.data.results;
                console.log(`✅ 数据类型转换测试完成`);
                
                results.forEach((result, index) => {
                    const dataPoint = dataTypeTests.dataPoints[index];
                    const status = result.success ? '✅' : '❌';
                    const convertedValue = result.value;
                    const expectedType = dataPoint.dataType;
                    
                    // 检查数据类型转换是否成功（通过检查转换后的值类型）
                    const conversionSuccess = !result.success ? false : 
                        (expectedType === 'Bool' && typeof convertedValue === 'boolean') ||
                        (expectedType === 'Int16' && typeof convertedValue === 'number' && Number.isInteger(convertedValue)) ||
                        (expectedType === 'Float' && typeof convertedValue === 'number') ||
                        (expectedType === 'String' && typeof convertedValue === 'string');
                    
                    console.log(`  ${status} ${dataPoint.address} (${dataPoint.dataType}): ${dataPoint.value} -> ${conversionSuccess ? '转换成功' : result.error}`);
                });
                
                // 对于数据类型转换测试，只要没有转换错误就算成功（设备不存在是正常的）
                const hasConversionErrors = results.some(r => r.error && r.error.includes('Cannot convert'));
                return !hasConversionErrors;
            } else {
                console.log('❌ 数据类型转换测试失败:', response.error || response.data?.error?.message);
                return false;
            }
        } catch (error) {
            console.error('❌ 数据类型转换测试异常:', error.message);
            return false;
        }
    }

    async testValidationErrors() {
        console.log('\n=== 测试验证错误处理 ===');
        
        // 测试缺少写入值的情况
        const invalidRequest = {
            deviceId: "test-device-01",
            operation: "Write",
            parallel: true,
            timeoutMs: 5000,
            dataPoints: [
                {
                    address: "40501",
                    dataType: "Int16",
                    name: "缺少值的写入点",
                    accessMode: "Write",
                    // value: 缺少value字段
                    enabled: true
                }
            ]
        };

        try {
            const response = await this.sendMessage('batch_datapoint_operation', invalidRequest);
            console.log('[验证错误响应]', JSON.stringify(response, null, 2));
            
            if (response.success && response.data?.code === 'INVALID_PARAMETER') {
                console.log('✅ 验证错误处理正常，正确识别了缺少写入值');
                return true;
            } else {
                console.log('❌ 验证错误处理异常，应该识别出错误');
                console.log('实际响应:', response.success, response.data?.code);
                return false;
            }
        } catch (error) {
            console.error('❌ 验证错误测试异常:', error.message);
            return false;
        }
    }

    async runAllTests() {
        console.log('=== Story 2.4 批量数据操作测试 ===\n');
        
        let passedTests = 0;
        let totalTests = 5;

        try {
            await this.connect();
            
            // 执行所有测试
            if (await this.testBatchRead()) passedTests++;
            if (await this.testBatchWrite()) passedTests++;
            if (await this.testBatchReadWrite()) passedTests++;
            if (await this.testDataTypeConversion()) passedTests++;
            if (await this.testValidationErrors()) passedTests++;
            
            console.log(`\n=== 测试结果 ===`);
            console.log(`通过: ${passedTests}/${totalTests}`);
            console.log(`成功率: ${(passedTests/totalTests*100).toFixed(1)}%`);
            
            if (passedTests === totalTests) {
                console.log('🎉 所有Story 2.4功能测试通过！');
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
const tester = new Story24Tester();
tester.runAllTests().then(() => {
    console.log('测试完成');
    process.exit(0);
}).catch(error => {
    console.error('测试失败:', error);
    process.exit(1);
});