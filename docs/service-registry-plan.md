# 实施方案与阶段计划

本文档是 **当前进度、已知限制、临时假设与阶段计划** 的唯一权威来源。  
目标设计见 [核心设计](./service-registry-core-design.md)、[应用层与集成](./service-registry-application-layer.md)、[Registry Bootstrap Discovery](./service-registry-bootstrap-discovery.md)。  
原型启动与平台排查见 [原型验证 runbook](./service-registry-prototype-validation.md)。  
**改代码时的模块入口与实现约定**见根目录 [AGENTS.md](../AGENTS.md)（不在此维护项目树）。

---

## 1. 已确认内容

- 基础网络能力继续复用 EasyTier，不重复造虚拟组网和 NAT 穿透。
- 角色统一使用 `registry / worker / client`（历史文档中的 A/B/C 分别对应三者，不再使用）。
- 服务目录、配置和 ACL 采用“拓扑所有权 + owner 确认链 + 多 registry 最终一致同步”。
- 调用治理只做实例选择与推荐调用方式，不封装业务 RPC。
- 弱网可用性采用“租约 + 应用健康 + 网络信号 + 观察者投票 + 调用反馈”的组合判断。
- 接入形态：**独立 `EtDiscovery.Runtime` 进程**承载 **registry**；**持有 Sdk ⇔ worker/client**；ActiveRenewal = 注册+续约同一 upsert。节点辅助可选；**仅 `embedded` 进程内托管 EasyTier**。
- 首版主要面向 Node.js、Java、.NET 接入；移动端首版只预留边界。

未决分歧见 [待讨论问题](./service-registry-open-questions.md)。

---

## 2. 当前实现进度

截至 2026-07-16：**契约已纠偏**；**`EtDiscovery.Runtime` 重命名已完成**。Runtime 原型仍是 **registry 发现 + `Services[]` worker 代注册**；Sdk/examples 仍指向 **已废止的 `/runtime/v1` 骨架**。Mode 解析与 `ManagesEasyTierProcess` 未实现（ProcessManager 仍总是托管）。

### 2.1 已完成

- 托管 `easytier-core`（生成 TOML + `-c`；运行时 `--rpc-portal`）；`easytier-cli` 读 peer/route（**尚未**按 mode 关闭托管）
- 配置：`EtDiscovery` / `EasyTier` 分离；`Peers` 属 EasyTier
- 角色元数据：仅由 `--roles` 推导，禁止配置覆盖
- registry 定位：`RegistryCandidates` → route metadata registry 位 → `GET /discovery/registry`（不再用“首个 peer”）
- registry 内存目录：upsert / deregister / 查询；`/discovery/services`、`/discovery/select`
- 原型 worker：`Services[]` + `WorkerRegistrationOrchestrator` 代注册（**与终态 C2b 冲突，待收敛**）
- Controllers 组织 HTTP；Dockerfile / entrypoint / k8s registry 样例
- 容器入口：`ETDISCOVERY_ROLES`（必填）、`ETDISCOVERY_MODE`（镜像默认 `embedded`；兼容旧 `standalone`→embedded）
- **`EtDiscovery.Contracts`** + **`EtDiscovery.Core`** + **`EtDiscovery.Sdk`**（DI/心跳骨架；**HTTP 目标仍为过时 `/runtime/v1`**）
- **`examples/ServiceA|B`**：SDK DI 骨架；非终态联调
- **Sdk 单测**（mock 旧路径）；Core/Runtime 既有测试保持通过
- **应用接入文档**（原 application-layer + app-runtime-interaction **已合并**；SDK 调用模式、Mode、ActiveRenewal）

### 2.2 接口进度（清单）

