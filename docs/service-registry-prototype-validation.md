# 最小 Web 原型验证

本文档记录 `etdiscovery` 首轮最小 Web 原型的实现范围、当前验证结论和已确认的注意事项。

## 1. 目标

首轮目标不是最终架构定稿，而是先验证以下问题：

- 能否在 `etdiscovery/` 下用纯 C# 快速搭建最小可运行原型
- 能否由 Web 服务托管 `easytier-core`，并通过 `easytier-cli` 读取真实组网状态
- 能否将 discovery 核心逻辑下沉到独立类库，并保持单元测试不依赖真实网络
- 能否完成 `registry + worker` 两节点的最小真实链路验证

## 2. 当前项目结构

当前以仓库现状为准：

- `etdiscovery/EtDiscovery.Web/`
- `etdiscovery/EtDiscovery.Core/`
- `etdiscovery/EtDiscovery.Tests/`
- `etdiscovery/docs/`

其中：

- `EtDiscovery.Web`
  - 托管 `easytier-core`
  - 调用 `easytier-cli`
  - 提供最小 Web API
- `EtDiscovery.Core`
  - 承载 discovery 相关模型和策略
- `EtDiscovery.Tests`
  - 以纯模拟数据验证核心逻辑和部分 Web 侧纯逻辑

## 3. 已落地能力

### 3.1 启动与配置

- `--roles` 为唯一必须显式通过命令行提供的参数
- 其他参数统一从配置文件读取
- 角色统一使用：
  - `registry`
  - `worker`
  - `client`

### 3.2 EasyTier 进程托管

- 使用 `ProcessStartInfo.ArgumentList` 逐项构造参数
- 自动分配本地 RPC 地址并传给 `--rpc-portal`
- 应用退出时自动回收子进程
- 已增加最近 `stdout/stderr` 缓存和退出摘要日志

### 3.3 HTTP 接口

当前接口全部去掉 `/api` 前缀：

- `GET /health`
- `GET /easytier/peers`
- `GET /test/ping`
- `POST /discovery/instances`
- `DELETE /discovery/instances/{instanceId}`
- `GET /discovery/instances/{instanceId}`
- `GET /discovery/services`
- `GET /discovery/select`

### 3.4 registry 侧能力

- 通过 `easytier-cli` 读取 peer 列表
- 按 `network-name` 和 `virtual-network-cidr` 过滤 discovery 候选
- 维护实例注册表，按 `instanceId` upsert / deregister 真实服务实例
- `/discovery/services` 返回已注册且当前可发现的实例列表

### 3.5 worker 侧能力

- 可加入现有 EasyTier 网络
- 可暴露最小 `/test/ping`
- 可根据 `Services[]` 配置主动向 registry 注册服务实例
- 当本机同时具备 `registry + worker` 角色时，可直接走进程内注册逻辑
- 可选择：
  - 显式配置静态 `Ipv4`
  - 在未配置 `Ipv4` 时追加 `--dhcp`

## 4. 当前验证结论

### 4.1 已初步验证可用

- 最小 Web 应用可以正常运行
- `registry` 与 `worker` 的核心逻辑链路已经打通
- `registry` 可以读取 EasyTier peer 状态并结合实例注册表生成 discovery 视图
- 增强日志和 `/health` 输出后，排查体验已明显改善

### 4.2 已确认的 EasyTier 行为

- `EtDiscovery:Ipv4` 为空时，EasyTier 不会直接创建本机 TUN 虚拟地址
- `worker + --dhcp` 的含义是：
  - 由 worker 自己启用 EasyTier 的自动地址选择逻辑
  - 不是“registry 显式充当 DHCP 服务器并主动分配地址”
- `registry` 不应因为“单点启动”而自动附加 `--dhcp`
  - 否则 registry 自己的 IP 可能漂移

### 4.3 DHCP 现阶段结论

- 官方 desktop 客户端在 Windows 上开启 DHCP 后，能够拿到 `10.1.1.2`
- Web 原型中仅给 worker 追加 `--dhcp` 后，逻辑方向已与 EasyTier 源码一致
- 但 DHCP 是否最终成功，还取决于节点是否有权限完成虚拟网卡创建与 IP 落地

## 5. 权限与平台注意事项

## 5.1 Windows

- 只要节点需要本机虚拟 IP，就应视为需要管理员权限
- 当前已确认需要管理员权限的典型场景：
  - `registry`
  - `worker` 使用静态 `Ipv4`
  - `worker` 使用 `--dhcp`
