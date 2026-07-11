# 参考点对照表

快速对照“某系统里常被讨论的机制”。详细事实见各专题页；本仓库如何取舍见设计文档。

| 系统 | 常被讨论的机制 | 常见不照搬点 |
| --- | --- | --- |
| DHCP | 入网后自动获基础设施配置；租约与续约 | L2 广播协议本身；与 overlay VIP 分配混为一谈 |
| ZooKeeper | 临时节点、watch、版本目录 | 强依赖中心 session 判活 |
| Nacos | 实例资源模型；注册/续租分离；metadata / list | 稳定数据中心网络默认假设 |
| Consul | register/deregister/maintenance；agent；成员与 catalog 分层 | 数据中心为主的默认前提 |
| Eureka | register/cancel/heartbeat；status override | application-first 路径风格 |
| Kubernetes EndpointSlice | ready/serving/terminating；endpoint 列表 | 控制器与 resourceVersion 全套 |
| Orleans | 多观察者 suspect | 完整 membership 协议 |
| Envoy xDS / EDS | 控制面发现与 endpoint 数据面分层；locality | 完整 xDS；业务代理职责 |
| Serf | 节点 tag/capability；membership 边界 | 新增一层 gossip |
| DNS SRV / Well-Known URI | 服务名或固定路径暴露元数据 | 依赖稳定外部 DNS |
| EasyTier RoutePeerInfo | overlay route 传播节点级元数据 | 把服务端口/region 等塞进网络层标志位 |
| gRPC | name resolver 边界 | 接管业务调用栈 |
| Spring Cloud | 客户端负载均衡体验 | 全量 SPI 兼容 |
| Dubbo | 消费方路由与地址发现分层 | 注册中心协议适配 |

## 怎么用

- 注册/心跳 API 形态 → Nacos、Consul、Eureka 专题页  
- 控制面/bootstrap → DHCP、Consul gossip、Serf、DNS SRV 专题页  
- 实例状态拆分 → EndpointSlice  
- 弱网判活 → Orleans  
- 应用接入 → gRPC / Spring / Dubbo 专题页  
