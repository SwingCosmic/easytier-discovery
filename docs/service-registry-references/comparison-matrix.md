# 参考点对照表

本文档用于快速对照不同系统能借来什么、哪些部分不适合直接复制。

| 系统 | 本方案重点借鉴 | 不直接照搬的部分 |
| --- | --- | --- |
| DHCP | 入网后自动发现基础设施配置、租约与缓存 | 不负责 EasyTier 虚拟 IP 分配，不实现 L2 广播协议 |
| ZooKeeper | 临时节点、watch、版本目录 | 强依赖中心 session 判活 |
| Nacos | 实例资源模型、注册/续租分离、metadata 与 list API | 默认数据中心稳定网络假设 |
| Consul | register/deregister/maintenance 分离、agent 上报模式、成员与 catalog 管理分层 | 以数据中心部署为主的默认前提 |
| Eureka | register/cancel/heartbeat/status override 分离 | Java 生态下 application-first 路径风格 |
| Kubernetes EndpointSlice | ready/serving/terminating 状态拆分 | 声明式控制器与资源版本体系 |
| Orleans | 多观察者怀疑投票、suspect 机制 | 完整成员管理协议不必全盘复制 |
| Envoy xDS / EDS | 控制面发现与 endpoint 数据面选择分层、locality 与健康状态 | 不实现完整 xDS 协议，不作为业务代理 |
| Serf | 节点 tag/capability、membership 与服务目录边界 | 不新增 gossip 成员层 |
| DNS SRV / Well-Known URI | 固定服务名或固定路径暴露 discovery metadata | 首版不依赖外部 DNS |
| EasyTier RoutePeerInfo | 复用 route metadata 传播官方能力标记和应用命名空间 flags | 不把端口、region、priority 等结构化服务发现信息塞进网络层标志位 |
| gRPC | name resolver 边界 | 不接管业务调用栈 |
| Spring Cloud | 调用方负载均衡体验 | 不强行兼容全部 SPI |
| Dubbo | 消费方路由与地址发现分层 | 不直接做注册中心协议适配 |

## 使用建议

- 想找注册/下线/心跳 API 形态时，先看 Nacos、Consul、Eureka。
- 想找 registry 自动发现和入网 bootstrap 形态时，先看 DHCP、Consul gossip、Serf、DNS SRV / Well-Known URI，以及 EasyTier `RoutePeerInfo`。
- 想找实例状态拆分方式时，先看 Kubernetes EndpointSlice。
- 想找核心模型参考时，先看 ZooKeeper、Nacos、Consul。
- 想找弱网判活与成员判断参考时，先看 Orleans。
- 想找应用层接入形态时，先看 gRPC、Spring Cloud、Dubbo。