- 若权限不足，典型现象包括：
  - `Failed to create adapter`
  - 本机 `virtualIp` 一直为空
  - DHCP 超时

### 5.1.1 Windows 清单嵌入

项目已新增 `EtDiscovery.Web/app.manifest` 并在 Windows 构建时嵌入：

- `requestedExecutionLevel = requireAdministrator`
- `UseAppHost = true`

注意：

- 只有运行生成出的 `EtDiscovery.Web.exe` 才会触发 UAC 提权
- 如果使用 `dotnet EtDiscovery.Web.dll` 启动，不会自动提权

### 5.1.2 Windows 推荐启动方式

- 优先运行发布或构建输出的 `EtDiscovery.Web.exe`
- 使用管理员权限启动

## 5.2 Linux

根据 EasyTier 源码当前可确认的最小前提：

- 如果需要本机虚拟 IP，则必须保证 TUN 可用
- 通常需要 `root` 或等效权限
- 需要确保 `/dev/net/tun` 可访问
- 必要时执行：
  - `modprobe tun`
- 某些可选能力需要 `CAP_NET_ADMIN`
  - 例如部分 socket mark 相关功能
  - 这与最基本的 TUN 前提不是同一层要求

## 6. 当前实现约束

### 6.1 网络筛选

- `/discovery/*` 只基于以下候选构建：
  - `sameNetwork = true`
  - 虚拟 IP 位于 `virtual-network-cidr` 内

### 6.2 服务元数据

- worker 已实现主动注册/下线服务实例
- registry 已支持一个服务名对应多个实例
- registry 自身兼任 worker 时，也可把本机服务实例注册到目录中
- 配置结构已从单服务字段切换为 `Services[]`

### 6.3 状态观测

- `/easytier/peers` 会明确返回：
  - `networkName`
  - `virtualIp`
  - `sameNetwork`
  - `inVirtualNetworkCidr`
  - `eligibleForDiscovery`
- `/health` 当前会额外返回：
  - `configuredVirtualIp`
  - `dhcpEnabled`
  - `requiresTunDevice`
  - `requiresWindowsElevationForEasyTier`
  - `processElevated`
  - `privilegeChecklist`
  - `easyTier.recentStdout`
  - `easyTier.recentStderr`

### 6.4 内存状态读取模型

- 当前原型在内存中维护一份共享数据源：
  - EasyTier 节点观测快照
  - 已注册服务实例表
  - 已构建的 discovery catalog
- 数据源本身允许后台线程并发更新，因此实现上优先使用并发集合或“整体替换快照引用”的方式。
- 对外接口的读取语义是“瞬时快照”：
  - 每次请求都读取当前时刻可见的数据
  - 不保证连续两次读取完全一致
  - 不要求节点状态、实例状态、选择结果之间形成强一致事务视图
- 这样做是有意设计，因为在 EasyTier 节点观测和服务注册场景里，底层事实本身就存在传播延迟、掉线抖动和短暂不确定性。

## 7. 推荐验证路径

### 7.1 Windows

推荐流程：

1. `registry` 使用显式固定 `Ipv4`
2. 运行 `EtDiscovery.Web.exe`
3. 使用管理员权限启动
4. `worker` 先尝试未配置 `Ipv4` 且启用 `--dhcp`
5. 若现场环境下 DHCP 不稳定，再改为显式静态 `Ipv4`

### 7.2 Linux

推荐流程：

1. 先检查 `/dev/net/tun`
2. 确认 `tun` 模块已加载
3. 再启动 Web 原型

## 8. 当前后续建议

- 在原型继续演进前，优先保持“可排查性”
- Windows 与 Linux 的启动方式、权限要求和排查步骤应同步写入用户文档
- 若后续需要继续依赖 DHCP 行为，建议增加：
  - 更明确的 DHCP 成功/失败状态日志
  - 更细粒度的 `blockingReason`
  - Windows 与 Linux 的启动示例

## 9. 当前阶段结论

首轮最小 Web 原型已经具备继续推进的基础，原因是：

- Web 托管 EasyTier 已可用
- discovery 核心逻辑已从真实网络依赖中拆分出来
- Windows 权限问题、DHCP 行为和启动方式的关键偏差已被明确识别
- 当前剩余问题更多是“环境与运行方式约束”，而不是原型整体方向错误
