const net = require('net');

// 测试Modbus TCP设备配置
const modbusTestConfig = {
    "messageId": "test_modbus_001",
    "command": "connect_device",
    "data": {
        "deviceId": "modbus_test_001",
        "type": 0, // ModbusTcp
        "name": "测试Modbus设备",
        "description": "用于集成测试的Modbus TCP设备",
        "connection": {
            "host": "127.0.0.1",
            "port": 502,
            "timeoutMs": 5000,
            "station": 1
        }
    }
};

// 先测试ping命令
const pingConfig = {
    "messageId": "ping_001",
    "command": "ping"
};

function connectToHlsService() {
    const client = new net.Socket();
    
    client.connect(8888, 'localhost', () => {
        console.log('✅ 连接到HLS服务成功');
        
        // 测试连接不同协议的设备
        
        // 先测试ping
        setTimeout(() => {
            console.log('🔄 测试Ping命令...');
            client.write(JSON.stringify(pingConfig) + '\n');
        }, 1000);
        
        // 先添加设备
        setTimeout(() => {
            const addDeviceConfig = {
                "messageId": "add_device_001",
                "command": "add_device",
                "data": {
                    "deviceId": "modbus_test_001",
                    "type": 0, // ModbusTcp
                    "name": "测试Modbus设备",
                    "description": "用于集成测试的Modbus TCP设备",
                    "connection": {
                        "host": "127.0.0.1",
                        "port": 502,
                        "timeoutMs": 5000,
                        "station": 1
                    }
                }
            };
            console.log('🔄 添加Modbus TCP设备...');
            client.write(JSON.stringify(addDeviceConfig) + '\n');
        }, 2000);
        
        // 然后连接设备
        setTimeout(() => {
            console.log('🔄 连接Modbus TCP设备...');
            client.write(JSON.stringify(modbusTestConfig) + '\n');
        }, 4000);
        
        // 测试读取数据
        setTimeout(() => {
            const readRequest = {
                "messageId": "test_read_001",
                "command": "read_data",
                "data": {
                    "deviceId": "modbus_test_001",
                    "addresses": ["40001", "40002"]
                }
            };
            console.log('🔄 测试数据读取...');
            client.write(JSON.stringify(readRequest) + '\n');
        }, 6000);
        
        // 测试写入数据
        setTimeout(() => {
            const writeRequest = {
                "messageId": "test_write_001",
                "command": "write_data", 
                "data": {
                    "deviceId": "modbus_test_001",
                    "dataPoints": [
                        {"address": "40001", "value": 1234, "dataType": "int16"}
                    ]
                }
            };
            console.log('🔄 测试数据写入...');
            client.write(JSON.stringify(writeRequest) + '\n');
        }, 8000);
        
        // 关闭连接
        setTimeout(() => {
            console.log('👋 关闭连接');
            client.destroy();
        }, 10000);
    });
    
    client.on('data', (data) => {
        const response = data.toString();
        console.log('📨 收到响应:', response);
    });
    
    client.on('error', (err) => {
        console.error('❌ 连接错误:', err.message);
    });
    
    client.on('close', () => {
        console.log('📪 连接已关闭');
    });
}

// 启动测试
console.log('=== HLS Communication 框架集成测试 ===');
console.log('🚀 开始测试 hlscommunication 框架集成...');
console.log('📡 尝试连接到 HLS 服务 (localhost:8888)...');

connectToHlsService();