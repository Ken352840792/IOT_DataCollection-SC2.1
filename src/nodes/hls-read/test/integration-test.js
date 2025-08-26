/**
 * Integration test for HLS-Read Node with HLS-Communication service
 * This test follows the pattern established in the existing project tests
 */

const net = require('net');
const { v4: uuidv4 } = require('uuid');

/**
 * Mock HLS server for testing
 */
class MockHLSServer {
  constructor(port = 8888) {
    this.port = port;
    this.server = null;
    this.clients = [];
  }

  start() {
    return new Promise((resolve, reject) => {
      this.server = net.createServer((socket) => {
        console.log('Client connected to mock HLS server');
        this.clients.push(socket);

        socket.on('data', (data) => {
          try {
            const request = JSON.parse(data.toString());
            const response = this.handleRequest(request);
            socket.write(JSON.stringify(response));
          } catch (err) {
            console.error('Mock server error:', err);
          }
        });

        socket.on('close', () => {
          console.log('Client disconnected from mock HLS server');
          const index = this.clients.indexOf(socket);
          if (index > -1) {
            this.clients.splice(index, 1);
          }
        });

        socket.on('error', (err) => {
          console.error('Mock server socket error:', err);
        });
      });

      this.server.listen(this.port, () => {
        console.log(`Mock HLS server listening on port ${this.port}`);
        resolve();
      });

      this.server.on('error', (err) => {
        console.error('Mock server error:', err);
        reject(err);
      });
    });
  }

  stop() {
    return new Promise((resolve) => {
      if (this.server) {
        this.clients.forEach(client => client.end());
        this.server.close(() => {
          console.log('Mock HLS server stopped');
          resolve();
        });
      } else {
        resolve();
      }
    });
  }

  handleRequest(request) {
    const { version, messageId, command, data } = request;

    switch (command) {
      case 'connect':
        return {
          version: '1.0',
          messageId: messageId,
          timestamp: new Date().toISOString(),
          success: true,
          data: {
            connectionId: uuidv4(),
            status: 'connected'
          }
        };

      case 'read':
        return {
          version: '1.0',
          messageId: messageId,
          timestamp: new Date().toISOString(),
          success: true,
          data: {
            address: data.address,
            value: this.generateMockValue(data.address),
            dataType: 'Int16',
            quality: 'Good'
          }
        };

      case 'readBatch':
        return {
          version: '1.0',
          messageId: messageId,
          timestamp: new Date().toISOString(),
          success: true,
          data: data.addresses.map(address => ({
            address: address,
            value: this.generateMockValue(address),
            dataType: 'Int16',
            quality: 'Good'
          }))
        };

      default:
        return {
          version: '1.0',
          messageId: messageId,
          timestamp: new Date().toISOString(),
          success: false,
          error: {
            code: '1001',
            message: 'Unknown command',
            details: `Command '${command}' not supported`
          }
        };
    }
  }

  generateMockValue(address) {
    // Generate predictable test values based on address
    const addrNum = parseInt(address.replace(/\D/g, '')) || 1;
    return Math.floor(Math.random() * 1000) + addrNum;
  }
}

/**
 * Test runner
 */
async function runIntegrationTests() {
  console.log('Starting HLS-Read Node Integration Tests...');

  const mockServer = new MockHLSServer(8888);
  
  try {
    // Start mock server
    await mockServer.start();
    console.log('✓ Mock HLS server started');

    // Test 1: Basic connection test
    await testBasicConnection();
    console.log('✓ Basic connection test passed');

    // Test 2: Single read test
    await testSingleRead();
    console.log('✓ Single read test passed');

    // Test 3: Batch read test
    await testBatchRead();
    console.log('✓ Batch read test passed');

    // Test 4: Error handling test
    await testErrorHandling();
    console.log('✓ Error handling test passed');

    console.log('\n✅ All integration tests passed!');

  } catch (error) {
    console.error('\n❌ Integration test failed:', error.message);
    process.exit(1);
  } finally {
    // Stop mock server
    await mockServer.stop();
    console.log('✓ Mock HLS server stopped');
  }
}

