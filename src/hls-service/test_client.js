const net = require('net');

// 测试设备管理功能
class DeviceTestClient {
    constructor() {
        this.host = '127.0.0.1';
        this.port = 8888;
    }

    // 发送IPC命令
    async sendCommand(command, data = null) {
        return new Promise((resolve, reject) => {
            const client = new net.Socket();
            let responseData = '';

            client.connect(this.port, this.host, () => {
                const request = {
                    messageId: Date.now().toString(),
                    version: '1.0',
                    command: command,
                    data: data
                };

                const message = JSON.stringify(request);
                console.log(`📤 发送命令: ${command}`);
                client.write(message);
            });

            client.on('data', (data) => {
                responseData += data.toString();
            });

            client.on('close', () => {
                try {
                    const response = JSON.parse(responseData);
                    console.log(`📥 响应: ${command} - ${response.success ? '成功' : '失败'}`);
                    if (!response.success && response.error) {
                        console.log(`   错误: ${response.error}`);
                    }
                    resolve(response);
                } catch (error) {
                    reject(error);
                }
            });

            client.on('error', (error) => {
                reject(error);
            });
        });
    }

    // 运行所有测试
    async runAllTests() {
        console.log('=== 设备管理功能测试 ===\n');

        try {
            // 1. 测试获取设备列表
            console.log('🔄 测试获取设备列表...');
            await this.sendCommand('device_list');
            console.log();

            // 2. 测试添加Modbus TCP设备
            console.log('🔄 测试添加Modbus TCP设备...');
            const deviceConfig = {
                deviceId: 'modbus_test_001',
                name: '测试Modbus设备',
                description: '用于测试的Modbus TCP设备',
                type: 'ModbusTcp',
                enabled: true,
                connection: {
                    host: '127.0.0.1',
                    port: 502,
                    station: 1,
                    timeoutMs: 5000
                }
            };
            await this.sendCommand('add_device', deviceConfig);
            console.log();

            // 3. 测试获取设备状态
            console.log('🔄 测试获取设备状态...');
            await this.sendCommand('device_status', { deviceId: 'modbus_test_001' });
            console.log();

            // 4. 测试连接设备
            console.log('🔄 测试连接设备...');
            await this.sendCommand('connect_device', { deviceId: 'modbus_test_001' });
            console.log();

            // 5. 测试读取数据
            console.log('🔄 测试读取数据...');
            await this.sendCommand('read_data', {
                deviceId: 'modbus_test_001',
                addresses: ['0', '1', '2']
            });
            console.log();

            // 6. 测试写入数据
            console.log('🔄 测试写入数据...');
            await this.sendCommand('write_data', {
                deviceId: 'modbus_test_001',
                dataPoints: [
                    { address: '0', value: 100 },
                    { address: '1', value: 200 }
                ]
            });
            console.log();

            // 7. 测试断开设备连接
            console.log('🔄 测试断开设备连接...');
            await this.sendCommand('disconnect_device', { deviceId: 'modbus_test_001' });
            console.log();

            // 8. 测试移除设备
            console.log('🔄 测试移除设备...');
            await this.sendCommand('remove_device', { deviceId: 'modbus_test_001' });
            console.log();

            // 9. 测试其他命令
            console.log('🔄 测试其他命令...');
            await this.sendCommand('ping');
            await this.sendCommand('version');
            await this.sendCommand('server_info');
            console.log();

            console.log('✅ 所有设备管理测试完成');

        } catch (error) {
            console.error('❌ 测试过程中出错:', error.message);
        }
    }
}

// 运行测试
const client = new DeviceTestClient();
client.runAllTests().then(() => {
    console.log('\n测试完成，按 Ctrl+C 退出');
}).catch(error => {
    console.error('测试失败:', error);
    process.exit(1);
});