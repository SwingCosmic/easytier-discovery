# Bootstrap、Membership 与服务发现参考

本文档补充与 EtDiscovery registry bootstrap 机制直接相关的外部系统资料。关注点不是复制某个系统，而是提炼“节点如何发现控制面、如何声明能力、如何区分节点健康与服务健康”。

## 1. DHCP

适合借鉴：

- 设备加入网络后，通过 discover / offer / request / ack 获得 IP、网关、DNS 等基础配置。
- 租约模型天然支持过期、续约和重新协商。
- 使用者不需要预先知道网络内部所有基础设施地址。

不直接照搬：

- EtDiscovery 不负责虚拟 IP 分配，虚拟 IP 仍由 EasyTier 处理。
- EtDiscovery 的 registry discovery 可以先用 HTTP 探测，不需要实现 L2 广播协议。

对本方案启发：

- registry bootstrap 应成为节点入网后的基础设施发现流程。
- B/C 只需要知道如何加入 EasyTier 网络，不应该必须知道唯一 registry IP。
- registry 选择结果应有租约和缓存，而不是一次性静态配置。

参考：

- [RFC 2131: Dynamic Host Configuration Protocol](https://www.rfc-editor.org/rfc/rfc2131)

## 2. Kubernetes EndpointSlice

适合借鉴：

- EndpointSlice 以 endpoint 列表表达服务后端，而不是把服务可用性简化成一个布尔值。
- endpoint 条件包含 `ready`、`serving`、`terminating` 等字段，适合表达“是否可接流量”和“是否正在排空”的差异。
- endpoint 可以携带 node、zone 等拓扑信息。

不直接照搬：

- 不引入 Kubernetes 的声明式控制器、资源版本和 reconciliation 全套机制。
- EtDiscovery 的服务实例必须绑定 EasyTier 虚拟 IP，而不是任意 Pod IP。

对本方案启发：

- 服务实例应绑定节点虚拟 IP，但健康、排空、可服务状态需要拆分。
- registry bootstrap 找到的是控制面入口，服务选择时仍应返回具体实例 endpoint。
- 后续多地域选择可以借鉴 node/zone/locality 信息。

参考：

- [Kubernetes EndpointSlices](https://kubernetes.io/docs/concepts/services-networking/endpoint-slices/)

## 3. Consul Agent 与 Health Check

适合借鉴：

- agent 负责本地服务注册、健康检查和上报，中心侧负责目录视图。
- service check 与 node check 可以分离。
- TTL check 适合表达“节点/服务需要持续续约，否则降级”的语义。
- 多数据中心和 prepared query 展示了“查询时按策略选目标”的设计方向。

不直接照搬：

- Consul 默认面向数据中心和 agent 常驻部署，EtDiscovery 需要适配更弱、更动态的 EasyTier 网络。
- EtDiscovery 当前不要求部署完整 agent 集群。

对本方案启发：

- B 节点的 EtDiscovery sidecar/Web 进程可以承担类似 agent 的职责。
- registry bootstrap 可以视为找到本网络中的 catalog agent/server。
- 服务健康不能只看 registry 连接，还要看 worker lease、应用健康和 EasyTier 可达性。

参考：

- [Consul Agent Service API](https://developer.hashicorp.com/consul/api-docs/agent/service)
- [Consul Health Checks](https://developer.hashicorp.com/consul/docs/services/usage/checks)
- [Consul Service Discovery](https://developer.hashicorp.com/consul/docs/discover/service-dynamic-discovery)

## 4. Consul Gossip 与 Server/Client 分层

适合借鉴：

- Consul 使用 gossip 做成员关系和故障探测。
- server 节点参与一致性和目录管理，client agent 可以参与网络但不参与 Raft peer set。
- 节点 membership 与服务目录是相关但不同的概念。

不直接照搬：

- EtDiscovery 首版不需要实现完整 gossip 协议。
- EasyTier 已经提供 overlay peer 可见性，EtDiscovery 应复用而不是重造成员网络。

对本方案启发：

- EasyTier peer 只能作为 registry 候选来源之一，不能直接等同于 registry。
- relay、打洞、client 节点可以是网络成员，但不是 catalog 管理者。
- registry 能力必须通过声明和探测确认。

参考：

- [Consul Gossip](https://developer.hashicorp.com/consul/docs/concept/gossip)
- [Consul Consensus](https://developer.hashicorp.com/consul/docs/concept/consensus)

## 5. Envoy xDS / EDS

适合借鉴：

- Envoy 将 listener、cluster、endpoint 等发现职责拆开。
- Endpoint Discovery Service 关注端点集合及其健康、locality、负载权重。
- 控制面更新是异步的，数据面保留已有配置直到新配置到达。

不直接照搬：

- EtDiscovery 不作为业务代理，不接管请求转发。
- 不需要实现完整 xDS 协议族。

对本方案启发：

- registry bootstrap 和服务 endpoint discovery 可以分层。
- registry 不可达时，client 可以短期使用缓存服务候选，而不是立刻归零。
- 多地域和弱网情况下，服务不可用原因应区分为 endpoint 问题、路径问题、控制面 stale。

参考：

- [Envoy Service Discovery](https://www.envoyproxy.io/docs/envoy/latest/intro/arch_overview/upstream/service_discovery)
- [Envoy Endpoint Discovery Service](https://www.envoyproxy.io/docs/envoy/latest/api-v3/service/endpoint/v3/eds.proto)

## 6. Serf

适合借鉴：

- 轻量 membership、gossip、tag 和 event/query 机制。
- 节点 tag 可以声明角色、region、能力等。
- 适合理解“成员发现”和“服务目录”之间的边界。

不直接照搬：

- EtDiscovery 首版不引入新的 gossip 成员层。
- EasyTier 已经提供 peer 关系，Serf 更适合作为概念参考。

对本方案启发：

- registry 能力可以看成节点 tag/capability 的显式声明。
- 自动发现时应先拿到声明，再决定是否把 peer 当 registry。
- 后续如需更主动的 registry 广播，可以参考 Serf event/query，但首版不必实现。

参考：

- [Serf Introduction](https://www.serf.io/intro/index.html)
- [Serf Agent Options](https://developer.hashicorp.com/serf/docs/agent/options)

## 7. DNS SRV 与 Well-Known URI

适合借鉴：

- DNS SRV 用服务名发现具体 host/port。
- `.well-known` URI 适合在固定路径暴露服务元数据或 discovery metadata。

不直接照搬：

- EasyTier 网络内不一定有稳定 DNS。
- 首版不依赖外部 DNS 或公网域名。

对本方案启发：

- `/.well-known/etdiscovery` 是低成本、易调试的首版能力声明方式。
- 未来可把 registry 候选来源扩展为 DNS SRV、MagicDNS 或配置中心。

参考：

- [RFC 2782: DNS SRV](https://www.rfc-editor.org/rfc/rfc2782)
- [RFC 8615: Well-Known URIs](https://www.rfc-editor.org/rfc/rfc8615)

## 8. 对 EtDiscovery 的归纳

建议采用的组合：

- 像 DHCP 一样，把 registry discovery 作为节点入网后的基础设施发现流程。
- 像 Consul 一样，区分成员节点、catalog 管理者、服务实例和健康检查。
- 像 Kubernetes EndpointSlice 一样，以 endpoint/instance 为服务选择对象，并保留拓扑和状态条件。
- 像 Envoy 一样，把控制面发现和业务流量转发解耦，允许缓存与最终一致。
- 像 Serf 一样，把 registry 能力视为节点 capability/tag，而不是从 peer 列表位置推断。
- 像 EasyTier 现有 `RoutePeerInfo` 一样，复用 overlay route metadata 传播节点级信息。

当前最适合 EtDiscovery 的首版路线：

1. 显式 `RegistryCandidates`
2. EasyTier `RoutePeerInfo.node_type_flags` 官方能力标记与 `node_type_app_id` 应用命名空间
3. `/.well-known/etdiscovery` 标准 API 明细声明
4. 本地 `LastKnownRegistries` 缓存
5. 从 EasyTier peer 虚拟 IP 自动探测作为旧版本兼容 fallback
6. 后续再增加签名、多 registry 同步、locality 评分和更丰富的安全策略
