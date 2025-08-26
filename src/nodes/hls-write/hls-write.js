/**
 * HLS-Write Node
 * Node-RED节点，用于向工业设备写入数据
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
     * 写入单个数据点
     */
    async writeData(connectionId, address, value, dataType) {
      const data = {
        connectionId: connectionId,
        address: address,
        value: value,
        dataType: dataType
      };
      return await this.sendRequest('write', data);
    }

    /**
     * 批量写入数据点
     */
    async writeBatchData(connectionId, writeItems) {
      const data = {
        connectionId: connectionId,
        writeItems: writeItems
      };
      return await this.sendRequest('writeBatch', data);
    }

    /**
     * 写入后回读验证
     */
    async writeWithVerify(connectionId, address, value, dataType) {
      const data = {
        connectionId: connectionId,
        address: address,
        value: value,
        dataType: dataType,
        verify: true
      };
      return await this.sendRequest('writeWithVerify', data);
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

  function HLSWriteNode(config) {
    RED.nodes.createNode(this, config);

    const node = this;

    // 节点配置
    node.name = config.name || 'HLS写入';
    node.deviceId = config.deviceId || '';
    node.addresses = config.addresses || [];
    node.server = config.server || 'localhost';
    node.port = parseInt(config.port) || 8888;
    node.protocol = config.protocol || 'ModbusTcp';
    node.devicePort = parseInt(config.devicePort) || 502;
    node.timeout = parseInt(config.timeout) || 5000;
    node.writeMode = config.writeMode || 'message'; // 'message' | 'config'
    node.verifyWrites = config.verifyWrites || false;

    // 连接状态和客户端
    node.connected = false;
    node.connectionId = null;
    node.hlsClient = new HLSIPCClient({
      host: node.server,
      port: node.port,
      timeout: 5000
    });

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

        // 构造数据点配置（写入节点需要包含可写入的点位）
        const dataPoints = node.addresses.map(addr => ({
          address: addr.address,
          dataType: addr.dataType || 'Int16',
          name: addr.description || addr.address,
          writable: true
        }));

        // 连接设备
        const response = await node.hlsClient.connectDevice(deviceConfig, dataPoints);
        
        if (response.success && response.data && response.data.connectionId) {
          node.connectionId = response.data.connectionId;
          node.connected = true;
          node.status({ fill: 'green', shape: 'dot', text: '已连接' });
          node.log(`成功连接到设备: ${node.deviceId}`);
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
     * 数据类型转换
     */
    function convertValue(value, dataType) {
      try {
        switch (dataType.toLowerCase()) {
          case 'bool':
          case 'boolean':
            if (typeof value === 'string') {
              return value.toLowerCase() === 'true';
            }
            return Boolean(value);
          case 'int16':
          case 'int32':
          case 'int64':
          case 'uint16':
          case 'uint32':
          case 'uint64':
            return parseInt(value);
          case 'float':
          case 'double':
            return parseFloat(value);
          case 'string':
            return String(value);
          default:
            return value;
        }
      } catch (err) {
        throw new Error(`数据类型转换失败: ${err.message}`);
      }
    }

    /**
     * 验证输入消息格式
     */
    function validateInputMessage(msg) {
      if (!msg.payload) {
        throw new Error('消息payload不能为空');
      }

      // 支持三种消息格式：
      // 1. 单点写入: { address: '40001', value: 100, dataType: 'Int16' }
      // 2. 批量写入: [{ address: '40001', value: 100, dataType: 'Int16' }, ...]
      // 3. 地址映射: { '40001': 100, '40002': 25.6 }
      
      let writeItems = [];
      const payload = msg.payload;
      
      if (Array.isArray(payload)) {
        // 批量写入格式
        writeItems = payload.map(item => {
          if (!item.address) {
            throw new Error('批量写入项缺少address字段');
          }
          return {
            address: item.address,
            value: item.value,
            dataType: item.dataType || 'Int16'
          };
        });
      } else if (payload.address !== undefined) {
        // 单点写入格式
        writeItems = [{
          address: payload.address,
          value: payload.value,
          dataType: payload.dataType || 'Int16'
        }];
      } else {
        // 地址映射格式，需要从配置中获取数据类型
        writeItems = Object.keys(payload).map(address => {
          const configItem = node.addresses.find(addr => addr.address === address);
          const dataType = configItem ? configItem.dataType : 'Int16';
          return {
            address: address,
            value: payload[address],
            dataType: dataType
          };
        });
      }

      return writeItems;
    }

    /**
     * 执行数据写入
     */
    async function performDataWriting(writeItems) {
      if (!node.connected || !node.connectionId) {
        throw new Error('设备未连接');
      }

      node.status({ fill: 'blue', shape: 'dot', text: '写入中...' });

      try {
        let response;
        const processedItems = writeItems.map(item => ({
          address: item.address,
          value: convertValue(item.value, item.dataType),
          dataType: item.dataType
        }));

        if (processedItems.length === 1) {
          // 单点写入
          const item = processedItems[0];
          if (node.verifyWrites) {
            response = await node.hlsClient.writeWithVerify(
              node.connectionId, 
              item.address, 
              item.value, 
              item.dataType
            );
          } else {
            response = await node.hlsClient.writeData(
              node.connectionId, 
              item.address, 
              item.value, 
              item.dataType
            );
          }
        } else {
          // 批量写入
          response = await node.hlsClient.writeBatchData(node.connectionId, processedItems);
        }

        if (response.success) {
          const resultData = Array.isArray(response.data) ? response.data : [response.data];
          
          node.status({ fill: 'green', shape: 'dot', text: `写入成功 ${processedItems.length} 个点位` });
          
          return {
            success: true,
            connectionId: node.connectionId,
            results: resultData.map(item => ({
              address: item.address,
              value: item.value,
              dataType: item.dataType,
              status: item.status || 'success',
              verified: item.verified || false,
              timestamp: response.timestamp || new Date().toISOString()
            })),
            timestamp: response.timestamp || new Date().toISOString(),
            operation: 'write'
          };
        } else {
          throw new Error(response.error ? response.error.message : '写入失败');
        }
      } catch (err) {
        node.status({ fill: 'red', shape: 'ring', text: `写入失败: ${err.message}` });
        throw err;
      }
    }

    // 节点输入处理
    node.on('input', async (msg, send, done) => {
      // 兼容Node-RED 0.x
      send = send || function () { node.send.apply(node, arguments); };
      done = done || function (err) { if (err) { node.error(err, msg); } };

      try {
        // 确保设备连接
        if (!node.connected) {
          await initializeConnection();
          if (!node.connected) {
            done(new Error('设备连接失败'));
            return;
          }
        }

        // 验证和解析输入消息
        const writeItems = validateInputMessage(msg);
        
        if (writeItems.length === 0) {
          done(new Error('没有有效的写入数据'));
          return;
        }

        // 执行写入操作
        const result = await performDataWriting(writeItems);

        // 构造输出消息
        const outputMsg = {
          payload: result,
          topic: msg.topic || 'hls-write',
          originalPayload: msg.payload
        };

        // 成功输出端口
        send([outputMsg, null]);
        done();
        
      } catch (err) {
        node.error(`写入操作失败: ${err.message}`, msg);
        
        // 错误输出端口
        const errorMsg = {
          payload: {
            success: false,
            error: {
              message: err.message,
              code: err.code || 'WRITE_ERROR'
            },
            timestamp: new Date().toISOString(),
            operation: 'write'
          },
          topic: msg.topic || 'hls-write-error',
          originalPayload: msg.payload
        };
        
        send([null, errorMsg]);
        done(err);
        
        // 连接失败时尝试重连
        if (err.message.includes('连接') || err.message.includes('超时')) {
          node.connected = false;
          setTimeout(() => {
            initializeConnection();
          }, 5000);
        }
      }
    });

    // 节点关闭时清理
    node.on('close', (removed, done) => {
      if (node.hlsClient) {
        node.hlsClient.disconnect();
      }
      
      node.log('HLS-Write节点已关闭');
      done();
    });

    // 延迟初始化连接（如果配置了设备ID）
    if (node.deviceId && (node.addresses.length > 0 || node.writeMode === 'message')) {
      setTimeout(() => {
        initializeConnection();
      }, 1000);
    }

    node.log('HLS-Write节点已初始化');
  }

  // 注册节点
  RED.nodes.registerType('hls-write', HLSWriteNode);
};
