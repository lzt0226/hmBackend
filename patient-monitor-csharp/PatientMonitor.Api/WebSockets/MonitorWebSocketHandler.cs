using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;

namespace PatientMonitor.Api.WebSockets;

/// <summary>
/// WebSocket 连接管理器，负责连接管理、心跳检测、超时清理和消息广播。
/// 与原 Java MonitorWebSocketHandler 行为保持一致。
/// </summary>
public class MonitorWebSocketHandler
{
    private readonly ConcurrentDictionary<string, WebSocket> _sessions = new();
    private readonly ConcurrentDictionary<string, long> _lastActivity = new();
    private readonly TimeSpan _sessionTimeout = TimeSpan.FromMinutes(5);
    private readonly TimeSpan _heartbeatInterval = TimeSpan.FromSeconds(45);
    private readonly TimeSpan _timeoutCheckInterval = TimeSpan.FromSeconds(60);
    private CancellationTokenSource? _cts;

    public int SessionCount => _sessions.Count;

    /// <summary>
    /// 启动心跳和超时检查的后台任务
    /// </summary>
    public void Start()
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => HeartbeatLoop(_cts.Token));
        _ = Task.Run(() => TimeoutCheckLoop(_cts.Token));
    }

    /// <summary>
    /// 停止后台任务
    /// </summary>
    public void Stop()
    {
        _cts?.Cancel();
    }

    /// <summary>
    /// 处理新的 WebSocket 连接
    /// </summary>
    public async Task HandleConnectionAsync(WebSocket webSocket, CancellationToken cancellationToken)
    {
        var sessionId = Guid.NewGuid().ToString("N");
        _sessions.TryAdd(sessionId, webSocket);
        _lastActivity.TryAdd(sessionId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

        var buffer = new byte[4096];
        try
        {
            while (webSocket.State == WebSocketState.Open && !cancellationToken.IsCancellationRequested)
            {
                var result = await webSocket.ReceiveAsync(new ArraySegment<byte>(buffer), cancellationToken);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    await webSocket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", cancellationToken);
                    break;
                }

                // 更新最后活跃时间
                _lastActivity.TryAdd(sessionId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var message = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    // 处理心跳响应
                    if (message.Equals("pong", StringComparison.OrdinalIgnoreCase) ||
                        message.Equals("ping", StringComparison.OrdinalIgnoreCase))
                    {
                        continue;
                    }
                }
            }
        }
        catch (WebSocketException)
        {
            // 连接异常断开
        }
        finally
        {
            SafeRemoveSession(sessionId);
        }
    }

    /// <summary>
    /// 广播消息给所有客户端
    /// </summary>
    public async Task BroadcastAsync(string message)
    {
        if (_sessions.IsEmpty) return;

        var data = Encoding.UTF8.GetBytes(message);
        var deadSessions = new List<string>();

        foreach (var (sessionId, webSocket) in _sessions)
        {
            try
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    await webSocket.SendAsync(new ArraySegment<byte>(data), WebSocketMessageType.Text, true, CancellationToken.None);
                    _lastActivity[sessionId] = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                }
                else
                {
                    deadSessions.Add(sessionId);
                }
            }
            catch
            {
                deadSessions.Add(sessionId);
            }
        }

        foreach (var id in deadSessions)
        {
            SafeRemoveSession(id);
        }
    }

    /// <summary>
    /// 心跳循环：每 45 秒发送 Ping 帧
    /// </summary>
    private async Task HeartbeatLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_heartbeatInterval, cancellationToken);

            if (_sessions.IsEmpty) continue;

            var deadSessions = new List<string>();
            foreach (var (sessionId, webSocket) in _sessions)
            {
                try
                {
                    if (webSocket.State == WebSocketState.Open)
                    {
                        // 发送文本心跳（兼容浏览器 WebSocket 不支持 Ping 帧）
                        var heartbeat = Encoding.UTF8.GetBytes("ping");
                        await webSocket.SendAsync(new ArraySegment<byte>(heartbeat), WebSocketMessageType.Text, true, CancellationToken.None);
                    }
                    else
                    {
                        deadSessions.Add(sessionId);
                    }
                }
                catch
                {
                    deadSessions.Add(sessionId);
                }
            }

            foreach (var id in deadSessions)
            {
                SafeRemoveSession(id);
            }
        }
    }

    /// <summary>
    /// 超时检查：每 60 秒检查，超过 5 分钟无活动则断开
    /// </summary>
    private async Task TimeoutCheckLoop(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            await Task.Delay(_timeoutCheckInterval, cancellationToken);

            if (_sessions.IsEmpty) continue;

            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            var timeoutSessions = _lastActivity
                .Where(kv => now - kv.Value > _sessionTimeout.TotalMilliseconds)
                .Select(kv => kv.Key)
                .ToList();

            foreach (var id in timeoutSessions)
            {
                SafeRemoveSession(id);
            }
        }
    }

    /// <summary>
    /// 安全移除会话
    /// </summary>
    private void SafeRemoveSession(string sessionId)
    {
        if (_sessions.TryRemove(sessionId, out var webSocket))
        {
            _lastActivity.TryRemove(sessionId, out _);
            try
            {
                if (webSocket.State == WebSocketState.Open)
                {
                    webSocket.CloseAsync(WebSocketCloseStatus.EndpointUnavailable, "Session timeout", CancellationToken.None);
                }
            }
            catch
            {
                // 忽略关闭异常
            }
            finally
            {
                webSocket.Dispose();
            }
        }
    }
}
