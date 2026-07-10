# 实施方案与阶段计划

本文档用于沉淀已确认内容、分歧点和阶段推进顺序，避免下一步细化时反复回到同一层面。

## 1. 已确认内容

- 基础网络能力继续复用 EasyTier，不重复造虚拟组网和 NAT 穿透。
- 当前原型角色统一使用 `registry / worker / client`，不再继续使用 `A/B/C` 简写。
- 服务目录、配置和 ACL 采用“拓扑所有权 + owner 确认链 + 多 A 最终一致同步”。
- 调用治理只做实例选择与推荐调用方式，不封装业务 RPC。
- 弱网可用性采用“租约 + 应用健康 + 网络信号 + 观察者投票 + 调用反馈”的组合判断。
- 首版主要面向 Node.js、Java、.NET 接入。
- 移动端首版只预留边界，不做正式落地。

## 2. 当前实现进度

截至本轮，已经完成的内容：

- 文档参考信息已补充 Consul、Nacos、Eureka、Kubernetes EndpointSlice 的注册/下线/状态接口风格。
- `registry` 侧已实现内存实例注册表：
  - 按 `instanceId` upsert
  - deregister
  - 单实例查询
  - 列表查询
- `worker` 已实现基于 HTTP 的主动注册/下线。
- `registry + worker` 同机时，已改为直接调用进程内注册逻辑，不走 HTTP 回环。
- `/discovery/services` 已切换为返回真实已注册实例，而不是固定配置拼装结果。
- `GET /discovery/select` 已切换到从真实实例列表里选择。
- 配置已从单服务字段切换到 `Services[]`：
  - `ServiceName`
  - `Port`
  - `Protocol`
  - 可选 `InstanceId / Version / Group / Tags / Metadata / Weight`
- worker 侧已增加 registry 定位配置：
  - `RegistryPeer`
    - 可填写 registry 的虚拟 IP
    - 也可填写 MagicDNS/DDNS 对应的绝对 URL
- 当前内存状态模型已经明确为：
  - 内存里维护一份共享数据源
  - 写入侧允许并发更新
  - 读取侧统一读取其瞬时快照
  - 接受短时间内两次读取结果不同

当前仍为占位、尚未实现完整行为的接口：

- `PUT /discovery/instances/{instanceId}/lease`
- `PUT /discovery/instances/{instanceId}/health`
- `PUT /discovery/instances/{instanceId}/status`
- `DELETE /discovery/instances/{instanceId}/status`
- `PUT /discovery/instances/{instanceId}/metadata`
- `GET /discovery/instances`
- `GET /discovery/nodes/{nodeId}/instances`
- `PUT /discovery/nodes/{nodeId}/status`
- `DELETE /discovery/nodes/{nodeId}/status`

该实现阶段不追求的能力：

- 不追求对 discovery 数据提供强一致读视图
- 不追求“节点观测、实例注册、服务选择”三者之间的事务级同步
- 不要求一次响应中的每个字段都严格代表同一物理时刻的底层网络事实

当前关于 worker/client 定位 registry 的约定：

- 早期原型优先使用显式配置的 `RegistryPeer`
- 如果未配置，则回退到当前 EasyTier 观测里“首个远端可发现 peer”的 `VirtualIp`
- 这一回退逻辑仅适合最小真实链路验证，基于以下临时假设：
  - peer 几乎都是 registry
  - 暂不考虑中转服务器等特殊节点类型
- 后续应按 [Registry Bootstrap Discovery 设计草案](./service-registry-bootstrap-discovery.md) 替换为：
  - `RegistryCandidates` 显式候选列表
  - EasyTier `RoutePeerInfo.node_type_flags` 官方能力标记
  - EasyTier `RoutePeerInfo.node_type_app_id` 应用命名空间
  - `/.well-known/etdiscovery` registry 标准 API 明细声明
  - `LastKnownRegistries` 本地缓存
  - 从 EasyTier peer 虚拟 IP 自动探测 registry 作为旧版本兼容 fallback

## 3. 当前主要分歧点

- Rust 核心之外，控制面和多语言接入的边界如何划分。
- sidecar 与 C ABI 的主次关系以及每种语言的首版选择。
- A 类注册中心是否只做目录聚合，还是允许带兜底代理职责。
- 评分权重和策略是固定内置，还是按服务有限可配置。
- Actor 模式是否纳入首版闭环。

## 4. 系统运行模式与 SDK 边界建议

