# 参考点对照表

本文档用于快速对照不同系统能借来什么、哪些部分不适合直接复制。

| 系统 | 本方案重点借鉴 | 不直接照搬的部分 |
| --- | --- | --- |
| ZooKeeper | 临时节点、watch、版本目录 | 强依赖中心 session 判活 |
| Nacos | 服务实例模型、心跳三段式、空保护 | 默认数据中心稳定网络假设 |
| Consul | agent 模式、健康检查、prepared query | 以数据中心部署为主的默认前提 |
| Orleans | 多观察者怀疑投票、suspect 机制 | 完整成员管理协议不必全盘复制 |
| gRPC | name resolver 边界 | 不接管业务调用栈 |
| Spring Cloud | 调用方负载均衡体验 | 不强行兼容全部 SPI |
| Dubbo | 消费方路由与地址发现分层 | 不直接做注册中心协议适配 |

## 使用建议

- 想找核心模型参考时，先看 ZooKeeper、Nacos、Consul。
- 想找弱网判活与成员判断参考时，先看 Orleans。
- 想找应用层接入形态时，先看 gRPC、Spring Cloud、Dubbo。
