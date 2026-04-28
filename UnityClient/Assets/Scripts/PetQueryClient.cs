using System;
using System.Collections;
using System.Net.Sockets;
using System.Text;
using System.Text.RegularExpressions;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

// 与服务器对应的数据格式
[Serializable]
public class ClientRequest
{
    public string query;
}

[Serializable]
public class ServerResponse
{
    public int? id;
    public string name;
    public bool success;
    public string message;
}

public class PetQueryClient : MonoBehaviour
{
    [Header("UI 组件")]
    public InputField inputField;      // 如果用普通 InputField，改为 InputField
    public Button queryButton;
    public Text resultText;                // 如果用 TextMeshPro，改为 TMP_Text

    [Header("网络设置")]
    public string serverIP = "127.0.0.1";
    public int serverPort = 8888;

    private TcpClient tcpClient;
    private NetworkStream stream;
    private bool isConnected = false;

    // 用于异步接收的队列
    private string pendingResponse = null;
    private readonly object responseLock = new object();

    void Start()
    {
        // 绑定按钮事件
        queryButton.onClick.AddListener(OnQueryButtonClick);

        // 启动时连接服务器
        ConnectToServer();
    }

    void ConnectToServer()
    {
        try
        {
            tcpClient = new TcpClient();
            tcpClient.BeginConnect(serverIP, serverPort, OnConnectComplete, null);
            resultText.text = "正在连接服务器...";
        }
        catch (Exception e)
        {
            resultText.text = $"连接失败: {e.Message}";
            Debug.LogError($"连接失败: {e.Message}");
        }
    }

    void OnConnectComplete(IAsyncResult ar)
    {
        try
        {
            tcpClient.EndConnect(ar);
            stream = tcpClient.GetStream();
            isConnected = true;

            // UI 操作需要在主线程
            UnityMainThreadDispatcher.ExecuteOnMainThread(() =>
            {
                resultText.text = "已连接服务器，请输入 ID 或宠物名";
            });

            Debug.Log("已连接到服务器");

            // 启动接收线程
            StartReceiving();
        }
        catch (Exception e)
        {
            UnityMainThreadDispatcher.ExecuteOnMainThread(() =>
            {
                resultText.text = $"连接错误: {e.Message}";
            });
        }
    }

    void StartReceiving()
    {
        // 使用线程接收数据，避免阻塞主线程
        System.Threading.Thread receiveThread = new System.Threading.Thread(ReceiveData);
        receiveThread.IsBackground = true;
        receiveThread.Start();
    }

    void ReceiveData()
    {
        byte[] buffer = new byte[4096];

        while (isConnected && tcpClient != null && tcpClient.Connected)
        {
            try
            {
                if (stream.DataAvailable)
                {
                    int bytesRead = stream.Read(buffer, 0, buffer.Length);
                    if (bytesRead > 0)
                    {
                        string response = Encoding.UTF8.GetString(buffer, 0, bytesRead);
                        lock (responseLock)
                        {
                            pendingResponse = response;
                        }
                    }
                }
                else
                {
                    System.Threading.Thread.Sleep(50);
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"接收数据错误: {e.Message}");
                break;
            }
        }
    }

    void OnQueryButtonClick()
    {
        if (!isConnected || tcpClient == null || !tcpClient.Connected)
        {
            resultText.text = "未连接到服务器，正在重连...";
            ConnectToServer();
            return;
        }

        string query = inputField.text.Trim();
        if (string.IsNullOrEmpty(query))
        {
            resultText.text = "请输入 ID 或宠物名";
            return;
        }

        // 构造请求
        ClientRequest request = new ClientRequest { query = query };
        string json = JsonUtility.ToJson(request);

        // 发送数据
        try
        {
            byte[] data = Encoding.UTF8.GetBytes(json);
            stream.Write(data, 0, data.Length);
            resultText.text = "查询中...";
        }
        catch (Exception e)
        {
            resultText.text = $"发送失败: {e.Message}";
            isConnected = false;
        }
    }

    void Update()
    {
        // 在主线程处理接收到的响应
        lock (responseLock)
        {
            if (pendingResponse != null)
            {
                ProcessResponse(pendingResponse);
                pendingResponse = null;
            }
        }
    }

    void ProcessResponse(string json)
    {
        try
        {
            ServerResponse response = JsonUtility.FromJson<ServerResponse>(json);
            if (response.success)
            {
                resultText.text = response.message;
            }
            else
            {
                resultText.text = response.message;
            }
        }
        catch (Exception e)
        {
            resultText.text = $"解析响应失败: {e.Message}";
            Debug.LogError($"解析失败: {json}");
        }
    }

    void OnDestroy()
    {
        // 关闭连接
        if (stream != null)
        {
            stream.Close();
            stream = null;
        }
        if (tcpClient != null)
        {
            tcpClient.Close();
            tcpClient = null;
        }
        isConnected = false;
    }
}