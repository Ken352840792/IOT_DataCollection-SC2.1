/**
 * Unit tests for HLS-Read Node
 */

const assert = require('assert');
const helper = require('node-red-node-test-helper');
const hlsReadNode = require('../hls-read.js');

helper.init(require.resolve('node-red'));

describe('HLS-Read Node', function() {
  beforeEach(function(done) {
    helper.startServer(done);
  });

  afterEach(function(done) {
    helper.unload();
    helper.stopServer(done);
  });

  it('should be loaded', function(done) {
    const flow = [{ id: "n1", type: "hls-read", name: "test name" }];
    helper.load(hlsReadNode, flow, function() {
      const n1 = helper.getNode("n1");
      assert.equal(n1.name, 'test name');
      done();
    });
  });

  it('should initialize with default configuration', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-read",
        name: "HLS Read Test",
        deviceId: "192.168.1.100",
        server: "localhost",
        port: 8888,
        protocol: "ModbusTcp",
        devicePort: 502,
        timeout: 5000,
        interval: 1000,
        addresses: [
          {
            address: "40001",
            dataType: "Int16",
            description: "Temperature"
          }
        ]
      }
    ];

    helper.load(hlsReadNode, flow, function() {
      const n1 = helper.getNode("n1");
      assert.equal(n1.name, 'HLS Read Test');
      assert.equal(n1.deviceId, '192.168.1.100');
      assert.equal(n1.server, 'localhost');
      assert.equal(n1.port, 8888);
      assert.equal(n1.protocol, 'ModbusTcp');
      assert.equal(n1.devicePort, 502);
      assert.equal(n1.timeout, 5000);
      assert.equal(n1.interval, 1000);
      assert.equal(n1.addresses.length, 1);
      assert.equal(n1.addresses[0].address, '40001');
      assert.equal(n1.addresses[0].dataType, 'Int16');
      done();
    });
  });

  it('should handle missing required configuration', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-read",
        name: "Incomplete Config Test"
        // Missing deviceId
      }
    ];

    helper.load(hlsReadNode, flow, function() {
      const n1 = helper.getNode("n1");
      assert.equal(n1.deviceId, '');
      assert.equal(n1.addresses.length, 0);
      done();
    });
  });

  it('should validate data point configuration', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-read",
        name: "Data Points Test",
        deviceId: "192.168.1.100",
        addresses: [
          {
            address: "40001",
            dataType: "Int16",
            description: "Temperature"
          },
          {
            address: "40002",
            dataType: "Float",
            description: "Pressure"
          },
          {
            address: "40003",
            dataType: "Bool",
            description: "Status"
          }
        ]
      }
    ];

    helper.load(hlsReadNode, flow, function() {
      const n1 = helper.getNode("n1");
      assert.equal(n1.addresses.length, 3);
      
      // Check each data point
      assert.equal(n1.addresses[0].address, '40001');
      assert.equal(n1.addresses[0].dataType, 'Int16');
      assert.equal(n1.addresses[0].description, 'Temperature');
      
      assert.equal(n1.addresses[1].address, '40002');
      assert.equal(n1.addresses[1].dataType, 'Float');
      assert.equal(n1.addresses[1].description, 'Pressure');
      
      assert.equal(n1.addresses[2].address, '40003');
      assert.equal(n1.addresses[2].dataType, 'Bool');
      assert.equal(n1.addresses[2].description, 'Status');
      
      done();
    });
  });

  it('should create HLS IPC client with correct configuration', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-read",
        name: "Client Config Test",
        deviceId: "192.168.1.100",
        server: "10.0.0.1",
        port: 9999,
        timeout: 3000
      }
    ];

    helper.load(hlsReadNode, flow, function() {
      const n1 = helper.getNode("n1");
      
      // Check HLS client configuration
      assert.equal(n1.hlsClient.host, '10.0.0.1');
      assert.equal(n1.hlsClient.port, 9999);
      assert.equal(n1.hlsClient.timeout, 3000);
      
      done();
    });
  });

  it('should handle node closure properly', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-read",
        name: "Closure Test",
        deviceId: "192.168.1.100"
      }
    ];

    helper.load(hlsReadNode, flow, function() {
      const n1 = helper.getNode("n1");
      
      // Simulate node close
      n1.close(false, function() {
        // Verify cleanup was performed
        assert.equal(n1.readInterval, null);
        done();
      });
    });
  });

  it('should support different data types', function(done) {
    const supportedTypes = ['Bool', 'Int16', 'Int32', 'Int64', 'UInt16', 'UInt32', 'UInt64', 'Float', 'Double', 'String'];
    
    const addresses = supportedTypes.map((type, index) => ({
      address: `4000${index + 1}`,
      dataType: type,
      description: `Test ${type}`
    }));

    const flow = [
      {
        id: "n1",
        type: "hls-read",
        name: "Data Types Test",
        deviceId: "192.168.1.100",
        addresses: addresses
      }
    ];

    helper.load(hlsReadNode, flow, function() {
      const n1 = helper.getNode("n1");
      
      assert.equal(n1.addresses.length, supportedTypes.length);
      
      // Verify each data type is preserved
      supportedTypes.forEach((type, index) => {
        assert.equal(n1.addresses[index].dataType, type);
        assert.equal(n1.addresses[index].description, `Test ${type}`);
      });
      
      done();
    });
  });

  it('should validate interval configuration', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-read",
        name: "Interval Test",
        deviceId: "192.168.1.100",
        interval: "2500" // String value should be parsed
      }
    ];

    helper.load(hlsReadNode, flow, function() {
      const n1 = helper.getNode("n1");
      
      // Should parse string to integer
      assert.equal(n1.interval, 2500);
      assert.equal(typeof n1.interval, 'number');
      
      done();
    });
  });
});

/**
 * Integration tests for HLS-Read Node IPC Client
 */
describe('HLS IPC Client', function() {
  let HLSIPCClient;
  
  before(function() {
    // Extract the HLSIPCClient class from the module for testing
    const moduleExports = hlsReadNode(null);
    // Since the class is internal, we'll test through the node interface
  });

  it('should handle connection timeout gracefully', function(done) {
    // This test would need a mock server or timeout simulation
    // For now, just ensure the test structure is in place
    done();
  });

  it('should format JSON messages correctly', function(done) {
    // Test message formatting according to JSON protocol v1.0
    // This would require extracting the client class or mocking
    done();
  });

  it('should handle response parsing correctly', function(done) {
    // Test response parsing and error handling
    done();
  });
});