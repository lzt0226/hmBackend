# 患者监控系统 API 文档

## 基础信息

- **Base URL**: `http://localhost:8080`
- **Content-Type**: `application/json`
- **Swagger UI**: `http://localhost:8080/swagger`（开发模式）

---

## 一、患者管理 — `/api/patients`

### 1. 获取所有患者

```
GET /api/patients
```

**响应示例**：

```json
[
  {
    "id": 1,
    "name": "张三",
    "age": 65,
    "gender": "男",
    "roomNumber": "301",
    "status": "normal",
    "severity": 0,
    "createdAt": "2026-07-03T19:28:20.9381275",
    "updatedAt": "2026-07-03T19:28:20.9411972"
  }
]
```

**字段说明**：

| 字段 | 类型 | 说明 |
|---|---|---|
| id | long | 患者 ID |
| name | string | 姓名 |
| age | int | 年龄 |
| gender | string | 性别 |
| roomNumber | string | 病房号 |
| status | string | 状态：normal / abnormal |
| severity | int | 异常等级 0-5 |
| createdAt | datetime | 创建时间 |
| updatedAt | datetime | 更新时间 |

---

### 2. 获取单个患者（含行为日志）

```
GET /api/patients/{id}
```

**路径参数**：

| 参数 | 类型 | 说明 |
|---|---|---|
| id | long | 患者 ID |

**响应示例**：

```json
{
  "patient": {
    "id": 1,
    "name": "张三",
    "age": 65,
    "gender": "男",
    "roomNumber": "301",
    "status": "abnormal",
    "severity": 3,
    "createdAt": "2026-07-03T19:28:20.9381275",
    "updatedAt": "2026-07-03T19:30:55.5613309"
  },
  "logs": [
    {
      "id": 2,
      "patientId": 1,
      "behaviorType": "跌倒",
      "description": "患者在病房内跌倒",
      "isAbnormal": 1,
      "recordTime": "2026-07-03T19:30:55.5593"
    },
    {
      "id": 1,
      "patientId": 1,
      "behaviorType": "跌倒",
      "description": "患者在病房内跌倒",
      "isAbnormal": 1,
      "recordTime": "2026-07-03T19:30:52.250744"
    }
  ]
}
```

> 日志最多返回最近 10 条，按时间倒序排列。

---

### 3. 获取异常患者列表（含日志）

```
GET /api/patients/abnormal
```

**说明**：返回所有 `status != "normal"` 的患者，按 severity 降序排列。

**响应格式**：与单个患者接口相同的数组格式。

---

### 4. 更新患者状态

```
PUT /api/patients/{id}/status
Content-Type: application/json
```

**请求体**：

```json
{
  "status": "normal"
}
```

| 字段 | 类型 | 必须 | 说明 |
|---|---|---|---|
| status | string | 是 | 状态值，不能为空。常用值：`normal` / `abnormal` |

**成功响应**：

```
状态更新成功
```

---

### 5. 更新患者异常等级

```
PUT /api/patients/{id}/severity
Content-Type: application/json
```

**请求体**：

```json
{
  "severity": 3
}
```

| 字段 | 类型 | 必须 | 说明 |
|---|---|---|---|
| severity | int | 是 | 异常等级，范围 0-5 |

**成功响应**：

```
异常等级更新成功
```

---

## 二、传感器管理 — `/api/sensor`

### 6. 处理传感器信号

```
POST /api/sensor/signal
Content-Type: application/json
```

**请求体**：

```json
{
  "patientId": 1,
  "behaviorType": "跌倒",
  "description": "患者在病房内跌倒",
  "isAbnormal": true,
  "severity": 3
}
```

**字段说明**：

| 字段 | 类型 | 必须 | 说明 |
|---|---|---|---|
| patientId | long | 是 | 患者 ID |
| behaviorType | string | 是 | 行为类型，如"跌倒"、"心率异常"、"离床"、"呼吸异常"、"体温异常"、"血压异常"、"紧急呼叫"、"长时间静止" |
| description | string | 是 | 行为描述 |
| isAbnormal | bool | 是 | 是否异常行为 |
| severity | int? | 否 | 严重等级 0-5。不传则根据 behaviorType 自动设定默认值 |

**默认严重等级映射**：

| 行为类型 | 默认 Severity |
|---|---|
| 跌倒 | 3 |
| 心率异常 | 2 |
| 离床 | 1 |
| 呼吸异常 | 3 |
| 体温异常 | 2 |
| 血压异常 | 2 |
| 紧急呼叫 | 3 |
| 长时间静止 | 2 |
| 其他 | 1 |

**业务逻辑**：

- 异常信号（`isAbnormal=true`）→ 患者 status 设为 `abnormal`，记录 severity
- 正常信号 + 患者当前非 `abnormal` → 恢复 `normal`，severity 归零
- 正常信号 + 患者当前是 `abnormal` → 不做改变（需人工确认恢复）
- 状态变化时通过 WebSocket 实时广播异常患者列表

**成功响应**：

```
处理成功
```

**WebSocket 推送失败时**：

```
处理成功，但WebSocket推送失败: {错误信息}
```

---

## 三、统计管理 — `/api/stats`

### 7. 获取统计数据

```
GET /api/stats
```

**响应示例**：

```json
{
  "totalPatients": 5,
  "abnormalPatients": 1,
  "todayAbnormalRecords": 3
}
```

**字段说明**：

| 字段 | 类型 | 说明 |
|---|---|---|
| totalPatients | long | 患者总数 |
| abnormalPatients | long | 当前异常患者数 |
| todayAbnormalRecords | long | 今日异常记录数 |

---

## 四、WebSocket 实时推送

### 连接

```
ws://localhost:8080/ws/monitor
```

### 协议

| 项目 | 值 | 说明 |
|---|---|---|
| 心跳间隔 | 45 秒 | 服务端发送文本 `ping` |
| 心跳回复 | 客户端回复 `pong` | 大小写不敏感 |
| 超时断开 | 5 分钟 | 无任何活动则自动断开 |
| 推送触发 | 患者状态变化时 | 全量广播异常患者列表 |

### 推送消息格式

与 `GET /api/patients/abnormal` 接口返回格式完全一致：

```json
[
  {
    "patient": { /* Patient 对象 */ },
    "logs": [ /* BehaviorLog 数组，最多 10 条 */ ]
  }
]
```

---

## 五、错误响应

### 格式

```json
{
  "code": 500,
  "message": "错误描述"
}
```

### 常见错误

| 状态码 | code | 场景 |
|---|---|---|
| 404 | 404 | 患者不存在 |
| 400 | 400 | 请求参数校验失败 |
| 500 | 500 | 服务器内部错误 |
