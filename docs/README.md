# 基于 EasyTier 的服务发现设计总览

本文档作为 `etdiscovery/docs/` 的总入口，记录当前设计文档导航，以及最小 Web 原型的最新验证结论。

## 文档导航

- [01. 核心设计](./service-registry-core-design.md)
- [02. 待讨论问题](./service-registry-open-questions.md)
- [03. 应用层与集成设计](./service-registry-application-layer.md)
- [04. 参考资料与对比](./service-registry-references.md)
- [05. 最小原型验证](./service-registry-prototype-validation.md)
- [06. 实施方案与阶段计划](./service-registry-plan.md)
- [07. EasyTier 仓库研究资料](../../easytier-research.md)

## 当前结论

- `etdiscovery/EtDiscovery.Web`、`etdiscovery/EtDiscovery.Core`、`etdiscovery/EtDiscovery.Tests` 已完成首轮最小 Web 原型落地。
- Web 原型已验证以下闭环可用：
  - 托管 `easytier-core`
  - 通过 `easytier-cli` 读取真实 peer 状态
  - 将 peer 状态映射为 `registry` 视角下的 discovery 候选
  - worker 通过 HTTP 向 `registry` 主动注册/下线服务实例
  - `registry + worker` 同机时可直接注册本机服务实例
  - 提供 `/health`、`/easytier/peers`、`/test/ping`、`/discovery/services`、`/discovery/select`、`/discovery/instances`
- API 路径已经统一去掉 `/api` 前缀。
- 原型中的角色、接口、配置项均统一使用 `registry / worker / client`，不再使用 A/B/C 简写。
- 启动输入中仅 `--roles` 必须通过命令行显式传入，其余参数统一从配置文件读取。
- 服务发布配置已经从单服务字段切换为 `Services[]` 数组；`/discovery/services` 现在反映真实已注册实例，而不是固定拼装结果。
- 当前原型对 discovery 数据采用“单份内存数据源 + 并发集合 + 瞬时快照读取”的设计：
  - 内存里维护一份支持并发更新的注册/观测数据
  - 所有查询接口都读取该数据源在某一时刻的快照视图
  - 允许短时间内两次读取结果不同，不要求强一致
- worker 定位 registry 时：
  - 优先使用显式配置的 `RegistryPeer`
  - 否则回退到首个远端可发现 peer 的 `VirtualIp`

## 当前注意事项

- Windows 上如果节点需要本机虚拟 IP，则通常需要管理员权限。
  - `registry` 需要管理员权限。
  - `worker` 使用静态 `Ipv4` 时需要管理员权限。
  - `worker` 使用 `--dhcp` 自动获取虚拟 IP 时也需要管理员权限。
- Windows 上为了自动弹出 UAC，`EtDiscovery.Web` 已嵌入 `app.manifest`。
  - 只有启动生成出来的 `EtDiscovery.Web.exe` 才会应用该 manifest。
  - 如果使用 `dotnet EtDiscovery.Web.dll` 启动，不会自动提权。
- Linux 上如果节点需要本机虚拟 IP，则必须保证 TUN 可用。
  - 通常需要 `root` 或等效权限。
  - 必须可访问 `/dev/net/tun`。
  - 必要时需要先加载 `tun` 模块。
- `worker + --dhcp` 在 EasyTier 中表示“worker 自己启用自动地址分配逻辑”，不是“registry 显式充当 DHCP 服务器并主动下发 IP”。
- 当前原型最稳妥的验证路径仍然是：
  - `registry` 显式指定固定虚拟 IP
  - `worker` 先尝试 `--dhcp`
  - 若现场环境下 DHCP 不稳定，则直接显式指定 `worker` 的虚拟 IP

## 推荐验证方式

- Windows：
  - 运行发布后的 `EtDiscovery.Web.exe`
  - 使用管理员权限启动
- Linux：
  - 确认 `/dev/net/tun` 和 `tun` 模块可用
  - 再运行发布产物

更详细的原型实现、限制和排查结论见 [最小原型验证](./service-registry-prototype-validation.md)。
