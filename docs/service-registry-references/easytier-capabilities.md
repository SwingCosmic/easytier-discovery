# EasyTier 可复用能力

EasyTier 已提供、上层服务发现可依赖的能力清单。更完整的仓库研究见 [easytier-research.md](../../../easytier-research.md)；字段扩展见 [route_peer_node_type_flags.md](../../../easytier/docs/route_peer_node_type_flags.md)。

评分与实体如何使用这些信号，见 [核心设计](../service-registry-core-design.md)，不在此展开算法。

## 1. 网络与连通

- 虚拟 IPv4/IPv6
- TUN 接入
- peer 路由
- proxy cidr

## 2. NAT 探测与穿透

- UDP/TCP STUN
- NAT 类型识别
- UDP/TCP 打洞
- 对称 NAT 相关探测与尝试

## 3. Relay 与弱网兜底

- relay
- KCP relay
- QUIC relay

## 4. 路由与链路观测

- 路由同步
- next-hop 策略
- latency / loss / rx/tx
- 连接状态
- `RoutePeerInfo` 及可扩展元数据（含 `node_type_flags` / `node_type_app_id` 等，见主仓库文档）

## 5. 控制面接入基础

- peer RPC
- 本地 JSON RPC 桥接（如 `easytier-cli`）

## 6. 跨语言与嵌入基础

- C ABI
- JNI
- 移动端 TUN FD 接入

以上能力构成 overlay 网络底座；服务注册层不重复实现虚拟组网。
