# 参考资料索引

本目录存放 **已查阅的第三方系统/协议摘要**，便于复用链接与事实，避免每次讨论重新搜索。

- 设计定稿、API、进度 **不写这里** → 核心设计 / 应用层 / Bootstrap / plan。  
- 本仓库结论回写设计文档；此处最多“相关机制 + 官方链接”。  
- **维护与编写规范** → 仓库根 [AGENTS.md §4](../AGENTS.md#4-文档职责与编写规范)。待办资料清单见 [plan §7](./service-registry-plan.md#7-待补充资料文档侧)。

## 目录

| 文档 | 内容 |
| --- | --- |
| [EasyTier 可复用能力](./service-registry-references/easytier-capabilities.md) | EasyTier 已有网络/观测/嵌入能力清单 |
| [外部系统概览](./service-registry-references/external-systems-overview.md) | ZooKeeper、Nacos、Consul、Eureka 等 |
| [应用层接口风格参考](./service-registry-references/application-integration-patterns.md) | gRPC / Spring / Dubbo 等接入边界 |
| [参考点对照表](./service-registry-references/comparison-matrix.md) | 系统 × 可借鉴点速查 |
| [Bootstrap 与 Membership 参考](./service-registry-references/bootstrap-and-membership-models.md) | DHCP、Serf、xDS、DNS SRV 等 |

旁路（仅当与 EasyTier 父仓并列检出时可读，**非本仓文件**）：父仓 `easytier-research.md`、`easytier/docs/route_peer_node_type_flags.md`。
