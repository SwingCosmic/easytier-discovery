# 基于 EasyTier 的服务发现设计总览

本文档作为 `etdiscovery/docs/` 的总入口，记录当前设计文档导航，以及最小 Web 原型的最新验证结论。

## 文档导航

- [01. 核心设计](./service-registry-core-design.md)
- [02. 待讨论问题](./service-registry-open-questions.md)
- [03. 应用层与集成设计](./service-registry-application-layer.md)
- [04. 参考资料与对比](./service-registry-references.md)
- [05. 最小原型验证](./service-registry-prototype-validation.md)
- [06. 实施方案与阶段计划](./service-registry-plan.md)
- [07. Registry Bootstrap Discovery 设计草案](./service-registry-bootstrap-discovery.md)
- [08. EasyTier RoutePeerInfo Node Type Flags 主仓库草案](../../easytier/docs/route_peer_node_type_flags.md)
- [09. EasyTier 仓库研究资料](../../easytier-research.md)

## 当前结论（2026-07-11）

- `etdiscovery/EtDiscovery.Web`、`EtDiscovery.Core`、`EtDiscovery.Tests` 已完成最小 Web 原型，并完成 **registry 发现 + worker 注册** 联调。
- Web 原型已验证以下闭环可用：
  - 托管 `easytier-core`（生成 TOML + `-c`，不再长命令行拼参）
  - 通过 `easytier-cli` 读取 peer / route 元数据
  - 用 `node_type_app_id` / `node_type_flags` 识别 registry 候选
  - worker 通过 HTTP 向 registry 注册服务实例
  - 提供 `/health`、`/easytier/peers`、`/test/ping`、`/discovery/registry`、`/discovery/services`、`/discovery/select`、`/discovery/instances`
- 配置与发现规则：
  - `EtDiscovery` / `EasyTier` 分节；`Peers` 只属于 EasyTier
  - 角色元数据只由 `--roles` 推导，禁止配置覆盖
  - 显式 `RegistryCandidates` + route metadata 发现；**不再**把首个 peer VIP 当 registry
  - 无相关元数据时远端 peer 默认 `worker`
  - registry 元数据：`GET /discovery/registry`（不用 `.well-known`）
- 联调踩坑与修复已写入 [Registry Bootstrap Discovery](./service-registry-bootstrap-discovery.md) 的“当前实现状态”：
  - 空 `Listeners` 必须生成默认 11010 监听
  - verbose `peer list` JSON 不能把 `ipv4_addr.address` 当 string
  - registry `ListenUrl` 禁止只绑 `127.0.0.1`

## 当前注意事项

- **registry `ListenUrl` 必须对虚拟网可达**：推荐 `http://0.0.0.0:8080`，不要用 `http://127.0.0.1:8080`。
- Windows 上如果节点需要本机虚拟 IP，则通常需要管理员权限。
  - `registry` / 静态 `Ipv4` / DHCP 都需要。
- Windows 上为了自动弹出 UAC，`EtDiscovery.Web` 已嵌入 `app.manifest`。
  - 只有 `EtDiscovery.Web.exe` 会应用；`dotnet xxx.dll` 不会提权。
- Linux 上如果节点需要本机虚拟 IP，则必须保证 TUN 可用（root/`/dev/net/tun`）。
- `worker + dhcp` 表示 EasyTier 自动分配虚拟 IP，不是“registry 当传统 DHCP 服务器”。
- 当前较稳妥验证路径：
  - `registry` 固定 `EasyTier.Ipv4` + `ListenUrl=0.0.0.0:8080`
  - `worker` 先 DHCP；不稳则写死 worker VIP

## 推荐验证方式

- Windows：
  - 运行发布后的 `EtDiscovery.Web.exe`
  - 使用管理员权限启动
- Linux：
  - 确认 `/dev/net/tun` 和 `tun` 模块可用
  - 再运行发布产物

更详细的原型实现、限制和排查结论见 [最小原型验证](./service-registry-prototype-validation.md)。
