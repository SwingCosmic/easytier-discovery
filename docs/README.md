# 基于 EasyTier 虚拟组网的服务注册设计总览

本文档改为总览入口，便于继续把设计、争议点、应用层和实施计划分别细化。

## 文档导航

- [01. 核心设计](./service-registry-core-design.md)
- [02. 待讨论分歧点](./service-registry-open-questions.md)
- [03. 应用层与集成设计](./service-registry-application-layer.md)
- [04. 参考资料与对比](./service-registry-references.md)
- [05. 最小验证原型方案](./service-registry-prototype-validation.md)
- [06. 实施方案与阶段计划](./service-registry-plan.md)
- [07. EasyTier 仓库研究资料](../../easytier-research.md)

## 项目背景

项目名称暂定：EasyTier Discovery  
代码标识名称：`etdiscovery`

目标是在 EasyTier 已有虚拟组网、NAT 穿透、relay、链路质量采集能力之上，增加：

- 服务注册与发现
- 弱网环境下的可用性判断
- 面向调用方的实例选择与调用方式推荐
- 多区域、多枢纽节点场景下的最终一致目录同步

补充参考：

- `easytier-research.md` 保持在仓库根目录，避免其中已有的大量项目文件链接失效。

## 当前结论摘要

- EasyTier 继续负责虚拟网络、P2P、STUN/NAT、relay 和基础链路观测。
- 独立的 A 类注册中心负责控制面、目录聚合、策略与状态汇总。
- B/C 类 SDK 负责注册、续约、发现、评分、失败反馈和本地缓存。
- 配置与 ACL 采用“拓扑所有权 + owner 确认链 + 多 A 最终一致同步”模型。
- 存活判断采用“租约 TTL + 应用健康 + EasyTier 路由信号 + 多观察者怀疑投票 + 调用反馈”的组合机制。
- SDK 只做实例选择，不封装业务 RPC。
- 单个节点允许同时承担多个角色，但最终权限必须收敛到“目录管理 / 服务发布 / 服务消费”三组互斥能力清单中，避免职责漂移。
- 在复杂算法落地前，先通过 `etdiscovery/` 下的 C# 原型解决方案验证“进程托管、连网即注册、节点处理、服务选择、最小 Web API”闭环。

## 下一步细化建议

建议后续讨论按下面顺序推进：

1. 先冻结核心模型和状态机。
2. 再讨论 sidecar 与 C ABI 的边界和首版取舍。
3. 然后细化应用层 API、已有框架集成方式和移动端预留策略。
4. 最后把参考方案与阶段计划持续补全。
