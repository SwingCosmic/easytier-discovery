# 参考资料目录索引

本文档改为资料目录入口，用来索引可长期积累的参考材料。后续新增资料时，优先补充到子文档中，避免每次讨论都重新搜索。

## 目录结构

- [00. EasyTier 仓库研究资料](../../easytier-research.md)
- [01. EasyTier 可复用能力](./service-registry-references/easytier-capabilities.md)
- [02. 外部系统概览](./service-registry-references/external-systems-overview.md)
- [03. 应用层接口风格参考](./service-registry-references/application-integration-patterns.md)
- [04. 参考点对照表](./service-registry-references/comparison-matrix.md)
- [05. 关键算法与机制清单](./service-registry-references/key-mechanisms.md)
- [06. 待补充资料清单](./service-registry-references/backlog.md)
- [07. Bootstrap、Membership 与服务发现参考](./service-registry-references/bootstrap-and-membership-models.md)
- [08. EasyTier RoutePeerInfo Node Type Flags 主仓库草案](../../easytier/docs/route_peer_node_type_flags.md)

## 当前重点参考

这一轮服务注册实现，重点参考以下几类系统的应用层 API 设计：

- Consul
  - `register / deregister / maintenance`
  - 参考其“实例由 agent 或本地进程上报、管理端可额外覆盖实例状态”的接口分层
- Nacos
  - `register / deregister / beat / metadata / list`
  - 参考其实例资源模型、心跳接口和元数据独立更新方式
- Eureka
  - `register / cancel / heartbeat / status override`
  - 参考其“注册、续租、管理端状态覆盖”分别建模的做法
- Kubernetes EndpointSlice
  - `ready / serving / terminating`
  - 参考其“可被流量选择”与“正在终止/排空”状态拆分

针对 registry 自动发现与类似 DHCP 的 bootstrap 协议，重点参考：

- DHCP
  - 参考其“入网后自动获得基础设施地址与租约”的体验模型
- Consul gossip / server-client 分层
  - 参考其“成员节点”和“catalog 管理节点”分离
- Envoy xDS / EDS
  - 参考其“控制面发现”和“endpoint 数据面选择”分层
- Serf
  - 参考其“节点 tag/capability 声明，而不是从成员列表位置推断角色”
- Well-Known URI / DNS SRV
  - 参考其固定路径或服务名发现 metadata 的轻量做法
- EasyTier `RoutePeerInfo`
  - 参考其已存在的 route metadata 传播链路，将 registry 定位收敛到“官方能力标记 + 应用命名空间 flags”，而不是扫描所有 peer

EtDiscovery 本轮拟采纳的 API 风格：

- 以“实例资源”作为核心，而不是只围绕“服务名”设计接口
- 注册与下线分离，允许调用方显式控制实例生命周期
- 续租、健康、运维状态、元数据更新拆为独立辅助接口
- 管理端主动上下线 node/instance 的接口先占位，后续补充实现

## 使用约定

- 概览型内容放在索引相邻的专题文档里，不再堆回本页。
- `easytier-research.md` 作为仓库级研究资料保留在原位置，只在入口文档中引用，不移动文件本体。
- 外部系统尽量一类一节，统一记录“适合借鉴”“不直接照搬”“对本方案启发”“参考链接”。
- 有明显独立主题的新资料，优先新建子文档，而不是往已有文档无限追加。
- 如果某次讨论形成了固定结论，应把结论沉淀回设计文档，而不是只停留在资料目录。

## 建议后续扩展方向

- sidecar 生命周期管理最佳实践
- Rust C ABI / .NET PInvoke 封装样例
- Android/iOS TUN 与后台限制资料
- 多区域网络质量感知调度案例
- Actor placement 与 membership 相关公开设计
