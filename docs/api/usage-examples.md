# HLS通信服务 API使用示例

## 版本: 1.0
## 最后更新: 2025-08-26

---

## 概述

本文档提供HLS-Communication服务API的实际使用示例，包括常见场景的完整代码示例和最佳实践。

---

## 基础使用场景

### 1. 简单的设备连接和数据读取

这个示例演示了如何连接到Modbus TCP设备并读取一个温度值。

#### Node.js示例

```javascript
const net = require('net');
const { v4: uuidv4 } = require('uuid');

class HLSClient {
    constructor(host = 'localhost', port = 8888) {
        this.host = host;
        this.port = port;
        this.socket = null;
    }

    async connect() {
        return new Promise((resolve, reject) => {
            this.socket = net.createConnection(this.port, this.host);
            this.socket.on('connect', resolve);
            this.socket.on('error', reject);
        });
    }

    async sendCommand(command, data = {}) {
        const request = {
            version: "1.0",
            messageId: uuidv4(),
            timestamp: new Date().toISOString(),
            command: command,
            data: data
        };

        return new Promise((resolve, reject) => {
            this.socket.write(JSON.stringify(request) + '\n');
            
            this.socket.once('data', (response) => {
                try {
                    const result = JSON.parse(response.toString());
                    if (result.success) {
                        resolve(result);
                    } else {
                        reject(new Error(result.error.message));
                    }
                } catch (error) {
                    reject(error);
                }
            });
        });
    }

    disconnect() {
        if (this.socket) {
            this.socket.end();
        }
    }
}

async function simpleExample() {
    const client = new HLSClient();
    let connectionId = null;

    try {
        // 1. 连接到HLS服务器
        await client.connect();
        console.log('已连接到HLS服务器');

        // 2. 连接到Modbus设备
        const connectResponse = await client.sendCommand('connect', {
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
                }
            ]
        });

        connectionId = connectResponse.data.connectionId;
        console.log(`设备已连接，连接ID: ${connectionId}`);

        // 3. 读取温度数据
        const readResponse = await client.sendCommand('read', {
            connectionId: connectionId,
            dataPoints: ["40001"]
        });

        const temperature = readResponse.data.results[0];
        console.log(`温度值: ${temperature.value}°C`);

        // 4. 断开连接
        await client.sendCommand('disconnect', {
            connectionId: connectionId
        });
        console.log('设备已断开连接');

    } catch (error) {
        console.error('错误:', error.message);
    } finally {
        client.disconnect();
    }
}

simpleExample();
```

#### Python示例

```python
import socket
import json
import uuid
from datetime import datetime

class HLSClient:
    def __init__(self, host='localhost', port=8888):
        self.host = host
        self.port = port
        self.socket = None

    def connect(self):
        self.socket = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
        self.socket.connect((self.host, self.port))
        print(f"已连接到HLS服务器 {self.host}:{self.port}")

    def send_command(self, command, data=None):
        if data is None:
            data = {}
        
        request = {
            'version': '1.0',
            'messageId': str(uuid.uuid4()),
            'timestamp': datetime.utcnow().isoformat() + 'Z',
            'command': command,
            'data': data
        }

        message = json.dumps(request) + '\n'
        self.socket.send(message.encode('utf-8'))

        response_data = self.socket.recv(4096)
        response = json.loads(response_data.decode('utf-8'))

        if response['success']:
            return response
        else:
            raise Exception(response['error']['message'])

    def disconnect(self):
        if self.socket:
            self.socket.close()

def simple_example():
    client = HLSClient()
    connection_id = None

    try:
        # 1. 连接到HLS服务器
        client.connect()

        # 2. 连接到Modbus设备
        connect_response = client.send_command('connect', {
            'deviceConfig': {
                'type': 'modbus-tcp',
                'host': '192.168.1.100',
                'port': 502,
                'timeout': 5000
            },
            'dataPoints': [
                {
                    'name': 'temperature',
                    'address': '40001',
                    'dataType': 'float',
                    'access': 'read',
                    'description': '温度传感器'
                }
            ]
        })

        connection_id = connect_response['data']['connectionId']
        print(f"设备已连接，连接ID: {connection_id}")

        # 3. 读取温度数据
        read_response = client.send_command('read', {
            'connectionId': connection_id,
            'dataPoints': ['40001']
        })

        temperature = read_response['data']['results'][0]
        print(f"温度值: {temperature['value']}°C")

        # 4. 断开连接
        client.send_command('disconnect', {
            'connectionId': connection_id
        })
        print('设备已断开连接')

    except Exception as e:
        print(f'错误: {e}')
    finally:
        client.disconnect()

if __name__ == '__main__':
    simple_example()
```

