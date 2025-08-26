/**
 * Node-RED HLS Communication Nodes Entry Point
 * 
 * This file exports the main entry point for the HLS communication nodes package.
 * Node-RED will load this module and register the nodes defined in the package.json.
 */

module.exports = function(RED) {
    // This module serves as the entry point for Node-RED to discover our nodes
    // The actual node registration is done in individual node files
    
    // Note: Individual nodes are registered automatically by Node-RED
    // based on the "node-red.nodes" configuration in package.json
    console.log('HLS Communication Nodes package loaded');
};