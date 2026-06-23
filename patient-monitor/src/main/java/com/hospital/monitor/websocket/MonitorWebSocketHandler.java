package com.hospital.monitor.websocket;

import lombok.extern.slf4j.Slf4j;
import org.springframework.scheduling.annotation.Scheduled;
import org.springframework.stereotype.Component;
import org.springframework.web.socket.CloseStatus;
import org.springframework.web.socket.PingMessage;
import org.springframework.web.socket.PongMessage;
import org.springframework.web.socket.TextMessage;
import org.springframework.web.socket.WebSocketSession;
import org.springframework.web.socket.handler.TextWebSocketHandler;

import java.io.IOException;
import java.nio.ByteBuffer;
import java.util.Iterator;
import java.util.Map;
import java.util.concurrent.ConcurrentHashMap;
import java.util.concurrent.atomic.AtomicInteger;

@Slf4j
@Component
public class MonitorWebSocketHandler extends TextWebSocketHandler {

    // 存储所有活跃的WebSocket会话
    private static final ConcurrentHashMap<String, WebSocketSession> SESSIONS = new ConcurrentHashMap<>();
    
    // 记录每个会话的最后活跃时间
    private static final ConcurrentHashMap<String, Long> LAST_ACTIVITY = new ConcurrentHashMap<>();
    
    // 超时时间（毫秒）- 5分钟无活动则断开
    private static final long SESSION_TIMEOUT_MS = 5 * 60 * 1000;
    
    // 心跳间隔（毫秒）- 每45秒发送一次心跳
    private static final long HEARTBEAT_INTERVAL_MS = 45 * 1000;

    @Override
    public void afterConnectionEstablished(WebSocketSession session) throws Exception {
        String sessionId = session.getId();
        SESSIONS.put(sessionId, session);
        LAST_ACTIVITY.put(sessionId, System.currentTimeMillis());
        
        log.info("WebSocket连接建立 - SessionId: {}, 远程地址: {}, 当前连接数: {}", 
                sessionId, 
                session.getRemoteAddress(), 
                SESSIONS.size());
    }

    @Override
    protected void handleTextMessage(WebSocketSession session, TextMessage message) throws Exception {
        String payload = message.getPayload();
        String sessionId = session.getId();
        
        // 更新活跃时间
        LAST_ACTIVITY.put(sessionId, System.currentTimeMillis());
        
        // 处理心跳响应
        if ("pong".equalsIgnoreCase(payload) || "ping".equalsIgnoreCase(payload)) {
            log.debug("收到客户端 {} 心跳响应: {}", sessionId, payload);
            return;
        }
        
        // 处理其他业务消息
        log.debug("收到客户端 {} 消息: {}", sessionId, payload);
    }

    @Override
    protected void handlePongMessage(WebSocketSession session, PongMessage message) throws Exception {
        String sessionId = session.getId();
        LAST_ACTIVITY.put(sessionId, System.currentTimeMillis());
        log.debug("收到客户端 {} Pong响应", sessionId);
    }

    @Override
    public void afterConnectionClosed(WebSocketSession session, CloseStatus status) throws Exception {
        String sessionId = session.getId();
        SESSIONS.remove(sessionId);
        LAST_ACTIVITY.remove(sessionId);
        
        log.info("WebSocket连接关闭 - SessionId: {}, 状态码: {}, 原因: {}, 当前连接数: {}", 
                sessionId, 
                status.getCode(), 
                status.getReason(),
                SESSIONS.size());
    }

    @Override
    public void handleTransportError(WebSocketSession session, Throwable exception) throws Exception {
        String sessionId = session.getId();
        log.error("WebSocket传输错误 - SessionId: {}, 错误: {}", 
                sessionId, 
                exception.getMessage());
        
        // 移除异常连接
        safeRemoveSession(sessionId);
    }

    @Override
    public boolean supportsPartialMessages() {
        return false;
    }

