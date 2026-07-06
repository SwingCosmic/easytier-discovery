# 应用层接口风格参考

本文档记录与应用层 SDK 设计直接相关的接口风格和集成参考。

## 1. gRPC

可借鉴点：

- name resolver 把“地址发现”和“业务调用”解耦
- channel 继续负责连接复用与请求级策略

对本方案的意义：

- etdiscovery 可以专注返回候选实例
- 不需要侵入业务调用协议本身

参考：

- [gRPC Custom Name Resolution](https://grpc.io/docs/guides/custom-name-resolution/)

## 2. Spring Cloud LoadBalancer

可借鉴点：

- 将实例列表供应和调用选择分层
- 在应用端执行客户端负载均衡

对本方案的意义：

- 有利于把 etdiscovery 放在“候选提供层”，而不是硬做全栈治理

参考：

- [Spring Cloud LoadBalancer Reference](https://docs.spring.io/spring-cloud-commons/reference/spring-cloud-commons/loadbalancer.html)

## 3. Dubbo

可借鉴点：

- 服务发现与地址列表管理
- 消费方路由和 provider 选择解耦

对本方案的意义：

- 适合作为“发现与选择分层”的接口体验参考

参考：

- [Dubbo Service Discovery](https://dubbo.apache.org/en/overview/mannual/java-sdk/tasks/service-discovery/)

## 4. 当前归纳

应用层最值得坚持的边界是：

- etdiscovery 做发现、筛选、评分、推荐
- 业务框架做连接、重试、序列化、协议处理

## 5. 面向迁移的注册 API 风格归纳

为了让已有业务代码从 Consul、Nacos、Eureka 一类系统迁移到 EtDiscovery 时成本更低，这一轮接口风格统一采用以下约定：

- 以“实例资源”作为核心对象
  - 服务名只是筛选维度，不是唯一控制入口
- 注册与下线分离
  - `POST /discovery/instances`
  - `DELETE /discovery/instances/{instanceId}`
- 查询接口同时支持“按服务看实例”和“按实例直接定位”
  - `GET /discovery/services?serviceName=...`
  - `GET /discovery/instances/{instanceId}`
- 辅助状态接口独立占位
  - `lease`
  - `health`
  - `status`
  - `metadata`

这样做的直接好处是：

- worker 周期性 upsert 比较自然
- 管理端主动下线/恢复不需要伪装成删除注册
- 后续扩展心跳、draining、metadata 增量更新时，不需要重做主接口

与读取模型配套的实现原则：

- 内存里维护一份共享数据源，允许并发更新
- 所有读接口直接读取该数据源的瞬时快照
- 不为了追求强一致而在整个读取路径上加重锁
- 这更符合服务发现系统“最终一致、短暂抖动可接受”的现实语义
