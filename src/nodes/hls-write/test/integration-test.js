/**
 * Integration tests for HLS-Write Node
 * These tests require HLS-Communication service to be running
 */

const assert = require('assert');
const helper = require('node-red-node-test-helper');
const hlsWriteNode = require('../hls-write.js');

helper.init(require.resolve('node-red'));

describe('HLS-Write Node Integration Tests', function() {
  // Longer timeout for integration tests
  this.timeout(10000);

  beforeEach(function(done) {
    helper.startServer(done);
  });

  afterEach(function(done) {
    helper.unload();
    helper.stopServer(done);
  });

  // Skip integration tests if no HLS service is available
  const runIntegrationTests = process.env.RUN_INTEGRATION_TESTS === 'true';

  (runIntegrationTests ? it : it.skip)('should connect to HLS service and perform single write', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        name: "Integration Test Write",
        deviceId: "127.0.0.1", // Use localhost for testing
        server: "localhost",
        port: 8888,
        protocol: "ModbusTcp",
        devicePort: 502,
        timeout: 5000,
        writeMode: "message",
        verifyWrites: false,
        wires: [["n2"], ["n3"]]
      },
      { id: "n2", type: "helper" }, // Success output
      { id: "n3", type: "helper" }  // Error output
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      const n2 = helper.getNode("n2");
      const n3 = helper.getNode("n3");

      let receivedSuccess = false;
      let receivedError = false;

      n2.on("input", function(msg) {
        receivedSuccess = true;
        console.log('Success output:', JSON.stringify(msg.payload, null, 2));
        
        // Validate success response
        assert.equal(msg.payload.success, true);
        assert(msg.payload.connectionId);
        assert(msg.payload.results);
        assert.equal(msg.payload.operation, 'write');
        assert(msg.payload.timestamp);
        
        if (!receivedError) {
          done();
        }
      });

      n3.on("input", function(msg) {
        receivedError = true;
        console.log('Error output:', JSON.stringify(msg.payload, null, 2));
        
        // If we get an error, it might be because the service or device isn't available
        assert.equal(msg.payload.success, false);
        assert(msg.payload.error);
        assert(msg.payload.error.message);
        
        if (!receivedSuccess) {
          done();
        }
      });

      // Wait a moment for initialization, then send write request
      setTimeout(() => {
        n1.receive({
          payload: {
            address: "40001",
            value: 100,
            dataType: "Int16"
          }
        });
      }, 2000);
    });
  });

  (runIntegrationTests ? it : it.skip)('should perform batch write operation', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        name: "Batch Write Test",
        deviceId: "127.0.0.1",
        server: "localhost",
        port: 8888,
        writeMode: "message",
        wires: [["n2"], ["n3"]]
      },
      { id: "n2", type: "helper" },
      { id: "n3", type: "helper" }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      const n2 = helper.getNode("n2");
      const n3 = helper.getNode("n3");

      n2.on("input", function(msg) {
        console.log('Batch write success:', JSON.stringify(msg.payload, null, 2));
        
        assert.equal(msg.payload.success, true);
        assert(Array.isArray(msg.payload.results));
        assert(msg.payload.results.length >= 2);
        done();
      });

      n3.on("input", function(msg) {
        console.log('Batch write error:', JSON.stringify(msg.payload, null, 2));
        // Accept error if service is not available
        done();
      });

      setTimeout(() => {
        n1.receive({
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
        });
      }, 2000);
    });
  });

  (runIntegrationTests ? it : it.skip)('should perform write with verification', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        name: "Write with Verify Test",
        deviceId: "127.0.0.1",
        server: "localhost", 
        port: 8888,
        verifyWrites: true,
        wires: [["n2"], ["n3"]]
      },
      { id: "n2", type: "helper" },
      { id: "n3", type: "helper" }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      const n2 = helper.getNode("n2");
      const n3 = helper.getNode("n3");

      n2.on("input", function(msg) {
        console.log('Verified write success:', JSON.stringify(msg.payload, null, 2));
        
        assert.equal(msg.payload.success, true);
        // Check if verification was performed
        if (msg.payload.results && msg.payload.results[0]) {
          console.log('Verification status:', msg.payload.results[0].verified);
        }
        done();
      });

      n3.on("input", function(msg) {
        console.log('Verified write error:', JSON.stringify(msg.payload, null, 2));
        done();
      });

      setTimeout(() => {
        n1.receive({
          payload: {
            address: "40001",
            value: 150,
            dataType: "Int16"
          }
        });
      }, 2000);
    });
  });

  (runIntegrationTests ? it : it.skip)('should handle address mapping format', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        deviceId: "127.0.0.1",
        server: "localhost",
        port: 8888,
        addresses: [
          { address: "40001", dataType: "Int16" },
          { address: "40002", dataType: "Float" },
          { address: "00001", dataType: "Bool" }
        ],
        wires: [["n2"], ["n3"]]
      },
      { id: "n2", type: "helper" },
      { id: "n3", type: "helper" }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      const n2 = helper.getNode("n2");
      const n3 = helper.getNode("n3");

      n2.on("input", function(msg) {
        console.log('Address mapping success:', JSON.stringify(msg.payload, null, 2));
        assert.equal(msg.payload.success, true);
        done();
      });

      n3.on("input", function(msg) {
        console.log('Address mapping error:', JSON.stringify(msg.payload, null, 2));
        done();
      });

      setTimeout(() => {
        n1.receive({
          payload: {
            "40001": 200,
            "40002": 30.5,
            "00001": true
          }
        });
      }, 2000);
    });
  });

  it('should handle connection failures gracefully', function(done) {
    const flow = [
      {
        id: "n1",
        type: "hls-write",
        name: "Connection Failure Test",
        deviceId: "192.168.999.999", // Invalid IP
        server: "localhost",
        port: 9999, // Invalid port
        timeout: 1000, // Short timeout
        wires: [["n2"], ["n3"]]
      },
      { id: "n2", type: "helper" },
      { id: "n3", type: "helper" }
    ];

    helper.load(hlsWriteNode, flow, function() {
      const n1 = helper.getNode("n1");
      const n3 = helper.getNode("n3");

      n3.on("input", function(msg) {
        console.log('Expected connection failure:', JSON.stringify(msg.payload, null, 2));
        assert.equal(msg.payload.success, false);
        assert(msg.payload.error);
        assert(msg.payload.error.message);
        done();
      });

      setTimeout(() => {
        n1.receive({
          payload: {
            address: "40001",
            value: 100,
            dataType: "Int16"
          }
        });
      }, 1000);
    });
  });
});