    /**
     * 定时发送心跳检测
     * 每45秒执行一次
     */
    @Scheduled(fixedRate = HEARTBEAT_INTERVAL_MS)
    public void sendHeartbeat() {
        if (SESSIONS.isEmpty()) {
            return;
        }
        
        long now = System.currentTimeMillis();
        AtomicInteger successCount = new AtomicInteger(0);
        AtomicInteger failCount = new AtomicInteger(0);
        
        Iterator<Map.Entry<String, WebSocketSession>> iterator = SESSIONS.entrySet().iterator();
        while (iterator.hasNext()) {
            Map.Entry<String, WebSocketSession> entry = iterator.next();
            String sessionId = entry.getKey();
            WebSocketSession session = entry.getValue();
            
            try {
                if (session.isOpen()) {
                    // 发送Ping消息（WebSocket标准心跳帧）
                    session.sendMessage(new PingMessage(ByteBuffer.wrap("heartbeat".getBytes())));
                    successCount.incrementAndGet();
                    log.debug("向客户端 {} 发送心跳", sessionId);
                } else {
                    // 会话已关闭，清理
                    iterator.remove();
                    LAST_ACTIVITY.remove(sessionId);
                    failCount.incrementAndGet();
                }
            } catch (IOException e) {
                log.warn("向客户端 {} 发送心跳失败: {}", sessionId, e.getMessage());
                iterator.remove();
                LAST_ACTIVITY.remove(sessionId);
                failCount.incrementAndGet();
            }
        }
        
        if (successCount.get() > 0 || failCount.get() > 0) {
            log.debug("心跳发送完成 - 成功: {}, 失败: {}", successCount.get(), failCount.get());
        }
    }

    /**
     * 定时检查超时连接
     * 每60秒执行一次
     */
    @Scheduled(fixedRate = 60000)
    public void checkTimeoutSessions() {
        if (SESSIONS.isEmpty()) {
            return;
        }
        
        long now = System.currentTimeMillis();
        AtomicInteger timeoutCount = new AtomicInteger(0);
        
        Iterator<Map.Entry<String, Long>> iterator = LAST_ACTIVITY.entrySet().iterator();
        while (iterator.hasNext()) {
            Map.Entry<String, Long> entry = iterator.next();
            String sessionId = entry.getKey();
            Long lastActive = entry.getValue();
            
            if (lastActive != null && (now - lastActive) > SESSION_TIMEOUT_MS) {
                log.warn("客户端 {} 超时（超过 {} 分钟无活动），断开连接", 
                        sessionId, SESSION_TIMEOUT_MS / 60000);
                safeRemoveSession(sessionId);
                timeoutCount.incrementAndGet();
            }
        }
        
        if (timeoutCount.get() > 0) {
            log.info("清理超时连接: {} 个", timeoutCount.get());
        }
    }

    /**
     * 广播消息给所有客户端
     */
    public void broadcast(String message) {
        if (SESSIONS.isEmpty()) {
            log.debug("没有活跃的WebSocket连接，跳过广播");
            return;
        }
        
        AtomicInteger successCount = new AtomicInteger(0);
        AtomicInteger failCount = new AtomicInteger(0);
        
        Iterator<Map.Entry<String, WebSocketSession>> iterator = SESSIONS.entrySet().iterator();
        while (iterator.hasNext()) {
            Map.Entry<String, WebSocketSession> entry = iterator.next();
            String sessionId = entry.getKey();
            WebSocketSession session = entry.getValue();
            
            try {
                if (session.isOpen()) {
                    session.sendMessage(new TextMessage(message));
                    LAST_ACTIVITY.put(sessionId, System.currentTimeMillis());
                    successCount.incrementAndGet();
                } else {
                    iterator.remove();
                    LAST_ACTIVITY.remove(sessionId);
                    failCount.incrementAndGet();
                }
            } catch (IOException e) {
                log.warn("向客户端 {} 推送消息失败: {}", sessionId, e.getMessage());
                iterator.remove();
                LAST_ACTIVITY.remove(sessionId);
                failCount.incrementAndGet();
            }
        }
        
        log.info("广播消息完成 - 成功: {}, 失败: {}, 当前连接数: {}", 
                successCount.get(), failCount.get(), SESSIONS.size());
    }

    /**
     * 安全移除会话
     */
    private void safeRemoveSession(String sessionId) {
        WebSocketSession session = SESSIONS.remove(sessionId);
        LAST_ACTIVITY.remove(sessionId);
        
        if (session != null && session.isOpen()) {
            try {
                session.close(CloseStatus.SESSION_NOT_RELIABLE);
            } catch (IOException e) {
                log.warn("关闭会话 {} 时发生异常: {}", sessionId, e.getMessage());
            }
        }
    }

    /**
     * 获取当前连接数
     */
    public int getSessionCount() {
        return SESSIONS.size();
    }

    /**
     * 检查会话是否活跃
     */
    public boolean isSessionActive(String sessionId) {
        return SESSIONS.containsKey(sessionId);
    }
}