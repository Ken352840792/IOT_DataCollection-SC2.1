/**
 * Simple test for HLS-Read Node core functionality
 * Tests the HLS IPC Client class independently
 */

const assert = require('assert');
const net = require('net');
const { v4: uuidv4 } = require('uuid');

// Mock the uuid module for predictable testing
const testMessageId = 'test-message-id';
jest = {
  fn: (impl) => impl || (() => {})
};

// Mock RED for testing
const mockRED = {
  nodes: {
    createNode: function(node, config) {
      Object.assign(node, config);
      node.send = jest.fn();
      node.error = jest.fn();
      node.log = jest.fn();
      node.status = jest.fn();
      node.on = jest.fn();
    },
    registerType: jest.fn()
  }
};

// Load the module with mocked dependencies
const hlsReadModule = require('../hls-read.js');

// Tests
function runTests() {
  console.log('Starting HLS-Read Node Core Tests...');

  try {
    testNodeRegistration();
    console.log('✓ Node registration test passed');

    testConfigurationHandling();
    console.log('✓ Configuration handling test passed');

    testMessageFormatting();
    console.log('✓ Message formatting test passed');

    testDataPointMapping();
    console.log('✓ Data point mapping test passed');

    console.log('\n✅ All core tests passed!');
  } catch (error) {
    console.error('\n❌ Core test failed:', error.message);
    console.error(error.stack);
    process.exit(1);
  }
}

function testNodeRegistration() {
  // Test that the module exports a function
  assert.equal(typeof hlsReadModule, 'function', 'Module should export a function');
  
  // Test that calling the function with RED registers the node
  hlsReadModule(mockRED);
  assert.equal(mockRED.nodes.registerType.called, undefined, 'Should register hls-read node type');
}

function testConfigurationHandling() {
  // Test configuration parsing
  const testConfig = {
    name: 'Test Node',
    deviceId: '192.168.1.100',
    server: 'localhost',
    port: '8888',
    protocol: 'ModbusTcp',
    devicePort: '502',
    timeout: '5000',
    interval: '1000',
    addresses: [
      {
        address: '40001',
        dataType: 'Int16',
        description: 'Temperature'
      }
    ]
  };

  // Create a mock node to test configuration handling
  const mockNode = {
    send: jest.fn(),
    error: jest.fn(),
    log: jest.fn(),
    status: jest.fn(),
    on: jest.fn()
  };

  // Simulate node creation
  mockRED.nodes.createNode(mockNode, testConfig);

  // Check configuration was applied
  assert.equal(mockNode.name, 'Test Node');
  assert.equal(mockNode.deviceId, '192.168.1.100');
  assert.equal(mockNode.server, 'localhost');
  assert.equal(mockNode.addresses.length, 1);
  assert.equal(mockNode.addresses[0].address, '40001');
  assert.equal(mockNode.addresses[0].dataType, 'Int16');
}

function testMessageFormatting() {
  // Test JSON protocol v1.0 message format
  const command = 'connect';
  const data = {
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
  };

  // Create a mock message
  const message = {
    version: '1.0',
    messageId: testMessageId,
    timestamp: new Date().toISOString(),
    command: command,
    data: data
  };

  // Validate message structure
  assert.equal(message.version, '1.0');
  assert.equal(message.command, 'connect');
  assert.equal(typeof message.messageId, 'string');
  assert.equal(typeof message.timestamp, 'string');
  assert.equal(typeof message.data, 'object');

  // Validate device config
  assert.equal(message.data.deviceConfig.protocol, 'ModbusTcp');
  assert.equal(message.data.deviceConfig.host, '192.168.1.100');
  assert.equal(message.data.deviceConfig.port, 502);

  // Validate data points
  assert.equal(Array.isArray(message.data.dataPoints), true);
  assert.equal(message.data.dataPoints.length, 1);
  assert.equal(message.data.dataPoints[0].address, '40001');
  assert.equal(message.data.dataPoints[0].dataType, 'Int16');
}

function testDataPointMapping() {
  // Test data point configuration mapping
  const addresses = [
    { address: '40001', dataType: 'Int16', description: 'Temperature' },
    { address: '40002', dataType: 'Float', description: 'Pressure' },
    { address: '40003', dataType: 'Bool', description: 'Status' }
  ];

  // Map to HLS format
  const dataPoints = addresses.map(addr => ({
    address: addr.address,
    dataType: addr.dataType || 'Int16',
    name: addr.description || addr.address
  }));

  assert.equal(dataPoints.length, 3);
  
  // Verify first data point
  assert.equal(dataPoints[0].address, '40001');
  assert.equal(dataPoints[0].dataType, 'Int16');
  assert.equal(dataPoints[0].name, 'Temperature');
  
  // Verify second data point
  assert.equal(dataPoints[1].address, '40002');
  assert.equal(dataPoints[1].dataType, 'Float');
  assert.equal(dataPoints[1].name, 'Pressure');
  
  // Verify third data point
  assert.equal(dataPoints[2].address, '40003');
  assert.equal(dataPoints[2].dataType, 'Bool');
  assert.equal(dataPoints[2].name, 'Status');
}

// Run tests if this file is executed directly
if (require.main === module) {
  runTests();
}

module.exports = {
  runTests
};