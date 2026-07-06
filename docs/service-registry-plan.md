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

当前关于 worker 定位 registry 的约定：

- 优先使用显式配置的 `RegistryPeer`
- 如果未配置，则回退到当前 EasyTier 观测里“首个远端可发现 peer”的 `VirtualIp`
- 这一回退逻辑基于当前原型假设：
  - peer 几乎都是 registry
  - 暂不考虑中转服务器等特殊节点类型

## 3. 当前主要分歧点

- Rust 核心之外，控制面和多语言接入的边界如何划分。
- sidecar 与 C ABI 的主次关系以及每种语言的首版选择。
- A 类注册中心是否只做目录聚合，还是允许带兜底代理职责。
- 评分权重和策略是固定内置，还是按服务有限可配置。
- Actor 模式是否纳入首版闭环。

## 4. 分阶段推进建议

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

- 先打通接入，再决定是否优化到更深的原生封装

产出：

- sidecar 协议定义
- Node.js 接入路径
- Java 接入路径
- .NET 双路径验证结论

完成标准：

- 至少两种主流语言能接入原型并稳定上报调用反馈

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

## 5. 首版推荐范围

建议首版只做以下闭环：

- 阶段 0 的最小可行性验证
- A 注册中心最小控制面
- B 服务节点注册、续约、健康上报
- C 调用方发现、选择、结果反馈
- 本地缓存、watch、空保护、简单熔断
- EasyTier peer/route/stun/latency/loss 信息读取

建议暂缓：

- 多 A 强一致
- 完整 Actor placement
- 业务 RPC 代理
- 全语言 SDK
- 移动端正式支持
- 现有注册中心协议兼容
- 深度改造 EasyTier 路由层

## 6. 下一轮细化顺序

建议下一步按这个顺序继续：

1. 先实际启动 `registry / worker` 验证真实注册与发现链路。
2. 再补齐 lease / health / status / metadata 等占位接口的请求响应契约与行为。
3. 随后细化应用层 API 和 gRPC/Spring/Dubbo 集成方式。
4. 最后继续补充评分细节、调用反馈和弱网调度能力。
