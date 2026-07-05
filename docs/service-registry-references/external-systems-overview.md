# 外部系统概览

本文档汇总当前设计最相关的外部系统资料，便于后续讨论时直接引用。

## 1. ZooKeeper

适合借鉴：

- 临时节点与 session 生命周期
- watch 事件模型
- 版本化目录和元数据管理

不直接照搬：

- 强依赖中心 session 判活

对本方案的启发：

- 可借用“实例跟随会话生命周期变化”的建模方式
- 但弱网与移动网络场景下，控制面断连不能直接视为实例死亡

参考：

- [Apache ZooKeeper Programmer's Guide](https://zookeeper.apache.org/doc/current/zookeeperProgrammers.html)

## 2. Nacos

适合借鉴：

- 服务、分组、集群、实例、metadata、weight、healthy、ephemeral 模型
- 三段式心跳与摘除节奏
- 服务发现与配置中心分层思路

不直接照搬：

- 默认数据中心式稳定网络前提

对本方案的启发：

- 服务实例模型和状态字段适合直接参考
- “空保护”很适合弱网场景下的缓存降级和雪崩保护

参考：

- [What is Nacos](https://nacos.io/en/docs/latest/what-is-nacos/)
- [Nacos Open API](https://nacos.io/en/docs/latest/manual/user/open-api/)

## 3. Consul

适合借鉴：

- agent 模式
- 多类型健康检查
- prepared query 的策略化发现

不直接照搬：

- 以数据中心部署为主的默认前提

对本方案的启发：

- 本地 SDK/agent 负责采集和上报，中心负责聚合，这一点与本方案高度一致
- prepared query 很适合映射为 `selectOneHealthyInstance` 一类查询

参考：

- [Consul Service Discovery](https://developer.hashicorp.com/consul/docs/discover/service-dynamic-discovery)
- [Consul Prepared Queries](https://developer.hashicorp.com/consul/docs/discover/load-balancer/prepared-query)

## 4. Orleans

适合借鉴：

- 多观察者怀疑投票
- suspect 优先于立即 dead
- 成员表只做视图协调

不直接照搬：

- 完整 membership 协议不必原样复制

对本方案的启发：

- 最值得借鉴的是“怀疑票 + 过期 + 多来源”
- 非稳定网络下，存活判断应该是组合推断，而不是单点结论

参考：

- [Orleans Cluster Management](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/cluster-management)
