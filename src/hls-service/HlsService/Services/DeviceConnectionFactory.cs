using HlsService.Interfaces;
using HlsService.Models;
using HlsService.Services.Devices;

namespace HlsService.Services
{
    /// <summary>
    /// 设备连接工厂
    /// 负责根据设备类型创建相应的设备连接实例
    /// </summary>
    public class DeviceConnectionFactory
    {
        /// <summary>
        /// 创建设备连接
        /// </summary>
        /// <param name="deviceConfig">设备配置</param>
        /// <returns>设备连接实例</returns>
        public static IDeviceConnection CreateDeviceConnection(DeviceConfiguration deviceConfig)
        {
            if (deviceConfig == null)
                throw new ArgumentNullException(nameof(deviceConfig));

            return deviceConfig.Type switch
            {
                DeviceType.ModbusTcp => new ModbusTcpConnectionSimple(deviceConfig.DeviceId),
                DeviceType.ModbusRtu => throw new NotSupportedException("Modbus RTU not implemented yet"),
                DeviceType.SiemensS7 => throw new NotSupportedException("Siemens S7 not implemented yet"), 
                DeviceType.OmronFins => throw new NotSupportedException("Omron FINS not implemented yet"),
                DeviceType.MitsubishiMC => throw new NotSupportedException("Mitsubishi MC not implemented yet"),
                _ => throw new NotSupportedException($"Unsupported device type: {deviceConfig.Type}")
            };
        }

        /// <summary>
        /// 获取支持的设备类型列表
        /// </summary>
        /// <returns>支持的设备类型</returns>
        public static DeviceType[] GetSupportedDeviceTypes()
        {
            return new[]
            {
                DeviceType.ModbusTcp
                // 其他协议正在开发中
            };
        }

        /// <summary>
        /// 获取设备类型的显示名称
        /// </summary>
        /// <param name="deviceType">设备类型</param>
        /// <returns>显示名称</returns>
        public static string GetDeviceTypeDisplayName(DeviceType deviceType)
        {
            return deviceType switch
            {
                DeviceType.ModbusTcp => "Modbus TCP",
                DeviceType.ModbusRtu => "Modbus RTU",
                DeviceType.SiemensS7 => "Siemens S7",
                DeviceType.OmronFins => "Omron FINS",
                DeviceType.MitsubishiMC => "Mitsubishi MC",
                _ => "Unknown"
            };
        }

        /// <summary>
        /// 验证设备配置
        /// </summary>
        /// <param name="deviceConfig">设备配置</param>
        /// <returns>验证结果</returns>
        public static (bool IsValid, string ErrorMessage) ValidateDeviceConfiguration(DeviceConfiguration deviceConfig)
        {
            if (deviceConfig == null)
                return (false, "Device configuration is null");

            if (string.IsNullOrWhiteSpace(deviceConfig.DeviceId))
                return (false, "Device ID is required");

            if (deviceConfig.Type == DeviceType.Unknown)
                return (false, "Device type must be specified");

            // 根据设备类型验证特定的配置参数
            return deviceConfig.Type switch
            {
                DeviceType.ModbusTcp => ValidateModbusTcpConfig(deviceConfig),
                DeviceType.ModbusRtu => ValidateModbusRtuConfig(deviceConfig),
                DeviceType.SiemensS7 => ValidateSiemensS7Config(deviceConfig),
                DeviceType.OmronFins => ValidateOmronFinsConfig(deviceConfig),
                DeviceType.MitsubishiMC => ValidateMitsubishiMCConfig(deviceConfig),
                _ => (false, $"Unsupported device type: {deviceConfig.Type}")
            };
        }

        #region 设备配置验证方法

        private static (bool IsValid, string ErrorMessage) ValidateModbusTcpConfig(DeviceConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Connection.Host))
                return (false, "Host address is required for Modbus TCP");

            if (config.Connection.Port <= 0 || config.Connection.Port > 65535)
                return (false, "Invalid port number for Modbus TCP");

            if (config.Connection.TimeoutMs < 1000 || config.Connection.TimeoutMs > 30000)
                return (false, "Timeout must be between 1000ms and 30000ms");

            return (true, string.Empty);
        }

        private static (bool IsValid, string ErrorMessage) ValidateModbusRtuConfig(DeviceConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Connection.ComPort))
                return (false, "COM port is required for Modbus RTU");

            if (config.Connection.BaudRate <= 0)
                return (false, "Valid baud rate is required for Modbus RTU");

            return (true, string.Empty);
        }

        private static (bool IsValid, string ErrorMessage) ValidateSiemensS7Config(DeviceConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Connection.Host))
                return (false, "Host address is required for Siemens S7");

            if (config.Connection.Rack < 0 || config.Connection.Rack > 7)
                return (false, "Rack number must be between 0 and 7 for Siemens S7");

            if (config.Connection.Slot < 0 || config.Connection.Slot > 31)
                return (false, "Slot number must be between 0 and 31 for Siemens S7");

            return (true, string.Empty);
        }

        private static (bool IsValid, string ErrorMessage) ValidateOmronFinsConfig(DeviceConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Connection.Host))
                return (false, "Host address is required for Omron FINS");

            return (true, string.Empty);
        }

        private static (bool IsValid, string ErrorMessage) ValidateMitsubishiMCConfig(DeviceConfiguration config)
        {
            if (string.IsNullOrWhiteSpace(config.Connection.Host))
                return (false, "Host address is required for Mitsubishi MC");

            return (true, string.Empty);
        }

        #endregion
    }

}