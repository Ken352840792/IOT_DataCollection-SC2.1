const net = require('net');

// 简单测试客户端
function testConnection() {
    const client = new net.Socket();
    let responseData = '';

    client.connect(8888, '127.0.0.1', () => {
        console.log('✅ 连接成功');
        
        const request = {
            messageId: 'test-001',
            version: '1.0',
            command: 'ping',
            data: null
        };

        const message = JSON.stringify(request);
        console.log('📤 发送:', message);
        client.write(message);
        
        // 设置超时，防止无限等待
        setTimeout(() => {
            console.log('⏰ 超时，关闭连接');
            client.destroy();
        }, 5000);
    });

    client.on('data', (data) => {
        responseData += data.toString();
        console.log('📥 接收到数据:', data.toString());
        
        // 检查是否接收到完整的JSON响应
        try {
            const response = JSON.parse(responseData);
            console.log('✅ 解析成功:', response);
            client.end();
        } catch (error) {
            // 数据可能不完整，继续接收
            console.log('⚠️  数据不完整，继续等待...');
        }
    });

    client.on('close', () => {
        console.log('🔌 连接已关闭');
        process.exit(0);
    });

    client.on('error', (error) => {
        console.error('❌ 连接错误:', error.message);
        process.exit(1);
    });
}

console.log('=== 简单连接测试 ===');
testConnection();