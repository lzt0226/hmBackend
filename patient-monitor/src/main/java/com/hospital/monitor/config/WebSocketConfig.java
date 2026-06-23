package com.hospital.monitor.config;

import com.hospital.monitor.websocket.MonitorWebSocketHandler;
import lombok.RequiredArgsConstructor;
import lombok.extern.slf4j.Slf4j;
import org.springframework.context.annotation.Configuration;
import org.springframework.http.server.ServerHttpRequest;
import org.springframework.http.server.ServerHttpResponse;
import org.springframework.http.server.ServletServerHttpRequest;
import org.springframework.web.socket.WebSocketHandler;
import org.springframework.web.socket.config.annotation.EnableWebSocket;
import org.springframework.web.socket.config.annotation.WebSocketConfigurer;
import org.springframework.web.socket.config.annotation.WebSocketHandlerRegistry;
import org.springframework.web.socket.server.HandshakeInterceptor;

import java.util.Map;

@Slf4j
@Configuration
@EnableWebSocket
@RequiredArgsConstructor
public class WebSocketConfig implements WebSocketConfigurer {

    private final MonitorWebSocketHandler monitorWebSocketHandler;

    @Override
    public void registerWebSocketHandlers(WebSocketHandlerRegistry registry) {
        registry.addHandler(monitorWebSocketHandler, "/ws/monitor")
                .setAllowedOriginPatterns("*")
                .addInterceptors(new HandshakeInterceptor() {
                    @Override
                    public boolean beforeHandshake(ServerHttpRequest request, ServerHttpResponse response,
                                                   WebSocketHandler wsHandler, Map<String, Object> attributes) throws Exception {
                        // 记录客户端信息
                        String clientIp = getClientIp(request);
                        attributes.put("clientIp", clientIp);
                        attributes.put("connectTime", System.currentTimeMillis());
                        log.info("WebSocket 握手请求 - IP: {}, URI: {}", clientIp, request.getURI());
                        return true;
                    }

                    @Override
                    public void afterHandshake(ServerHttpRequest request, ServerHttpResponse response,
                                               WebSocketHandler wsHandler, Exception exception) {
                        if (exception != null) {
                            log.error("WebSocket 握手失败: {}", exception.getMessage());
                        } else {
                            log.info("WebSocket 握手成功");
                        }
                    }
                    
                    private String getClientIp(ServerHttpRequest request) {
                        if (request instanceof ServletServerHttpRequest servletRequest) {
                            String forwardedFor = servletRequest.getServletRequest().getHeader("X-Forwarded-For");
                            if (forwardedFor != null && !forwardedFor.isEmpty()) {
                                return forwardedFor.split(",")[0].trim();
                            }
                            return servletRequest.getServletRequest().getRemoteAddr();
                        }
                        return request.getRemoteAddress() != null ? 
                               request.getRemoteAddress().getAddress().getHostAddress() : "unknown";
                    }
                });
    }
}