---

### 2. 批量数据操作示例

这个示例演示如何同时读取和写入多个数据点。

```javascript
async function batchOperationExample() {
    const client = new HLSClient();
    let connectionId = null;

    try {
        await client.connect();

        // 连接设备
        const connectResponse = await client.sendCommand('connect', {
            deviceConfig: {
                type: "modbus-tcp",
                host: "192.168.1.100",
                port: 502,
                timeout: 5000
            },
            dataPoints: [
                { name: "temp1", address: "40001", dataType: "float", access: "read" },
                { name: "temp2", address: "40002", dataType: "float", access: "read" },
                { name: "setpoint", address: "40010", dataType: "float", access: "write" },
                { name: "mode", address: "40011", dataType: "int16", access: "write" }
            ]
        });

        connectionId = connectResponse.data.connectionId;

        // 批量读取数据
        console.log('\n=== 批量读取数据 ===');
        const readResponse = await client.sendCommand('readBatch', {
            connectionId: connectionId,
            dataPoints: ["40001", "40002", "40010", "40011"],
            options: {
                timeout: 5000,
                includeQuality: true
            }
        });

        readResponse.data.results.forEach(result => {
            console.log(`地址 ${result.address}: ${result.value} (${result.dataType})`);
        });

        console.log(`成功读取: ${readResponse.data.summary.successful}个数据点`);

        // 批量写入数据
        console.log('\n=== 批量写入数据 ===');
        const writeResponse = await client.sendCommand('writeBatch', {
            connectionId: connectionId,
            dataPoints: [
                { address: "40010", value: 25.5, dataType: "float" },
                { address: "40011", value: 1, dataType: "int16" }
            ],
            options: {
                timeout: 5000,
                validateBeforeWrite: true
            }
        });

        writeResponse.data.results.forEach(result => {
            if (result.success) {
                console.log(`✓ 地址 ${result.address}: 写入成功 (${result.value})`);
            } else {
                console.log(`✗ 地址 ${result.address}: 写入失败 - ${result.error.message}`);
            }
        });

        console.log(`成功写入: ${writeResponse.data.summary.successful}个数据点`);

        // 断开连接
        await client.sendCommand('disconnect', { connectionId: connectionId });

    } catch (error) {
        console.error('批量操作失败:', error.message);
    } finally {
        client.disconnect();
    }
}
```

---

### 3. 错误处理和重试机制

这个示例展示如何处理各种错误情况和实现重试机制。

```javascript
class RobustHLSClient extends HLSClient {
    constructor(host, port, options = {}) {
        super(host, port);
        this.maxRetries = options.maxRetries || 3;
        this.retryDelay = options.retryDelay || 1000;
    }

    async sendCommandWithRetry(command, data = {}, retryCount = 0) {
        try {
            return await this.sendCommand(command, data);
        } catch (error) {
            console.log(`命令执行失败: ${error.message}`);
            
            // 检查是否是可重试的错误
            if (this.isRetryableError(error) && retryCount < this.maxRetries) {
                console.log(`等待 ${this.retryDelay}ms 后重试... (${retryCount + 1}/${this.maxRetries})`);
                
                await this.sleep(this.retryDelay);
                return this.sendCommandWithRetry(command, data, retryCount + 1);
            }
            
            throw error;
        }
    }

    isRetryableError(error) {
        // 检查错误是否可重试
        const retryableCodes = ['2003', '2004', '3003', '3004', '1008'];
        return retryableCodes.some(code => error.message.includes(code));
    }

    sleep(ms) {
        return new Promise(resolve => setTimeout(resolve, ms));
    }
}

async function errorHandlingExample() {
    const client = new RobustHLSClient('localhost', 8888, {
        maxRetries: 3,
        retryDelay: 2000
    });

    try {
        await client.connect();

        // 尝试连接一个可能不存在的设备
        console.log('尝试连接到设备...');
        const connectResponse = await client.sendCommandWithRetry('connect', {
            deviceConfig: {
                type: "modbus-tcp",
                host: "192.168.1.999", // 可能无法访问的IP
                port: 502,
                timeout: 3000
            }
        });

        console.log('设备连接成功!');

    } catch (error) {
        console.error('最终连接失败:', error.message);
        
        // 分析错误类型
        if (error.message.includes('2008')) {
            console.log('建议: 检查网络连接和设备IP地址');
        } else if (error.message.includes('2003')) {
            console.log('建议: 增加连接超时时间');
        } else if (error.message.includes('2009')) {
            console.log('建议: 检查设备端口和防火墙设置');
        }
    } finally {
        client.disconnect();
    }
}
```

