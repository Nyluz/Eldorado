using System;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using UnityEngine;

public class AdminWsClient : MonoBehaviour
{
    private ClientWebSocket socket;
    private CancellationTokenSource cts;

    [SerializeField] private string wsUrl = "ws://localhost:3000/adminws";

    public async void ConnectAndLogin(string userId)
    {
        socket = new ClientWebSocket();
        cts = new CancellationTokenSource();

        try
        {
            Uri serverUri = new Uri(wsUrl);
            await socket.ConnectAsync(serverUri, cts.Token);
            Debug.Log("Connected to WebSocket server.");

            // Send login message
            var loginMsg = $"{{\"type\":\"login\",\"userId\":\"{userId}\"}}";
            ArraySegment<byte> bytes = new ArraySegment<byte>(Encoding.UTF8.GetBytes(loginMsg));
            await socket.SendAsync(bytes, WebSocketMessageType.Text, true, cts.Token);
            //Debug.Log("Login message sent: " + loginMsg);

            // Start receiving messages
            _ = ReceiveLoop();
        }
        catch (Exception ex)
        {
            Debug.LogError("WebSocket error: " + ex.Message);
        }
    }

    private async System.Threading.Tasks.Task ReceiveLoop()
    {
        var buffer = new byte[1024];
        try
        {
            while (socket.State == WebSocketState.Open)
            {
                var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), cts.Token);
                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Debug.Log("Server closed connection.");
                    await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "", cts.Token);
                }
                else
                {
                    string msg = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    //Debug.Log("Received: " + msg);
                }
            }
        }
        catch (Exception ex)
        {
            //Debug.LogError("Receive loop error: " + ex.Message);
        }
    }

    private async void OnApplicationQuit()
    {
        if (socket != null && socket.State == WebSocketState.Open)
        {
            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Quit", cts.Token);
            socket.Dispose();
        }
    }
}
