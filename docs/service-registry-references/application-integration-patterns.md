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