这一轮建议把“部署形态”和“应用接入契约”解耦，避免因为当前 sidecar 原型跑通，就把 SDK 等同于“一个必须随应用同进同出的子进程”。

### 4.1 先冻结的边界

建议先冻结如下分层：

- 应用进程边界
  - 负责提供业务身份、服务定义、业务健康信号、调用反馈。
  - 保留对 HTTP/gRPC/TCP 等现有业务协议栈的控制权。
  - 不直接感知 EasyTier 路由细节，也不承担 registry bootstrap 逻辑。
- 本地 runtime 边界
  - 负责 registry 定位、本地缓存、watch、实例选择、反馈汇总、诊断输出。
  - 负责与 EasyTier runtime 建立控制桥，读取 route/peer/link 信号。
  - 可选负责拉起、托管、复用本机 EasyTier runtime。
- registry/control-plane 边界
  - 负责目录聚合、状态整合、策略下发、审计与跨节点视图。
  - 不侵入业务进程内调用栈，不要求接管业务连接池和序列化。

核心约束是：

- SDK 负责“注册、发现、选择、反馈”的稳定契约。
- runtime 负责“网络与缓存复杂度”。
- 业务框架继续负责“真正发请求”。

### 4.2 EtDiscovery 更接近哪类中间件

参考现有中间件的集成方式，可以把 EtDiscovery 放在 APM 与 service mesh 之间：

- 它比 service mesh 更贴近应用。
  - 因为服务身份、业务健康、实例元数据、调用反馈都来自应用语义，不能只靠透明转发层推断。
- 它又比 APM 更应该保持松耦合。
  - 因为它不需要强行侵入每个调用栈、字节码或框架拦截器，也不需要接管业务 RPC 编解码。
- 在 Kubernetes 里，边界会被 deployment 方式弱化，但契约不应变化。
  - Pod 内 sidecar、Node 上 daemon、宿主机常驻进程，都只是 runtime 承载位置不同。
  - 应用看到的仍应是同一套注册/发现/反馈 API。

因此，EtDiscovery 不宜做成“强绑定某语言框架的深嵌 SDK”，也不宜退化成“完全透明、无应用参与的纯网络代理”。更合适的定位是“薄 SDK + 本地 runtime”的双层模式。

### 4.3 建议支持的运行模式矩阵

建议把运行模式明确为同一逻辑面的不同承载方式：

- Mode A：sidecar 容器 / sidecar 进程
  - 适合 Kubernetes、容器化微服务、Node.js、Java。
  - 应用通过本地 HTTP/gRPC/Unix Socket 与 runtime 通信。
  - 首版最适合作为主推模式。
- Mode B：host daemon / slim daemon
  - 适合 VM、物理机、多进程共享宿主机 runtime 的场景。
  - 一个节点上的多个业务进程复用同一个 runtime。
  - 适合参考 Dapr 的 slim/self-hosted 经验，降低“一应用一 sidecar”的运维成本。
- Mode C：embedded runtime（C ABI/FFI）
  - 适合 Rust、C/C++、少数对本地调用时延和进程内状态共享敏感的场景。
  - 不应作为首版所有语言的统一主路径，而应作为性能/封装深度优化项。
- Mode D：no-SDK / minimal integration
  - 适合只做运维验证、脚本接入、现有系统渐进迁移。
  - 通过标准 HTTP/gRPC 接口直接注册、查询或上报，不要求语言专属封装。

这四种模式的关键不是提供四套能力，而是共享：

- 同一份应用层 API 语义
- 同一份实例/节点/反馈数据模型
- 同一份 sidecar/runtime 控制协议

### 4.4 对 Dapr 的可借鉴点

Dapr 值得借鉴的不是某个单独部署形态，而是“先定义 runtime API，再允许多种托管方式”的思路：

- 应用看到的是稳定 API，而不是具体部署拓扑。
- sidecar 不是唯一形态，daemon/self-hosted 同样可用。
- 可以先通过标准 HTTP/gRPC 接入，再按语言逐步补 SDK 糖衣。
- 即使没有语言 SDK，也能完成最小可用接入。

对 EtDiscovery 的启发建议落成以下原则：

- 先冻结 runtime 对应用暴露的协议面，再讨论它跑在 sidecar、daemon 还是进程内。
- 语言 SDK 首版尽量做薄封装，不把核心状态机复制到各语言里。
- sidecar 原型演进为 SDK 时，SDK 主要负责：
  - 参数组织与默认值
  - 本地缓存访问
  - watch 回调与重连包装
  - 与业务框架的轻度适配
