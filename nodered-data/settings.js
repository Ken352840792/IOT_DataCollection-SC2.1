/**
 * Node-RED配置文件
 * 用于SS_SC_IOT项目的Node-RED实例
 */

module.exports = {
    // Node-RED运行时设置
    uiPort: process.env.NODERED_PORT || 1880,
    
    // 管理接口设置
    httpAdminRoot: '/admin',
    httpNodeRoot: '/api',
    
    // 用户目录
    userDir: __dirname,
    
    // 流程文件
    flowFile: 'flows.json',
    
    // 凭据加密密钥
    credentialSecret: process.env.NODE_RED_CREDENTIAL_SECRET || "ss_sc_iot_default_key",
    
    // 函数节点设置
    functionGlobalContext: {
        // 全局上下文变量
        projectName: "SS_SC_IOT",
        version: require('../package.json').version
    },
    
    // 编辑器主题
    editorTheme: {
        projects: {
            // 启用项目功能
            enabled: true
        },
        palette: {
            // 调色板配置
            editable: true,
            catalogues: ['https://catalogue.nodered.org/catalogue.json'],
            uploadDir: "uploads/"
        },
        header: {
            title: "SS_SC_IOT - 数采盒子配置",
            image: ""
        }
    },
    
    // 日志设置
    logging: {
        console: {
            level: process.env.LOG_LEVEL || "info",
            metrics: false,
            audit: false
        },
        file: {
            level: "info",
            filename: "../logs/node-red.log",
            maxFiles: 5,
            maxSize: "10MB"
        }
    },
    
    // 上下文存储
    contextStorage: {
        default: {
            module: "localfilesystem"
        }
    },
    
    // 导出设置
    exportGlobalContextKeys: false,
    
    // 安全设置
    requireHttps: false,
    
    // API设置
    httpNodeCors: {
        origin: "*",
        methods: "GET,PUT,POST,DELETE"
    },
    
    // 调试设置
    debugMaxLength: 1000,
    
    // 函数超时设置
    functionTimeout: 10,
    
    // Node设置
    nodesDir: '../src/nodes',
    
    // 禁用Node-RED自动安装缺失的节点
    autoInstallModules: false
};