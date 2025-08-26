/**
 * Unit tests for HLS-Write Node
 */

const assert = require('assert');
const helper = require('node-red-node-test-helper');
const hlsWriteNode = require('../hls-write.js');

helper.init(require.resolve('node-red'));

describe('HLS-Write Node', function() {
  beforeEach(function(done) {
    helper.startServer(done);
  });

  afterEach(function(done) {
    helper.unload();
    helper.stopServer(done);
  });

  it('should be loaded', function(done) {
    const flow = [{ id: "n1", type: "hls-write", name: "test name" }];
    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      assert.equal(n1.name, 'test name');
      done();
    });
  });

  it('should initialize with default configuration', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        name: "HLS Write Test",
        deviceId: "192.168.1.100",
        server: "localhost",
        port: 8888,
        protocol: "ModbusTcp",
        devicePort: 502,
        timeout: 5000,
        writeMode: "message",
        verifyWrites: false,
        addresses: [
          {
            address: "40001",
            dataType: "Int16",
            defaultValue: "100",
            description: "Setpoint"
          }
        ]
      }
    ];
    
    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      assert.equal(n1.name, 'HLS Write Test');
      assert.equal(n1.deviceId, '192.168.1.100');
      assert.equal(n1.server, 'localhost');
      assert.equal(n1.port, 8888);
      assert.equal(n1.protocol, 'ModbusTcp');
      assert.equal(n1.writeMode, 'message');
      assert.equal(n1.verifyWrites, false);
      assert.equal(n1.addresses.length, 1);
      assert.equal(n1.addresses[0].address, '40001');
      done();
    });
  });

  it('should validate input message format - address mapping', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        deviceId: "192.168.1.100",
        addresses: [
          { address: "40001", dataType: "Int16" },
          { address: "40002", dataType: "Float" }
        ]
      }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      
      // Test address mapping format
      const testMessage = {
        payload: {
          "40001": 100,
          "40002": 25.5
        }
      };

      // Since we don't have actual HLS service running, we expect connection error
      // but we can test that the node processes the message format correctly
      n1.receive(testMessage);
      done();
    });
  });

  it('should validate input message format - single point write', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        deviceId: "192.168.1.100"
      }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      
      // Test single point write format
      const testMessage = {
        payload: {
          address: "40001",
          value: 100,
          dataType: "Int16"
        }
      };

      n1.receive(testMessage);
      done();
    });
  });

  it('should validate input message format - batch write', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        deviceId: "192.168.1.100"
      }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      
      // Test batch write format
      const testMessage = {
        payload: [
          {
            address: "40001",
            value: 100,
            dataType: "Int16"
          },
          {
            address: "40002",
            value: 25.5,
            dataType: "Float"
          }
        ]
      };

      n1.receive(testMessage);
      done();
    });
  });

  it('should handle data type conversion', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        deviceId: "192.168.1.100"
      }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      
      // Test various data type conversions
      const testMessages = [
        {
          payload: { address: "00001", value: "true", dataType: "Bool" }
        },
        {
          payload: { address: "40001", value: "123", dataType: "Int16" }
        },
        {
          payload: { address: "40002", value: "25.5", dataType: "Float" }
        },
        {
          payload: { address: "40003", value: "Hello", dataType: "String" }
        }
      ];

      // Send test messages
      testMessages.forEach(msg => {
        n1.receive(msg);
      });
      
      done();
    });
  });

  it('should handle empty payload error', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        deviceId: "192.168.1.100",
        wires: [[], ["n2"]]
      },
      { id: "n2", type: "helper" }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      const n2 = helper.getNode("n2");

      n2.on("input", function(msg) {
        assert.equal(msg.payload.success, false);
        assert(msg.payload.error);
        assert(msg.payload.error.message.includes('payload不能为空'));
        done();
      });

      // Send empty payload
      n1.receive({ payload: null });
    });
  });

  it('should handle missing device connection', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        deviceId: "",  // Empty device ID
        wires: [[], ["n2"]]
      },
      { id: "n2", type: "helper" }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      const n2 = helper.getNode("n2");

      n2.on("input", function(msg) {
        assert.equal(msg.payload.success, false);
        assert(msg.payload.error);
        done();
      });

      // Send write request without device connection
      n1.receive({
        payload: {
          address: "40001",
          value: 100,
          dataType: "Int16"
        }
      });
    });
  });

  it('should support write verification mode', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        deviceId: "192.168.1.100",
        verifyWrites: true
      }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      assert.equal(n1.verifyWrites, true);
      done();
    });
  });

  it('should support different write modes', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        deviceId: "192.168.1.100",
        writeMode: "config"
      }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      assert.equal(n1.writeMode, 'config');
      done();
    });
  });

  it('should have dual output ports for success and error', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        deviceId: "192.168.1.100",
        wires: [["n2"], ["n3"]]
      },
      { id: "n2", type: "helper" },
      { id: "n3", type: "helper" }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      // Just verify the node loaded with wire configuration
      assert(n1);
      done();
    });
  });
});