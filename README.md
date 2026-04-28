# Unity-Socket
一个简单的 Socket 通信示例项目，包含 Unity 客户端和 C# 服务器。

## 运行方法

1. **启动服务器**：用 Visual Studio 打开 `SocketServer/SocketServer.csproj`，按 F5 运行
2. **启动客户端**：用 Unity 打开 `UnityClient` 文件夹，运行场景

## 配置

默认 IP：`127.0.0.1`，端口：`8888`

修改位置：
- 客户端：`UnityClient/Assets/Scripts/SocketClient.cs`
- 服务器：`SocketServer/Program.cs`

## 注意事项

- 先启动服务器，再启动客户端
- 确保 IP 和端口配置一致