语义见 [应用接入 §4 API](./service-registry-application-layer.md#4-api-面)。本处只记实现状态。

| HTTP | 状态 |
| --- | --- |
| `GET /health` | 已实现 |
| `GET /easytier/peers` | 已实现 |
| `GET /test/ping` | 已实现 |
| `GET /discovery/registry` | 已实现 |
| `POST /discovery/instances` | 已实现 |
| `DELETE /discovery/instances/{instanceId}` | 已实现 |
| `GET /discovery/instances/{instanceId}` | 已实现 |
| `GET /discovery/services` | 已实现 |
| `GET /discovery/select` | 已实现（仅 registry） |
| `PUT /discovery/instances/{instanceId}/lease` | 占位；**非主路径**（ActiveRenewal 用 POST upsert） |
| `PUT /discovery/instances/{instanceId}/health` | 占位；存活由 upsert 覆盖 |
| `PUT /discovery/instances/{instanceId}/status` | 占位 |
| `DELETE /discovery/instances/{instanceId}/status` | 占位 |
| `PUT /discovery/instances/{instanceId}/metadata` | 占位 |
| `GET /discovery/instances` | 占位 |
| `GET /discovery/nodes/{nodeId}/instances` | 占位 |
| `PUT /discovery/nodes/{nodeId}/status` | 占位 |
| `DELETE /discovery/nodes/{nodeId}/status` | 占位 |
| `GET/POST/PUT/DELETE /runtime/v1/*` | **错误草案，不作为目标**；勿实现为主路径 |
| `GET /bootstrap/status` | 未做 |
| watch / reportCallResult 等 | 未做 |
| SDK 直连控制面 ActiveRenewal（POST upsert）+ select | **未做**（终态主路径） |
| `EtDiscovery.Web` → `EtDiscovery.Runtime` 重命名 | **已完成** |

### 2.2b 代码组件进度

| 组件 | 状态 |
| --- | --- |
| `EtDiscovery.Contracts` Shared Project | 已落地 |
| `EtDiscovery.Sdk` + Sdk.Tests | 骨架已落地；**目标改为控制面客户端（待改）** |
| `examples/ServiceA\|B` | DI 骨架；待对齐终态 |
| 应用接入文档（合并后） | **已纠偏冻结**（2026-07-16） |
| Runtime 解析 `Mode` / `ManagesEasyTierProcess` 仅 embedded | 未做（ProcessManager 仍总是托管） |
| 弱化 `Services[]` 代注册 Healthy | 未做 |
| ActiveHeartbeat TTL 管线（业务 SDK → 控制面） | 未做 |

### 2.3 Bootstrap 相关进度

设计见 [Registry Bootstrap Discovery](./service-registry-bootstrap-discovery.md)。

已落地：

- EasyTier `RoutePeerInfo.node_type_flags` / `node_type_app_id` 透传
- `RegistryCandidates` + route metadata 发现 + `GET /discovery/registry`
- 角色 → node_type 广播（不可配置覆盖）

尚未实现：

- `LastKnownRegistries` 本地缓存
- `/bootstrap/status`、`POST /bootstrap/refresh`
- 全量 peer HTTP 扫描 fallback
- 声明签名与强安全校验
- 多 registry 协同

### 2.4 本阶段不追求

- discovery 数据的强一致读视图
- “节点观测、实例注册、服务选择”的事务级同步
- 响应中每个字段严格同一物理时刻

---

## 3. 当前限制与运维假设

实现与联调硬约束摘要统一维护在 [AGENTS.md §3](../AGENTS.md#3-代码与行为硬约定)，含 ListenUrl、listeners、角色元数据、权限、DHCP、RegistryCandidates 等。
启动步骤与观测字段见 [runbook](./service-registry-prototype-validation.md)。

### 3.1 联调已修问题（备忘，防回归）

| 现象 | 根因 | 处理 |
| --- | --- | --- |
| worker 无 VIP，peer 连 registry 超时 | 仅 `-c` 且 TOML 无 listeners 时 CLI 不补默认 11010 | 空 `Listeners` 写默认监听 |
| `routeMetadataCandidates=0` | `peer list -v` 中 `ipv4_addr.address` 是对象不是 string | route DTO 忽略嵌套地址，只读 `peer_id` / `hostname` / `node_type_*` |
| 访问 `http://10.x.x.x:8080` 超时 | registry 只绑 `127.0.0.1` | 强制非 loopback `ListenUrl` |

### 3.2 配置迁移（进度相关）

- 显式 registry 列表：**`RegistryCandidates`**；旧名 `RegistryPeer` 过渡兼容，**计划移除**。  
- Registry 元数据路径：**`GET /discovery/registry`**（勿再用 `/.well-known/etdiscovery`）。

---

## 4. 分阶段推进

### 阶段 0：最小可行性验证 — 已完成

- C# 解决方案、托管 EasyTier、Core 策略抽象、模拟数据测试基线。

### 阶段 1：冻结核心模型 — 基本完成

- 角色/能力模型、核心实体、健康状态机、选择主流程见 [核心设计](./service-registry-core-design.md)。

### 阶段 2：控制面最小闭环 — 部分完成

- 已完成：实例注册/下线/查询/发现/选择（控制面 `/discovery/*`）；契约侧明确 **SDK → 控制面**。
- 未完成：Sdk 直连控制面；lease / health / status / metadata 行为；watch；调用反馈；Mode 二元托管；去掉代注册 Healthy。

### 阶段 2.5：Registry Bootstrap Discovery — 主路径已联调

目标：worker/client 入网后自动找到 registry，替代“首个 peer 当 registry”。

产出对照：

| 产出 | 状态 |
| --- | --- |
| `node_type_flags` / `node_type_app_id` | 已有 |
| `RegistryCandidates` | 已有（`RegistryPeer` 待删） |
| `GET /discovery/registry` | 已有 |
| route metadata 候选筛选 | 已有 |
| `LastKnownRegistries` | 未做 |
| `/bootstrap/status` | 未做 |

### 阶段 3：弱网调度能力 — 未开始

- 可用性评分、怀疑票、空保护、NAT/链路质量进评分。

### 阶段 4：多语言接入 — 部分开始

- .NET：`EtDiscovery.Sdk` + Contracts + examples 骨架已落地；**须迁移为控制面客户端**（废除 `/runtime/v1` 目标）。
- 统一控制面协议（gRPC 主、HTTP 辅）与其它语言 SDK 未开始；见 [应用接入](./service-registry-application-layer.md)。

### 阶段 5：框架适配与扩展 — 未开始

- gRPC / Spring / Dubbo 适配；移动端仅预留。

---

## 5. 首版推荐范围

建议纳入：

- 阶段 0–2.5 已验证路径
- `registry` 最小控制面 + `worker` 注册/发现/选择
- 同一份 runtime 多角色
- 本地缓存、watch、空保护、简单熔断（待实现）
- EasyTier peer/route 观测读入

建议暂缓：

- 多 registry 强一致
- 完整 Actor placement
- 业务 RPC 代理
- 全语言深封装 SDK
- 移动端正式支持
- 现有注册中心协议兼容
- 深度改造 EasyTier 路由层

---

## 6. 下一轮工作顺序

1. **（文档已完成）** 边界与接入契约；**应用层 + 交互契约已合并**为 [application-layer](./service-registry-application-layer.md)。  
2. **（已完成）** `EtDiscovery.Web` → `EtDiscovery.Runtime` 全量重命名。  
3. 实现 **`Mode` 解析** 与 **`ManagesEasyTierProcess = embedded only`**；daemon/sidecar 仅连接观测；条件必填。  
4. **Sdk 改为控制面客户端**：周期 **`POST /discovery/instances` upsert（ActiveRenewal）** + select；合并/废弃双轨 Register+Heartbeat；TTL。  
5. **弱化/移除** 无 Sdk 的 `Services[]` 代注册 Healthy；准单体须进程内 Sdk 自注册。  
6. examples：业务仅 Sdk + 独立 Runtime registry；Linux/K8s VIP 验证。  
7. 移除 `RegistryPeer`；status/metadata；`LastKnownRegistries`；多语言 SDK；评分与反馈。

---

## 7. 待补充资料（文档侧）

从旧 references backlog 迁入，避免参考目录承载项目待办：

- sidecar 生命周期管理最佳实践
- Rust C ABI / .NET PInvoke 封装样例
- Android/iOS TUN 与后台网络限制
- 多区域网络质量感知调度案例
- Actor placement / 弱网目录同步 / 熔断反馈实践

某项资料开始反复引用时，再写入 `service-registry-references/` 作为第三方摘要。
