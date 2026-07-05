# EasyTier 可复用能力

本文档记录 etdiscovery 可以直接依赖或重点复用的 EasyTier 基础能力。

## 1. 网络与连通能力

- 虚拟 IPv4/IPv6
- TUN 接入
- peer 路由
- proxy cidr

这些能力构成 etdiscovery 的基础网络底座，服务注册层不重复实现虚拟组网。

## 2. NAT 探测与穿透

- UDP/TCP STUN
- NAT 类型识别
- UDP/TCP 打洞
- 对称 NAT 相关探测与尝试

这些信息会直接影响：

- 节点画像
- 可用性评分
- 调用方式推荐
- relay 与直连的优先级

## 3. relay 与弱网兜底

- relay
- KCP relay
- QUIC relay

这些能力适合作为：

- 对称 NAT 场景下的连接兜底
- 直连失败时的备用路径
- 调用方式推荐中的 `relay_preferred` 基础

## 4. 路由与链路观测

- 路由同步
- next-hop 策略
- latency
- loss
- rx/tx
- 连接状态

这些观测数据适合输入到：

- `LinkProfile`
- `NodeAvailabilityScore`
- `ServiceEndpointScore`

## 5. 控制面接入基础

- peer RPC
- 本地 JSON RPC 桥接

这些能力适合做：

- 注册中心管理接口
- sidecar 与本地 runtime 的控制桥
- 节点本地诊断接口

## 6. 跨语言与嵌入基础

- C ABI
- JNI
- 移动端 TUN FD 接入

这些能力决定了后续多语言 SDK 和移动端方案具备演进空间，但不强迫首版全部落地。
