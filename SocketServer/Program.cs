using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;

namespace PetServer
{
    // 客户端发送的请求格式
    public class ClientRequest
    {
        public string query { get; set; }
    }

    // 服务器返回的响应格式
    public class ServerResponse
    {
        public int? id { get; set; }
        public string name { get; set; }
        public bool success { get; set; }
        public string message { get; set; }
    }

    class Program
    {
        // 宠物数据：ID -> 宠物名（同时保证名字唯一）
        private static Dictionary<int, string> idToName = new Dictionary<int, string>()
        {
            { 1, "伊莎公主" },
            { 2, "吉拉" },
            { 3, "大师兔" }
        };

        // 宠物名 -> ID（用于反向查询）
        private static Dictionary<string, int> nameToId = new Dictionary<string, int>();

        static void Main(string[] args)
        {
            // 构建反向映射（假设宠物名唯一）
            foreach (var kvp in idToName)
            {
                if (!nameToId.ContainsKey(kvp.Value))
                {
                    nameToId.Add(kvp.Value, kvp.Key);
                }
            }

            Console.WriteLine("=== 宠物查询服务器 ===");
            Console.WriteLine($"宠物数据：");
            foreach (var kvp in idToName)
            {
                Console.WriteLine($"  {kvp.Key} -> {kvp.Value}");
            }
            Console.WriteLine();

            // 启动 TCP 服务器
            StartServer(8888);
        }

        static void StartServer(int port)
        {
            TcpListener listener = null;
            try
            {
                listener = new TcpListener(IPAddress.Any, port);
                listener.Start();
                Console.WriteLine($"服务器已启动，监听端口 {port}");
                Console.WriteLine("等待客户端连接...\n");

                while (true)
                {
                    // 接受客户端连接（阻塞）
                    TcpClient client = listener.AcceptTcpClient();
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 客户端已连接");

                    // 为每个客户端创建独立线程处理（支持多客户端）
                    var clientThread = new System.Threading.Thread(() => HandleClient(client));
                    clientThread.Start();
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"服务器错误: {ex.Message}");
            }
            finally
            {
                listener?.Stop();
            }
        }

        static void HandleClient(TcpClient client)
        {
            NetworkStream stream = null;
            try
            {
                stream = client.GetStream();
                byte[] buffer = new byte[4096];

                while (client.Connected)
                {
                    // 读取数据（非阻塞检测）
                    if (!stream.DataAvailable)
                    {
                        System.Threading.Thread.Sleep(50);
                        continue;
                    }

                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead == 0)
                    {
                        // 客户端正常断开
                        break;
                    }

                    string receivedJson = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 收到: {receivedJson}");

                    // 处理请求并返回响应
                    string responseJson = ProcessRequest(receivedJson);
                    byte[] responseData = Encoding.UTF8.GetBytes(responseJson);
                    stream.Write(responseData, 0, responseData.Length);
                    Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 返回: {responseJson}\n");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"处理客户端时出错: {ex.Message}");
            }
            finally
            {
                stream?.Close();
                client.Close();
                Console.WriteLine($"[{DateTime.Now:HH:mm:ss}] 客户端已断开\n");
            }
        }

        static string ProcessRequest(string json)
        {
            try
            {
                // 解析 JSON
                var request = JsonSerializer.Deserialize<ClientRequest>(json);
                if (request == null || string.IsNullOrEmpty(request.query))
                {
                    return JsonSerializer.Serialize(new ServerResponse
                    {
                        success = false,
                        message = "请求格式错误"
                    });
                }

                string query = request.query.Trim();
                ServerResponse response = new ServerResponse { success = true };

                // 判断是否为纯数字
                if (int.TryParse(query, out int id))
                {
                    // 按 ID 查询
                    if (idToName.ContainsKey(id))
                    {
                        response.id = id;
                        response.name = idToName[id];
                        response.message = $"ID {id} 对应宠物: {response.name}";
                    }
                    else
                    {
                        response.success = false;
                        response.message = "没有这个 ID 或宠物名";
                    }
                }
                else
                {
                    // 按宠物名查询
                    if (nameToId.ContainsKey(query))
                    {
                        response.id = nameToId[query];
                        response.name = query;
                        response.message = $"宠物 {query} 对应的 ID 是: {response.id}";
                    }
                    else
                    {
                        response.success = false;
                        response.message = "没有这个 ID 或宠物名";
                    }
                }

                return JsonSerializer.Serialize(response);
            }
            catch (Exception ex)
            {
                return JsonSerializer.Serialize(new ServerResponse
                {
                    success = false,
                    message = $"服务器处理错误: {ex.Message}"
                });
            }
        }
    }
}