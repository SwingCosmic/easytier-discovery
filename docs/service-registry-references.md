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