- runtime 继续承载：
  - EasyTier 连接与生命周期
  - registry bootstrap discovery
  - 评分与实例选择
  - 网络诊断与调试接口

### 4.5 首版推荐主次关系

结合当前进展，建议首版明确主次，而不是同时把两条路线都做到同等完备：

- 主路径：同一份 runtime 代码 + 角色分支运行 + sidecar/daemon 承载 + 薄语言 SDK
  - Node.js、Java 首版都走这条路。
  - .NET 也先走这条保底路径，确保跨平台和部署体验先成立。
  - `registry` 与 `worker` 优先复用同一份 EtDiscovery runtime 代码，通过配置或启动参数决定节点角色。
  - 业务服务不嵌入完整状态机，只通过本地 SDK 调用本地 runtime 暴露的 HTTP/gRPC 接口。
- 次路径：C ABI 作为 runtime 内核复用与特定语言优化出口
  - Rust/C/C++ 可优先直连。
  - .NET 可在 sidecar 跑通后，再评估是否追加 PInvoke 深封装。
- 不建议首版做成：
  - 各语言都各自实现一套完整进程内状态机
  - sidecar 与 embedded 分别演化出两套不兼容 API
  - 把 EasyTier runtime 生命周期强绑到所有接入方式里

更具体地说，首版建议把“SDK 边界”定义为：

- 业务可见 API：`register / renew / deregister / resolve / select / watch / report`
- runtime 可见 API：bootstrap、cache、policy、diagnostics、EasyTier bridge
- C ABI 边界：优先服务 runtime 内部复用和少数高性能嵌入场景，不直接决定所有语言产品形态

建议再明确一条首版代码组织原则：

- `registry` 与 `worker` 不是两套与业务绑定的独立产品，而是同一份 runtime 程序在不同节点角色下的运行分支。
- 业务语言 SDK 首版主要是“本地 runtime client wrapper”，而不是“把 EtDiscovery 核心逻辑移植到各语言”。
- SDK 的大多数实现都应是网络请求封装、返回模型映射、watch 回调包装和少量本地缓存。
- 业务 RPC 仍由应用自己的 HTTP/gRPC/TCP client 直接发往选中的 `virtual_ip:port` 或 `recommended_endpoint`。

### 4.6 当前可直接进入实现的结论

如果按这个方向推进，阶段 4 的工作可进一步细化为：

- 先定义统一 runtime 协议
  - gRPC 作为主协议
  - HTTP/JSON 作为调试、无 SDK 接入和运维协议
- 再定义统一运行模式约束
  - sidecar、daemon、embedded 共享同一套实例模型与错误码
- 最后才为各语言补薄 SDK
  - Node.js：优先封装服务注册、查询、watch、反馈
  - Java：优先提供与 Spring/Dubbo 风格兼容的适配层
  - .NET：先封装 sidecar client，再决定是否补 C ABI 直连

可以先把最小运行闭环固定为：

- 一台设备/容器运行 `registry` 角色
- 多台靠近应用的设备/容器运行 `worker` 角色
- 业务进程通过本地 SDK 请求本地 `worker`
- `worker` 负责 registry 定位、服务选择、EasyTier 信号整合
- 业务进程拿到 `SelectedInstance` 后直连目标实例的业务地址

这样可以把当前已跑通的 sidecar 原型，顺势提升为“默认 runtime 承载方式”，而不是过早把它固化成唯一产品形态。

## 5. 分阶段推进建议

### 阶段 0：最小可行性验证

目标：

- 用最小范围验证 C# 原型方案、EasyTier 进程托管和策略抽象是否可行

产出：

- `etdiscovery/` 下的 C# 独立解决方案
- A/B 角色原型 Web 项目
- A 侧算法逻辑类库
- 模拟数据测试项目
- EasyTier 连接实例读取和最小服务选择能力
- 多角色节点与三组互斥能力清单的运行时约束

完成标准：

- 能托管 `easytier-core` 并完成最小启停
- 能在不依赖真实网络环境的前提下验证节点处理和轮询选择
- 能为后续双机组网验证提供可部署原型基线

当前状态：

- 已完成

### 阶段 1：冻结核心模型

目标：

- 把不会轻易变化的设计先稳定下来

产出：

