# 实施方案与阶段计划

本文档是 **当前进度、已知限制、临时假设与阶段计划** 的唯一权威来源。  
目标设计见 [核心设计](./service-registry-core-design.md)、[应用层与集成](./service-registry-application-layer.md)、[Registry Bootstrap Discovery](./service-registry-bootstrap-discovery.md)。  
原型启动与平台排查见 [原型验证 runbook](./service-registry-prototype-validation.md)。

---

## 1. 已确认内容

- 基础网络能力继续复用 EasyTier，不重复造虚拟组网和 NAT 穿透。
- 角色统一使用 `registry / worker / client`（历史文档中的 A/B/C 分别对应三者，不再使用）。
- 服务目录、配置和 ACL 采用“拓扑所有权 + owner 确认链 + 多 registry 最终一致同步”。
- 调用治理只做实例选择与推荐调用方式，不封装业务 RPC。
- 弱网可用性采用“租约 + 应用健康 + 网络信号 + 观察者投票 + 调用反馈”的组合判断。
- 接入形态采用 **薄 SDK + 本地 runtime + 多承载模式**（sidecar / daemon / embedded），契约见 [应用 ↔ Runtime 交互](./service-registry-app-runtime-interaction.md) 与 [应用层](./service-registry-application-layer.md)。
- 首版主要面向 Node.js、Java、.NET 接入；移动端首版只预留边界。

未决分歧见 [待讨论问题](./service-registry-open-questions.md)。

---

## 2. 当前实现进度

截至 2026-07-12：Web 原型完成 **registry 发现 + worker 控制面注册**；**Contracts Shared Project + Sdk + examples 接入骨架**已落地。本地 `/runtime/v1` 与 mode 驱动的 EasyTier 托管策略仍按 [交互契约](./service-registry-app-runtime-interaction.md) 待实现。

### 2.1 已完成

- 托管 `easytier-core`（生成 TOML + `-c`；运行时 `--rpc-portal`）；`easytier-cli` 读 peer/route
- 配置：`EtDiscovery` / `EasyTier` 分离；`Peers` 属 EasyTier
- 角色元数据：仅由 `--roles` 推导，禁止配置覆盖
- registry 定位：`RegistryCandidates` → route metadata registry 位 → `GET /discovery/registry`（不再用“首个 peer”）
- registry 内存目录：upsert / deregister / 查询；`/discovery/services`、`/discovery/select`
- worker 经 HTTP 向 registry 注册；同机 `registry+worker` 可进程内注册；`Services[]` 配置代注册
- Controllers 组织 HTTP；Dockerfile / entrypoint / k8s registry 样例
- 容器入口：`ETDISCOVERY_ROLES`（必填）、`ETDISCOVERY_MODE`（镜像默认 `embedded`；兼容旧 `standalone`→embedded）
- **`EtDiscovery.Contracts`**（Shared Project）+ **`EtDiscovery.Core`**（引擎/宿主）+ **`EtDiscovery.Sdk`**（`AddEtDiscovery` / `UseEtDiscovery` / 心跳 HostedService）
- **`examples/ServiceA|B`**：SDK DI 接入与瘦配置；无跨服务业务调用代码
- **Sdk 单测**（mock HTTP 路径）；Core/Web 既有测试保持通过
- 交互契约文档（结论）：[应用 ↔ Runtime 交互](./service-registry-app-runtime-interaction.md)

### 2.2 接口进度（清单）