---

### 4. 连接状态监控

这个示例展示如何监控连接状态和实现心跳检查。

```javascript
class MonitoringClient extends HLSClient {
    constructor(host, port) {
        super(host, port);
        this.connections = new Map(); // 存储连接信息
        this.monitoringInterval = null;
    }

    async startMonitoring(intervalMs = 10000) {
        console.log(`开始监控连接状态 (间隔: ${intervalMs}ms)`);
        
        this.monitoringInterval = setInterval(async () => {
            await this.checkAllConnections();
        }, intervalMs);
    }

    stopMonitoring() {
        if (this.monitoringInterval) {
            clearInterval(this.monitoringInterval);
            this.monitoringInterval = null;
            console.log('已停止连接监控');
        }
    }

    async checkAllConnections() {
        try {
            // 获取所有连接列表
            const listResponse = await this.sendCommand('listConnections');
            const connections = listResponse.data.connections;

            console.log(`\n=== 连接状态检查 (${new Date().toLocaleString()}) ===`);
            console.log(`活动连接数: ${connections.length}/${listResponse.data.maxConnections}`);

            for (const conn of connections) {
                try {
                    // 检查每个连接的详细状态
                    const statusResponse = await this.sendCommand('status', {
                        connectionId: conn.connectionId
                    });

                    const status = statusResponse.data;
                    console.log(`📱 ${conn.connectionId.substring(0, 20)}...`);
                    console.log(`   状态: ${status.status} | 设备: ${conn.host}:${conn.port}`);
                    console.log(`   响应时间: ${status.connectionHealth.responseTime.toFixed(1)}ms`);
                    
                    // 更新本地连接信息
                    this.connections.set(conn.connectionId, {
                        ...conn,
                        lastCheck: new Date(),
                        health: status.connectionHealth
                    });

                } catch (error) {
                    console.log(`❌ ${conn.connectionId.substring(0, 20)}... - 检查失败: ${error.message}`);
                }
            }

            // 显示服务器状态
            const serverResponse = await this.sendCommand('serverStatus');
            const server = serverResponse.data;
            console.log(`\n🖥️  服务器状态:`);
            console.log(`   运行时间: ${server.uptime} | 内存使用: ${server.memoryUsageMB.toFixed(1)}MB`);
            console.log(`   已处理消息: ${server.messagesProcessed}`);

        } catch (error) {
            console.error('连接监控失败:', error.message);
        }
    }

    async createMonitoredConnection(deviceConfig) {
        const connectResponse = await this.sendCommand('connect', { deviceConfig });
        const connectionId = connectResponse.data.connectionId;
        
        console.log(`✅ 创建监控连接: ${connectionId}`);
        return connectionId;
    }
}

async function monitoringExample() {
    const client = new MonitoringClient();

    try {
        await client.connect();

        // 创建几个测试连接
        const conn1 = await client.createMonitoredConnection({
            type: "modbus-tcp",
            host: "192.168.1.100",
            port: 502
        });

        const conn2 = await client.createMonitoredConnection({
            type: "modbus-tcp", 
            host: "192.168.1.101",
            port: 502
        });

        // 开始监控 (每5秒检查一次)
        await client.startMonitoring(5000);

        // 运行30秒后停止监控
        setTimeout(async () => {
            client.stopMonitoring();
            
            // 清理连接
            await client.sendCommand('disconnect', { connectionId: conn1 });
            await client.sendCommand('disconnect', { connectionId: conn2 });
            
            console.log('\n监控示例完成');
            client.disconnect();
        }, 30000);

    } catch (error) {
        console.error('监控示例失败:', error.message);
        client.disconnect();
    }
}
```

---

### 5. Node-RED集成示例

这个示例展示如何在Node-RED中使用HLS API。

#### Node-RED自定义节点代码

