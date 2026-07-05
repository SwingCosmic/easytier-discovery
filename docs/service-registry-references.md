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
