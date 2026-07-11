# Bootstrap、Membership 与服务发现参考

节点如何发现控制面、声明能力、区分“节点成员”与“服务目录”。  
本仓库 bootstrap 设计见 [Registry Bootstrap Discovery](../service-registry-bootstrap-discovery.md)。

## 1. DHCP

**机制：**

- discover / offer / request / ack 获取 IP、网关、DNS 等
- 租约：过期、续约、重新协商
- 客户端不必预先知道全部基础设施地址

**链接：**

- [RFC 2131: Dynamic Host Configuration Protocol](https://www.rfc-editor.org/rfc/rfc2131)

## 2. Kubernetes EndpointSlice

**机制：**

- endpoint 列表 + `ready` / `serving` / `terminating`
- 可带 node、zone 等拓扑信息

**链接：**

- [Kubernetes EndpointSlices](https://kubernetes.io/docs/concepts/services-networking/endpoint-slices/)

## 3. Consul Agent 与 Health Check

**机制：**

- 本地 agent 注册与检查；中心侧 catalog 视图
- service check 与 node check 可分离；TTL check 表达持续续约
- prepared query：查询时按策略选目标

**链接：**

- [Consul Agent Service API](https://developer.hashicorp.com/consul/api-docs/agent/service)
- [Consul Health Checks](https://developer.hashicorp.com/consul/docs/services/usage/checks)
- [Consul Service Discovery](https://developer.hashicorp.com/consul/docs/discover/service-dynamic-discovery)

## 4. Consul Gossip 与 Server/Client 分层

**机制：**

- gossip 做成员关系与故障探测
- server 参与一致性与目录；client agent 可在网内但不进 Raft peer set
- membership 与服务目录相关但不同

**链接：**

- [Consul Gossip](https://developer.hashicorp.com/consul/docs/concept/gossip)
- [Consul Consensus](https://developer.hashicorp.com/consul/docs/concept/consensus)

## 5. Envoy xDS / EDS

**机制：**

- listener / cluster / endpoint 等发现职责拆分
- EDS 关注端点集合、健康、locality、权重
- 控制面异步更新；数据面可保留旧配置直至新配置到达

**链接：**

- [Envoy Service Discovery](https://www.envoyproxy.io/docs/envoy/latest/intro/arch_overview/upstream/service_discovery)
- [Envoy Endpoint Discovery Service](https://www.envoyproxy.io/docs/envoy/latest/api-v3/service/endpoint/v3/eds.proto)

## 6. Serf

**机制：**

- 轻量 membership、gossip、tag、event/query
- 节点 tag 可声明角色、region、能力

**链接：**

- [Serf Introduction](https://www.serf.io/intro/index.html)
- [Serf Agent Options](https://developer.hashicorp.com/serf/docs/agent/options)

## 7. DNS SRV 与 Well-Known URI

**机制：**

- DNS SRV：用服务名解析 host/port
- Well-Known URI（RFC 8615）：在固定路径暴露站点元数据（行业常见模式之一）

**说明：**

- 是否在某一产品中采用固定路径、路径具体叫什么，属于该产品的设计选择，不在本摘要中定稿

**链接：**

- [RFC 2782: DNS SRV](https://www.rfc-editor.org/rfc/rfc2782)
- [RFC 8615: Well-Known URIs](https://www.rfc-editor.org/rfc/rfc8615)

## 8. 对照要点（中性）

阅读上述资料时可关注：

| 主题 | 常见做法 |
| --- | --- |
| 入网后基础设施发现 | DHCP 式“先入网再拿配置” |
| 成员 vs 目录 | 网络成员不必等于 catalog 管理者 |
| 能力声明 | tag / capability / 元数据，而非列表位置推断 |
| 控制面 vs 数据面 | 控制面入口与业务 endpoint 分层；可缓存 |
| 实例状态 | 多条件（就绪、排空、服务中）优于单布尔 |
| 元数据暴露 | DNS SRV、固定 HTTP 路径等均可；视环境选型 |
