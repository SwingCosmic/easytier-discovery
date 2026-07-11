# 外部系统概览

第三方注册发现与成员系统摘要。结构：机制事实 → 不宜照搬点 → 官方链接。  
本仓库 API 定稿见 [应用层文档](../service-registry-application-layer.md)，不在此重复。

## 1. ZooKeeper

**机制：**

- 临时节点与 session 生命周期
- watch 事件模型
- 版本化目录与元数据

**不宜照搬：**

- 强依赖中心 session 判活（弱网下控制面断连不等于实例死亡）

**链接：**

- [Apache ZooKeeper Programmer's Guide](https://zookeeper.apache.org/doc/current/zookeeperProgrammers.html)

## 2. Nacos

**机制：**

- 服务 / 分组 / 集群 / 实例 / metadata / weight / healthy / ephemeral
- 注册与心跳分离；metadata、健康、列表多为独立接口
- 资源模型偏“实例为主，服务为容器”
- 服务发现与配置中心分层；存在“空保护”类降级思路

**不宜照搬：**

- 默认数据中心式稳定网络前提

**链接：**

- [What is Nacos](https://nacos.io/en/docs/latest/what-is-nacos/)
- [Nacos Open API](https://nacos.io/en/docs/latest/manual/user/open-api/)

## 3. Consul

**机制：**

- agent 本地注册/检查，中心侧 catalog
- register / deregister / maintenance 分离；管理端可覆盖服务状态而不必删注册
- 多类型健康检查、prepared query 策略化发现
- 接口常围绕本地 agent 组织

**不宜照搬：**

- 以数据中心与 agent 常驻为主的默认部署前提

**链接：**

- [Consul Service Discovery](https://developer.hashicorp.com/consul/docs/discover/service-dynamic-discovery)
- [Consul Prepared Queries](https://developer.hashicorp.com/consul/docs/discover/load-balancer/prepared-query)
- [Consul Agent Service API](https://developer.hashicorp.com/consul/api-docs/agent/service)

## 4. Eureka

**机制：**

- application / instance 分层
- register / cancel / heartbeat 分离
- 管理端状态覆盖可与自动状态并存（如 `OUT_OF_SERVICE`）

**不宜照搬：**

- 偏 Java 生态的 application-first 资源路径风格

**链接：**

- [Eureka REST Operations](https://github.com/Netflix/eureka/wiki/eureka-rest-operations)

## 5. Kubernetes EndpointSlice

**机制：**

- 以 endpoint 列表表达后端
- 条件拆分：`ready` / `serving` / `terminating`
- 可携带 node、zone 等拓扑信息

**不宜照搬：**

- 完整声明式控制器、资源版本与 reconciliation 体系

**链接：**

- [Kubernetes EndpointSlices](https://kubernetes.io/docs/concepts/services-networking/endpoint-slices/)

## 6. Orleans

**机制：**

- 多观察者怀疑投票；suspect 优先于立即 dead
- 成员表侧重视图协调

**不宜照搬：**

- 完整 membership 协议原样复制

**链接：**

- [Orleans Cluster Management](https://learn.microsoft.com/en-us/dotnet/orleans/implementation/cluster-management)
