const net = require('net');

async function sendCommand(command, data = null) {
    return new Promise((resolve, reject) => {
        const client = new net.Socket();
        let responseData = '';

        const timeout = setTimeout(() => {
            client.destroy();
            reject(new Error('请求超时'));
        }, 5000);

        client.connect(8888, '127.0.0.1', () => {
            const request = {
                messageId: `test-${Date.now()}`,
                version: '1.0',
                command: command,
                data: data
            };

            client.write(JSON.stringify(request));
        });

        client.on('data', (data) => {
            responseData += data.toString();
            
            try {
                const response = JSON.parse(responseData);
                clearTimeout(timeout);
                client.end();
                resolve(response);
            } catch (error) {
                // 数据可能不完整，继续等待
            }
        });

        client.on('error', (error) => {
            clearTimeout(timeout);
            reject(error);
        });
    });
}

async function runDeviceTests() {
    console.log('=== 设备管理功能测试 ===\n');

    try {
        // 1. 获取设备列表
        console.log('🔄 测试获取设备列表...');
        let response = await sendCommand('device_list');
        console.log(`✅ 设备列表: ${response.success ? '成功' : '失败'}`);
        if (response.data && response.data.supportedTypes) {
            console.log(`   支持的设备类型: ${response.data.supportedTypes.join(', ')}`);
        }
        console.log();

        // 2. 添加设备
        console.log('🔄 测试添加Modbus TCP设备...');
        const deviceConfig = {
            deviceId: 'test_modbus_001',
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
        
        response = await sendCommand('add_device', deviceConfig);
        console.log(`✅ 添加设备: ${response.success ? '成功' : '失败'}`);
        if (!response.success) console.log(`   错误: ${response.error}`);
        console.log();

        // 3. 获取设备状态
        console.log('🔄 测试获取设备状态...');
        response = await sendCommand('device_status', { deviceId: 'test_modbus_001' });
        console.log(`✅ 设备状态: ${response.success ? '成功' : '失败'}`);
        if (response.data && response.data.status) {
            console.log(`   状态: ${response.data.status.status}`);
        }
        console.log();

        // 4. 测试连接设备
        console.log('🔄 测试连接设备...');
        response = await sendCommand('connect_device', { deviceId: 'test_modbus_001' });
        console.log(`✅ 连接设备: ${response.success ? '成功' : '失败'}`);
        if (!response.success) console.log(`   错误: ${response.error || '连接失败(正常，因为没有实际的Modbus服务器)'}`);
        console.log();

        // 5. 测试读取数据
        console.log('🔄 测试读取数据...');
        response = await sendCommand('read_data', {
            deviceId: 'test_modbus_001',
            addresses: ['0', '1', '2']
        });
        console.log(`✅ 读取数据: ${response.success ? '成功' : '失败'}`);
        if (response.data && response.data.results) {
            response.data.results.forEach(result => {
                console.log(`   地址 ${result.address}: ${result.success ? '成功' : '失败'}`);
                if (result.error) console.log(`     错误: ${result.error}`);
            });
        }
        console.log();

        // 6. 清理 - 移除设备
        console.log('🔄 测试移除设备...');
        response = await sendCommand('remove_device', { deviceId: 'test_modbus_001' });
        console.log(`✅ 移除设备: ${response.success ? '成功' : '失败'}`);
        console.log();

        console.log('✅ 所有设备管理测试完成!');

    } catch (error) {
        console.error('❌ 测试失败:', error.message);
    }
}

runDeviceTests();