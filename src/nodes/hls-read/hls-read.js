/**
 * HLS-Read Node
 * Node-RED节点，用于从工业设备读取数据
 */

const net = require('net');
const { v4: uuidv4 } = require('uuid');

module.exports = function (RED) {
  'use strict';

  /**
   * HLS IPC客户端类
   */
  class HLSIPCClient {
    constructor(options) {
      this.host = options.host || 'localhost';
      this.port = options.port || 8888;
      this.timeout = options.timeout || 5000;
      this.socket = null;
      this.connected = false;
      this.pendingRequests = new Map();
    }

    /**
     * 连接到HLS服务
     */
    async connect() {
      return new Promise((resolve, reject) => {
        if (this.connected) {
          return resolve();
        }

        this.socket = new net.Socket();
        this.socket.setTimeout(this.timeout);

        this.socket.on('connect', () => {
          this.connected = true;
          resolve();
        });

        this.socket.on('data', (data) => {
          this.handleResponse(data);
        });

        this.socket.on('error', (err) => {
          this.connected = false;
          reject(err);
        });

        this.socket.on('close', () => {
          this.connected = false;
        });

        this.socket.on('timeout', () => {
          this.socket.destroy();
          reject(new Error('连接超时'));
        });

        this.socket.connect(this.port, this.host);
      });
    }

    /**
     * 处理服务器响应
     */
    handleResponse(data) {
      try {
        const response = JSON.parse(data.toString());
        const messageId = response.messageId;
        
        if (this.pendingRequests.has(messageId)) {
          const { resolve, reject } = this.pendingRequests.get(messageId);
          this.pendingRequests.delete(messageId);
          
          if (response.success) {
            resolve(response);
          } else {
            reject(new Error(response.error ? response.error.message : '未知错误'));
          }
        }
      } catch (err) {
        // 忽略解析错误
      }
    }

    /**
     * 发送请求到HLS服务
     */
    async sendRequest(command, data) {
      if (!this.connected) {
        await this.connect();
      }

      return new Promise((resolve, reject) => {
        const messageId = uuidv4();
        const request = {
          version: '1.0',
          messageId: messageId,
          timestamp: new Date().toISOString(),
          command: command,
          data: data
        };

        this.pendingRequests.set(messageId, { resolve, reject });

        // 设置请求超时
        setTimeout(() => {
          if (this.pendingRequests.has(messageId)) {
            this.pendingRequests.delete(messageId);
            reject(new Error('请求超时'));
          }
        }, this.timeout);

        this.socket.write(JSON.stringify(request));
      });
    }

    /**
     * 连接设备
     */
    async connectDevice(deviceConfig, dataPoints) {
      const data = {
        deviceConfig: deviceConfig,
        dataPoints: dataPoints
      };
      return await this.sendRequest('connect', data);
    }

    /**
     * 读取单个数据点
     */
    async readData(connectionId, address) {
      const data = {
        connectionId: connectionId,
        address: address
      };
      return await this.sendRequest('read', data);
    }

    /**
     * 批量读取数据点
     */
    async readBatchData(connectionId, addresses) {
      const data = {
        connectionId: connectionId,
        addresses: addresses
      };
      return await this.sendRequest('readBatch', data);
    }

    /**
     * 断开连接
     */
    disconnect() {
      if (this.socket) {
        this.socket.destroy();
      }
      this.connected = false;
      this.pendingRequests.clear();
    }
  }

  function HLSReadNode(config) {
    RED.nodes.createNode(this, config);

    const node = this;

    // 节点配置
    node.name = config.name || 'HLS读取';
    node.deviceId = config.deviceId || '';
    node.addresses = config.addresses || [];
    node.interval = parseInt(config.interval) || 1000;
    node.server = config.server || 'localhost';
    node.port = parseInt(config.port) || 8888;
    node.protocol = config.protocol || 'ModbusTcp';
    node.devicePort = parseInt(config.devicePort) || 502;
    node.timeout = parseInt(config.timeout) || 5000;

    // 连接状态和客户端
    node.connected = false;
    node.connectionId = null;
    node.hlsClient = new HLSIPCClient({
      host: node.server,
      port: node.port,
      timeout: 5000
    });

    // 定时读取器
    node.readInterval = null;

    // 状态指示
    node.status({ fill: 'red', shape: 'ring', text: '未连接' });

    /**
     * 初始化设备连接
     */
    async function initializeConnection() {
      try {
        node.status({ fill: 'yellow', shape: 'ring', text: '连接中...' });

        // 构造设备配置
        const deviceConfig = {
          protocol: node.protocol,
          host: node.deviceId,
          port: node.devicePort,
          timeout: node.timeout
        };

        // 构造数据点配置
        const dataPoints = node.addresses.map(addr => ({
          address: addr.address,
          dataType: addr.dataType || 'Int16',
          name: addr.description || addr.address
        }));

        // 连接设备
        const response = await node.hlsClient.connectDevice(deviceConfig, dataPoints);
        
        if (response.success && response.data && response.data.connectionId) {
          node.connectionId = response.data.connectionId;
          node.connected = true;
          node.status({ fill: 'green', shape: 'dot', text: '已连接' });
          node.log(`成功连接到设备: ${node.deviceId}`);
          
          // 开始定时读取
          startPeriodicReading();
        } else {
          throw new Error('设备连接失败');
        }
      } catch (err) {
        node.connected = false;
        node.status({ fill: 'red', shape: 'ring', text: `连接失败: ${err.message}` });
        node.error(`设备连接失败: ${err.message}`);
      }
    }

    /**
     * 开始定时读取数据
     */
    function startPeriodicReading() {
      if (node.readInterval) {
        clearInterval(node.readInterval);
      }

      if (node.interval > 0 && node.addresses.length > 0) {
        node.readInterval = setInterval(async () => {
          await performDataReading();
        }, node.interval);
      }
    }

    /**
     * 执行数据读取
     */
    async function performDataReading() {
      if (!node.connected || !node.connectionId) {
        return;
      }

      try {
        node.status({ fill: 'blue', shape: 'dot', text: '读取中...' });

        const addresses = node.addresses.map(addr => addr.address);
        let response;

        if (addresses.length === 1) {
          // 单点读取
          response = await node.hlsClient.readData(node.connectionId, addresses[0]);
        } else {
          // 批量读取
          response = await node.hlsClient.readBatchData(node.connectionId, addresses);
        }

        if (response.success && response.data) {
          // 构造输出消息
          const outputData = Array.isArray(response.data) ? response.data : [response.data];
          
          const msg = {
            payload: {
              connectionId: node.connectionId,
              data: outputData.map(item => ({
                address: item.address,
                value: item.value,
                dataType: item.dataType,
                timestamp: response.timestamp || new Date().toISOString(),
                quality: 'Good'
              })),
              timestamp: response.timestamp || new Date().toISOString(),
              status: 'success'
            },
            topic: 'hls-read'
          };

          node.send(msg);
          node.status({ fill: 'green', shape: 'dot', text: `已读取 ${outputData.length} 个点位` });
        } else {
          throw new Error('数据读取失败');
        }
      } catch (err) {
        node.status({ fill: 'red', shape: 'ring', text: `读取失败: ${err.message}` });
        node.error(`数据读取失败: ${err.message}`);
        
        // 尝试重新连接
        node.connected = false;
        setTimeout(() => {
          initializeConnection();
        }, 5000);
      }
    }

    // 节点输入处理（手动触发读取）
    node.on('input', async (msg, send, done) => {
      // 兼容Node-RED 0.x
      send = send || function () { node.send.apply(node, arguments); };
      done = done || function (err) { if (err) { node.error(err, msg); } };

      try {
        if (!node.connected) {
          await initializeConnection();
          if (!node.connected) {
            done(new Error('设备未连接'));
            return;
          }
        }

        // 执行一次数据读取
        await performDataReading();
        done();
      } catch (err) {
        node.error(`手动读取失败: ${err.message}`, msg);
        done(err);
      }
    });

    // 节点关闭时清理
    node.on('close', (removed, done) => {
      if (node.readInterval) {
        clearInterval(node.readInterval);
      }
      
      if (node.hlsClient) {
        node.hlsClient.disconnect();
      }
      
      node.log('HLS-Read节点已关闭');
      done();
    });

    // 初始化连接（如果配置了设备ID和地址）
    if (node.deviceId && node.addresses.length > 0) {
      setTimeout(() => {
        initializeConnection();
      }, 1000); // 延迟1秒初始化，确保Node-RED完全启动
    }

    node.log('HLS-Read节点已初始化');
  }

  // 注册节点
  RED.nodes.registerType('hls-read', HLSReadNode);
};
