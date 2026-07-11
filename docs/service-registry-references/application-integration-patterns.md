# 应用层接口风格参考

第三方框架如何把“地址发现”和“业务调用”拆开。本仓库接入方案见 [应用层与集成](../service-registry-application-layer.md)。

## 1. gRPC

**机制：**

- Custom name resolution 将“解析地址”与 channel 上的业务调用解耦
- channel 继续负责连接复用与请求级策略

**链接：**

- [gRPC Custom Name Resolution](https://grpc.io/docs/guides/custom-name-resolution/)

## 2. Spring Cloud LoadBalancer

**机制：**

- 实例列表供应（Supplier）与调用侧负载均衡分层
- 客户端负载均衡在应用侧执行

**链接：**

- [Spring Cloud LoadBalancer Reference](https://docs.spring.io/spring-cloud-commons/reference/spring-cloud-commons/loadbalancer.html)

## 3. Dubbo

**机制：**

- 服务发现与地址列表管理
- 消费方路由、provider 选择与注册中心职责可分层

**链接：**

- [Dubbo Service Discovery](https://dubbo.apache.org/en/overview/mannual/java-sdk/tasks/service-discovery/)

## 4. 中性对照点

上述系统共同倾向：

- 发现/选择层输出候选地址或实例列表
- 业务框架保留连接、重试、序列化与协议处理

不在此规定本仓库的 HTTP 路径或 SDK 形状。