语义见 [应用层](./service-registry-application-layer.md#4-应用层-api) 与 [交互契约](./service-registry-app-runtime-interaction.md#4-api-面)。本处只记实现状态。

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
| `PUT /discovery/instances/{instanceId}/lease` | 占位 |
| `PUT /discovery/instances/{instanceId}/health` | 占位 |
| `PUT /discovery/instances/{instanceId}/status` | 占位 |
| `DELETE /discovery/instances/{instanceId}/status` | 占位 |
| `PUT /discovery/instances/{instanceId}/metadata` | 占位 |
| `GET /discovery/instances` | 占位 |
| `GET /discovery/nodes/{nodeId}/instances` | 占位 |
| `PUT /discovery/nodes/{nodeId}/status` | 占位 |
| `DELETE /discovery/nodes/{nodeId}/status` | 占位 |
| `GET/POST/PUT/DELETE /runtime/v1/*` | **未做**（Sdk 已按契约编码） |
| `GET /bootstrap/status` | 未做 |
| watch / reportCallResult 等 | 未做 |

### 2.2b 代码组件进度

| 组件 | 状态 |
| --- | --- |
| `EtDiscovery.Contracts` Shared Project | 已落地 |
| `EtDiscovery.Sdk` + Sdk.Tests | 已落地（对 `/runtime/v1` 的客户端） |
| `examples/ServiceA\|B` | 已落地（DI only） |
| Web 解析 `Mode` / 按 mode 托管 EasyTier | 未做（入口 env 已预置；ProcessManager 仍总是托管） |
| ActiveHeartbeat TTL 管线 | 未做（仍有 worker 周期 upsert 路径） |

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

### 3.1 必须遵守的运维约束

1. **registry 的 `EtDiscovery:ListenUrl` 必须对虚拟网可达**  
   - 正确：`http://0.0.0.0:8080`  
   - 错误：`http://127.0.0.1:8080`（只绑回环，worker 经 VIP 访问会超时）  
   - 启动时会校验并禁止 loopback。
2. **生成的 EasyTier TOML 必须有 listeners**  
   - `Listeners` 为空时写入默认 11010 系；否则公网 peer 可能无法入网。
3. **角色元数据不可手写**  
   - 只由 `--roles` 推导 `node_type_app_id` / `node_type_flags`。
4. **Windows 提权**  
   - 需要本机 VIP/DHCP 时用 `EtDiscovery.Web.exe`（嵌入 manifest）启动；`dotnet xxx.dll` 不会 UAC。
5. **实例名**  
   - registry / worker 建议不同 `EasyTier:InstanceName`，避免同机 CLI `-n` 混淆。

### 3.2 平台与网络

- Windows：需要本机虚拟 IP 时通常要管理员权限（registry / 静态 `Ipv4` / DHCP 均如此）。
- Linux：需要本机虚拟 IP 时须 TUN 可用（root 或 `/dev/net/tun`）。
- `worker + dhcp` 表示 EasyTier 自动分配虚拟 IP，**不是**“registry 当传统 DHCP 服务器”。
- 较稳妥验证路径：registry 固定 `EasyTier.Ipv4` + `ListenUrl=0.0.0.0:8080`；worker 先 DHCP，不稳则写死 VIP。

### 3.3 联调中已修问题（备忘）

| 现象 | 根因 | 处理 |
| --- | --- | --- |
| worker 无 VIP，peer 连 registry 超时 | 仅 `-c` 且 TOML 无 listeners 时 CLI 不补默认 11010 | 空 `Listeners` 写默认监听 |
| `routeMetadataCandidates=0` | `peer list -v` 中 `ipv4_addr.address` 是对象不是 string | route DTO 忽略嵌套地址，只读 `peer_id` / `hostname` / `node_type_*` |
| 访问 `http://10.x.x.x:8080` 超时 | registry 只绑 `127.0.0.1` | 强制非 loopback `ListenUrl` |

更细的启动步骤与排查字段见 [原型验证 runbook](./service-registry-prototype-validation.md)。

### 3.4 配置迁移说明

- 显式 registry 列表字段名为 **`RegistryCandidates`**。
- 旧名 `RegistryPeer` 仅过渡兼容，**下次调整将移除**；新配置请只写 `RegistryCandidates`。
- 不要再文档或配置中使用 `/.well-known/etdiscovery`；registry 元数据路径为 **`GET /discovery/registry`**。

---

## 4. 分阶段推进

### 阶段 0：最小可行性验证 — 已完成

- C# 解决方案、托管 EasyTier、Core 策略抽象、模拟数据测试基线。

### 阶段 1：冻结核心模型 — 基本完成

- 角色/能力模型、核心实体、健康状态机、选择主流程见 [核心设计](./service-registry-core-design.md)。

### 阶段 2：控制面最小闭环 — 部分完成

- 已完成：实例注册/下线/查询/发现/选择（控制面 `/discovery/*`）。
- 未完成：本地 `/runtime/v1/*`；lease / health / status / metadata 行为；watch；调用反馈；mode 驱动 EasyTier 托管。

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

- .NET：`EtDiscovery.Sdk` + Contracts + examples 接入骨架已落地；依赖 Web `/runtime/v1`。
- 统一 runtime 协议（gRPC 主、HTTP 辅）与其它语言 SDK 未开始；见 [应用层](./service-registry-application-layer.md)、[交互契约](./service-registry-app-runtime-interaction.md)。

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

1. 实现 Web **`/runtime/v1/*`**（register / heartbeat / select / resolve）与角色门禁；对接 ActiveHeartbeat TTL。  
2. 实现 **`Mode` 解析** 与 EasyTier 托管策略（daemon 不托管 / sidecar·embedded 捆绑）。  
3. examples 补跨服务调用（在 runtime API 可用后）；Linux/K8s VIP 验证。  
4. 移除 `RegistryPeer`；补齐 lease/health/status/metadata 行为。  
5. `LastKnownRegistries`、`/bootstrap/status`；多语言薄 SDK；评分与反馈。

---

## 7. 待补充资料（文档侧）

从旧 references backlog 迁入，避免参考目录承载项目待办：

- sidecar 生命周期管理最佳实践
- Rust C ABI / .NET PInvoke 封装样例
- Android/iOS TUN 与后台网络限制
- 多区域网络质量感知调度案例
- Actor placement / 弱网目录同步 / 熔断反馈实践

某项资料开始反复引用时，再写入 `service-registry-references/` 作为第三方摘要。
