# 参考资料索引

本目录存放 **已查阅的第三方系统/协议摘要**，便于复用链接与事实，避免每次讨论重新搜索。

- 设计定稿、API 路径、阶段进度 **不写在这里**；见 [核心设计](./service-registry-core-design.md)、[应用层](./service-registry-application-layer.md)、[Bootstrap](./service-registry-bootstrap-discovery.md)、[实施方案](./service-registry-plan.md)。
- 某次讨论形成的本仓库结论，应回写设计文档；参考页最多增加“相关机制 + 官方链接”。

## 目录

| 文档 | 内容 |
| --- | --- |
| [EasyTier 可复用能力](./service-registry-references/easytier-capabilities.md) | EasyTier 已有网络/观测/嵌入能力清单 |
| [外部系统概览](./service-registry-references/external-systems-overview.md) | ZooKeeper、Nacos、Consul、Eureka 等 |
| [应用层接口风格参考](./service-registry-references/application-integration-patterns.md) | gRPC / Spring / Dubbo 等接入边界 |
| [参考点对照表](./service-registry-references/comparison-matrix.md) | 系统 × 可借鉴点速查 |
| [Bootstrap 与 Membership 参考](./service-registry-references/bootstrap-and-membership-models.md) | DHCP、Serf、xDS、DNS SRV 等 |
| [EasyTier 仓库研究](../../easytier-research.md) | 仓库级研究笔记（文件不移动） |
| [RoutePeerInfo Node Type Flags](../../easytier/docs/route_peer_node_type_flags.md) | 主仓库字段草案 |

## 维护约定

1. 一篇一主题；新主题优先新建子文档，不无限追加到已有页。
2. 推荐结构：系统是什么 → 相关机制/接口（对方术语）→ 官方链接 → 可选“与服务发现相关的中性对照点”。
3. 不写本仓库 HTTP 路径定稿、不写实现进度、不写项目 backlog（待办见 [plan §7](./service-registry-plan.md#7-待补充资料文档侧)）。
4. 对照表用于快速导航；细节以各专题页为准。
