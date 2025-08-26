using HslCommunication.ModBus;
using HslCommunication;

namespace SimpleTest
{
    class Program
    {
        static void Main()
        {
            Console.WriteLine("=== HslCommunication API 测试 ===");
            
            // 测试Modbus TCP API
            using var modbusTcp = new ModbusTcpNet("127.0.0.1", 502);
            
            Console.WriteLine("Modbus TCP 客户端创建成功");
            Console.WriteLine($"目标地址: {modbusTcp.IpAddress}:{modbusTcp.Port}");
            
            Console.WriteLine("测试完成");
        }
    }
}