```javascript
// hls-connector.js
module.exports = function(RED) {
    const net = require('net');
    const { v4: uuidv4 } = require('uuid');

    function HLSConnectorNode(config) {
        RED.nodes.createNode(this, config);
        const node = this;
        
        node.host = config.host || 'localhost';
        node.port = config.port || 8888;
        node.socket = null;
        node.connectionId = null;

        // 连接到HLS服务器
        function connectToHLS() {
            node.socket = net.createConnection(node.port, node.host);
            
            node.socket.on('connect', () => {
                node.status({fill: "green", shape: "dot", text: "已连接"});
                node.log('已连接到HLS服务器');
            });

            node.socket.on('error', (err) => {
                node.status({fill: "red", shape: "ring", text: "连接错误"});
                node.error('HLS连接错误: ' + err.message);
            });

            node.socket.on('close', () => {
                node.status({fill: "red", shape: "ring", text: "连接断开"});
            });
        }

        // 发送HLS命令
        async function sendHLSCommand(command, data = {}) {
            return new Promise((resolve, reject) => {
                const request = {
                    version: "1.0",
                    messageId: uuidv4(),
                    timestamp: new Date().toISOString(),
                    command: command,
                    data: data
                };

                const timeout = setTimeout(() => {
                    reject(new Error('命令超时'));
                }, 10000);

                node.socket.once('data', (response) => {
                    clearTimeout(timeout);
                    try {
                        const result = JSON.parse(response.toString());
                        if (result.success) {
                            resolve(result);
                        } else {
                            reject(new Error(result.error.message));
                        }
                    } catch (error) {
                        reject(error);
                    }
                });

                node.socket.write(JSON.stringify(request) + '\n');
            });
        }

        // 处理输入消息
        node.on('input', async function(msg) {
            try {
                const command = msg.payload.command;
                const data = msg.payload.data || {};

                // 根据命令类型处理
                switch (command) {
                    case 'connect':
                        const connectResponse = await sendHLSCommand('connect', data);
                        node.connectionId = connectResponse.data.connectionId;
                        msg.payload = {
                            success: true,
                            connectionId: node.connectionId,
                            data: connectResponse.data
                        };
                        break;

                    case 'read':
                        if (!node.connectionId) {
                            throw new Error('设备未连接');
                        }
                        data.connectionId = node.connectionId;
                        const readResponse = await sendHLSCommand('read', data);
                        msg.payload = {
                            success: true,
                            results: readResponse.data.results
                        };
                        break;

                    case 'write':
                        if (!node.connectionId) {
                            throw new Error('设备未连接');
                        }
                        data.connectionId = node.connectionId;
                        const writeResponse = await sendHLSCommand('write', data);
                        msg.payload = {
                            success: true,
                            results: writeResponse.data.results
                        };
                        break;

                    default:
                        const response = await sendHLSCommand(command, data);
                        msg.payload = response.data;
                }

                node.send(msg);

            } catch (error) {
                msg.payload = {
                    success: false,
                    error: error.message
                };
                node.error(error.message, msg);
                node.send(msg);
            }
        });

        // 初始化连接
        connectToHLS();

        // 节点关闭时清理
        node.on('close', async function() {
            if (node.connectionId) {
                try {
                    await sendHLSCommand('disconnect', { connectionId: node.connectionId });
                } catch (error) {
                    // 忽略断开连接时的错误
                }
            }
            if (node.socket) {
                node.socket.destroy();
            }
        });
    }

    RED.nodes.registerType("hls-connector", HLSConnectorNode);
}
```

#### Node-RED流程配置示例

```json
[
    {
        "id": "inject1",
        "type": "inject",
        "name": "连接设备",
        "payload": {
            "command": "connect",
            "data": {
                "deviceConfig": {
                    "type": "modbus-tcp",
                    "host": "192.168.1.100",
                    "port": 502
                },
                "dataPoints": [
                    {
                        "name": "temperature",
                        "address": "40001",
                        "dataType": "float",
                        "access": "read"
                    }
                ]
            }
        },
        "payloadType": "json",
        "repeat": "",
        "crontab": "",
        "once": false,
        "x": 120,
        "y": 100
    },
    {
        "id": "inject2", 
        "type": "inject",
        "name": "读取数据",
        "payload": {
            "command": "read",
            "data": {
                "dataPoints": ["40001"]
            }
        },
        "payloadType": "json",
        "repeat": "10",
        "crontab": "",
        "once": false,
        "x": 120,
        "y": 160
    },
    {
        "id": "hls1",
        "type": "hls-connector",
        "name": "HLS连接器",
        "host": "localhost",
        "port": "8888",
        "x": 300,
        "y": 130
    },
    {
        "id": "debug1",
        "type": "debug",
        "name": "输出",
        "active": true,
        "console": false,
        "complete": "payload",
        "x": 480,
        "y": 130
    }
]
```