- A/B/C 角色模型
- 核心实体定义
- 健康状态机
- 拓扑所有权与确认链模型
- 实例选择主流程

完成标准：

- 核心设计文档可以单独阅读，不依赖语言和框架讨论

### 阶段 2：原型化控制面闭环

目标：

- 做最小可运行的注册、续约、发现闭环

产出：

- A 类注册中心最小原型
- B/C SDK 最小注册与查询原型
- 本地缓存与 watch 原型
- `selectOneHealthyInstance` 原型

完成标准：

- 至少支持单区域、少量节点的服务注册与发现

当前状态：

- 已完成“实例注册/下线/查询/发现/选择”的最小闭环
- 尚未完成 lease/health/status/metadata 等辅助管理接口
- 尚未完成 registry bootstrap discovery，当前仍依赖显式配置或早期 peer fallback

### 阶段 2.5：Registry Bootstrap Discovery

目标：

- 让 B/C 节点加入 EasyTier 网络后，能够像 DHCP 获取网络基础设施配置一样自动找到 registry。
- 替换 `peers[0]` 作为 registry 的临时假设。

产出：

- EasyTier `RoutePeerInfo.node_type_flags` 官方能力标记
- EasyTier `RoutePeerInfo.node_type_app_id` 应用命名空间
- `Bootstrap.RegistryCandidates` 配置
- `/.well-known/etdiscovery` registry 声明接口
- 基于 route metadata 的候选 registry 筛选与选择策略
- `LastKnownRegistries` 本地缓存
- `/bootstrap/status` 调试接口
- worker/client 统一 registry 定位流程

完成标准：

- worker 未配置具体 registry IP 时，可以根据 EasyTier route metadata 中的 EtDiscovery 应用标记自动完成注册。
- client 未配置具体 registry IP 时，可以根据 EasyTier route metadata 中的 EtDiscovery 应用标记自动找到 registry 并完成服务查询。
- 找不到 registry 时，`/health` 或 `/bootstrap/status` 能给出明确 blocking reason。

### 阶段 3：补齐弱网调度能力

目标：

- 让实例选择不再只是“在线/离线”二值判断

产出：

- 可用性评分实现
- 怀疑票与观察者探测实现
- 空保护与简单熔断
- NAT、链路质量和拓扑距离进入评分

完成标准：

- 在控制面断连、跨区域抖动、P2P 不稳定场景下仍能给出稳定候选

### 阶段 4：明确多语言接入路径

目标：

- 先打通统一 runtime 协议与默认运行模式，再决定是否优化到更深的原生封装

产出：

- runtime 协议定义
- sidecar / daemon / embedded 运行模式约束
- Node.js 接入路径
- Java 接入路径
- .NET 双路径验证结论

完成标准：

- 至少两种主流语言能通过同一 runtime 协议接入原型并稳定上报调用反馈
- sidecar 与 daemon 形态不改变应用层 API 语义

### 阶段 5：应用层适配与扩展

目标：

- 让现有业务能低成本迁移进来

产出：

- gRPC 接入方案
- Spring/Dubbo 风格适配方案
- HTTP/TCP 自定义客户端样例
- 移动端预留设计说明

完成标准：

- 业务方能在不更换原 RPC 栈的前提下使用 etdiscovery

## 6. 首版推荐范围

建议首版只做以下闭环：

- 阶段 0 的最小可行性验证
- A 注册中心最小控制面
- B 服务节点注册、续约、健康上报
- C 调用方发现、选择、结果反馈
- 同一份 runtime 代码支撑 `registry / worker` 两种角色
- sidecar/daemon 统一 runtime 协议
- 本地缓存、watch、空保护、简单熔断
- EasyTier peer/route/stun/latency/loss 信息读取

建议暂缓：

- 多 A 强一致
- 完整 Actor placement
- 业务 RPC 代理
- 全语言深封装 SDK
- 移动端正式支持
- 现有注册中心协议兼容
- 深度改造 EasyTier 路由层

## 7. 下一轮细化顺序

建议下一步按这个顺序继续：

1. 先实际启动 `registry / worker` 验证真实注册与发现链路。
2. 再补齐 lease / health / status / metadata 等占位接口的请求响应契约与行为。
3. 随后冻结 runtime 协议面，以及 sidecar / daemon / embedded 三种运行模式共享的数据模型。
4. 再细化应用层 API 和 gRPC/Spring/Dubbo 集成方式。
5. 最后继续补充评分细节、调用反馈和弱网调度能力。