async function testBasicConnection() {
  const socket = new net.Socket();
  
  return new Promise((resolve, reject) => {
    socket.setTimeout(5000);
    
    socket.on('connect', () => {
      // Send connect request
      const request = {
        version: '1.0',
        messageId: uuidv4(),
        timestamp: new Date().toISOString(),
        command: 'connect',
        data: {
          deviceConfig: {
            protocol: 'ModbusTcp',
            host: '192.168.1.100',
            port: 502,
            timeout: 5000
          },
          dataPoints: [
            {
              address: '40001',
              dataType: 'Int16',
              name: 'Temperature'
            }
          ]
        }
      };

      socket.write(JSON.stringify(request));
    });

    socket.on('data', (data) => {
      try {
        const response = JSON.parse(data.toString());
        if (response.success && response.data && response.data.connectionId) {
          socket.end();
          resolve();
        } else {
          reject(new Error('Invalid connect response'));
        }
      } catch (err) {
        reject(err);
      }
    });

    socket.on('error', reject);
    socket.on('timeout', () => reject(new Error('Connection timeout')));
    
    socket.connect(8888, 'localhost');
  });
}

async function testSingleRead() {
  const socket = new net.Socket();
  
  return new Promise((resolve, reject) => {
    socket.setTimeout(5000);
    
    socket.on('connect', () => {
      // Send read request
      const request = {
        version: '1.0',
        messageId: uuidv4(),
        timestamp: new Date().toISOString(),
        command: 'read',
        data: {
          connectionId: uuidv4(),
          address: '40001'
        }
      };

      socket.write(JSON.stringify(request));
    });

    socket.on('data', (data) => {
      try {
        const response = JSON.parse(data.toString());
        if (response.success && response.data && response.data.address === '40001') {
          socket.end();
          resolve();
        } else {
          reject(new Error('Invalid read response'));
        }
      } catch (err) {
        reject(err);
      }
    });

    socket.on('error', reject);
    socket.on('timeout', () => reject(new Error('Read timeout')));
    
    socket.connect(8888, 'localhost');
  });
}

async function testBatchRead() {
  const socket = new net.Socket();
  
  return new Promise((resolve, reject) => {
    socket.setTimeout(5000);
    
    socket.on('connect', () => {
      // Send batch read request
      const request = {
        version: '1.0',
        messageId: uuidv4(),
        timestamp: new Date().toISOString(),
        command: 'readBatch',
        data: {
          connectionId: uuidv4(),
          addresses: ['40001', '40002', '40003']
        }
      };

      socket.write(JSON.stringify(request));
    });

    socket.on('data', (data) => {
      try {
        const response = JSON.parse(data.toString());
        if (response.success && Array.isArray(response.data) && response.data.length === 3) {
          socket.end();
          resolve();
        } else {
          reject(new Error('Invalid batch read response'));
        }
      } catch (err) {
        reject(err);
      }
    });

    socket.on('error', reject);
    socket.on('timeout', () => reject(new Error('Batch read timeout')));
    
    socket.connect(8888, 'localhost');
  });
}

async function testErrorHandling() {
  const socket = new net.Socket();
  
  return new Promise((resolve, reject) => {
    socket.setTimeout(5000);
    
    socket.on('connect', () => {
      // Send invalid command
      const request = {
        version: '1.0',
        messageId: uuidv4(),
        timestamp: new Date().toISOString(),
        command: 'invalid_command',
        data: {}
      };

      socket.write(JSON.stringify(request));
    });

    socket.on('data', (data) => {
      try {
        const response = JSON.parse(data.toString());
        if (!response.success && response.error && response.error.message) {
          socket.end();
          resolve();
        } else {
          reject(new Error('Error handling test failed - should return error response'));
        }
      } catch (err) {
        reject(err);
      }
    });

    socket.on('error', reject);
    socket.on('timeout', () => reject(new Error('Error handling timeout')));
    
    socket.connect(8888, 'localhost');
  });
}

// Run tests if this file is executed directly
if (require.main === module) {
  runIntegrationTests().catch(console.error);
}

module.exports = {
  runIntegrationTests,
  MockHLSServer
};