---

## 最佳实践

### 1. 连接管理

```javascript
// ✅ 好的做法：复用连接
const connectionPool = new Map();

async function getOrCreateConnection(deviceConfig) {
    const key = `${deviceConfig.host}:${deviceConfig.port}`;
    
    if (connectionPool.has(key)) {
        const conn = connectionPool.get(key);
        // 检查连接是否仍然有效
        try {
            await client.sendCommand('status', { connectionId: conn.id });
            return conn;
        } catch (error) {
            // 连接失效，移除并重新创建
            connectionPool.delete(key);
        }
    }

    // 创建新连接
    const response = await client.sendCommand('connect', { deviceConfig });
    const connection = {
        id: response.data.connectionId,
        config: deviceConfig,
        createdAt: new Date()
    };
    
    connectionPool.set(key, connection);
    return connection;
}
```

### 2. 错误处理

```javascript
// ✅ 好的做法：详细的错误分类处理
function handleHLSError(error) {
    const errorCode = error.message.match(/\d{4}/)?.[0];
    
    switch (errorCode) {
        case '2003': // 连接超时
        case '2004': // 连接失败
            return { 
                retry: true, 
                delay: 5000, 
                action: '检查设备网络连接' 
            };
            
        case '3001': // 地址无效
        case '3002': // 数据类型错误
            return { 
                retry: false, 
                action: '检查数据点配置' 
            };
            
        case '1007': // 资源耗尽
            return { 
                retry: true, 
                delay: 10000, 
                action: '等待系统资源释放' 
            };
            
        default:
            return { 
                retry: false, 
                action: '联系技术支持' 
            };
    }
}
```

### 3. 性能优化

```javascript
// ✅ 好的做法：批量操作
async function optimizedDataCollection(connectionId, addresses) {
    // 分批处理大量数据点
    const BATCH_SIZE = 100;
    const batches = [];
    
    for (let i = 0; i < addresses.length; i += BATCH_SIZE) {
        batches.push(addresses.slice(i, i + BATCH_SIZE));
    }
    
    const results = [];
    for (const batch of batches) {
        const response = await client.sendCommand('readBatch', {
            connectionId: connectionId,
            dataPoints: batch
        });
        results.push(...response.data.results);
    }
    
    return results;
}
```

---

## 常见问题解答

### Q: 如何处理网络断开重连？

A: 实现自动重连机制：

```javascript
class AutoReconnectClient extends HLSClient {
    constructor(host, port) {
        super(host, port);
        this.reconnectInterval = 5000;
        this.maxReconnectAttempts = 10;
        this.reconnectCount = 0;
    }

    async connectWithRetry() {
        while (this.reconnectCount < this.maxReconnectAttempts) {
            try {
                await this.connect();
                this.reconnectCount = 0;
                return;
            } catch (error) {
                this.reconnectCount++;
                console.log(`重连失败 (${this.reconnectCount}/${this.maxReconnectAttempts})`);
                
                if (this.reconnectCount < this.maxReconnectAttempts) {
                    await this.sleep(this.reconnectInterval);
                }
            }
        }
        throw new Error('达到最大重连次数');
    }
}
```

### Q: 如何优化大量数据点的读取性能？

A: 使用并发批量读取：

```javascript
async function parallelBatchRead(connectionId, dataPoints, concurrency = 3) {
    const chunks = chunkArray(dataPoints, 50); // 每批50个数据点
    const results = [];

    // 并发处理多个批次
    for (let i = 0; i < chunks.length; i += concurrency) {
        const batch = chunks.slice(i, i + concurrency);
        const promises = batch.map(chunk => 
            client.sendCommand('readBatch', {
                connectionId: connectionId,
                dataPoints: chunk
            })
        );

        const responses = await Promise.all(promises);
        responses.forEach(response => {
            results.push(...response.data.results);
        });
    }

    return results;
}
```

---

*API使用示例文档版本: 1.0，最后更新: 2025-08-26*