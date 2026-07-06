[简体中文](./README.md) | [English](./README_EN.md)

# EasyTier Discovery

EasyTier Discovery（代码内简称 `EtDiscovery`）是一个构建在 EasyTier 之上的应用层服务发现与实例选择中间件。

当前仓库仍处于非常早期的原型阶段，现阶段主要在验证一个核心方向：

- 继续把 EasyTier 作为网络底座
- 在其上构建 discovery / control plane
- 提供一套更适合弱网、NAT、relay、跨地域场景的服务注册与发现 API

## 项目目标

EtDiscovery 预期会逐步演进为一个面向业务框架的中间件层，主要能力包括：

- 服务注册与下线
- 服务实例发现
- 健康实例选择
- 拓扑感知与网络感知的路由建议
- 为后续调度决策收集应用侧反馈

长期来看，EtDiscovery 并不是要替代现有 RPC 栈，而是希望承担以下角色：

- 更智能的地址簿
- 实例选择器
- 现有业务框架可接入的 discovery / control plane 集成点

## 功能与进展

- ✅ 基础运行形态
  - ✅ 在 .NET Web 应用中托管 EasyTier 进程
  - ✅ 通过 `easytier-cli` 读取真实节点与 peer 状态
  - ✅ 将 peer 观测结果映射为 discovery 候选信息
- ✅ 服务注册表
  - ✅ registry 维护内存服务目录
  - ✅ worker 通过 HTTP 主动注册服务
  - ✅ 单服务支持多个实例
  - ✅ registry 节点可同时兼任 worker 并注册本机服务
  - ✅ 返回真实注册实例，而不是固定配置拼装结果
- ✅ 基础选择能力
  - ✅ 支持按服务名选择实例
  - ✅ 支持在客户端或网关侧集成实例选择
- ✅ 当前并发模型
  - ✅ 单份内存数据源
  - ✅ 并发更新
  - ✅ 瞬时快照读取
  - ✅ 允许短时间内读取结果不一致
- ⬜ 待完成能力
  - ⬜ lease 续租
  - ⬜ health 更新
  - ⬜ draining / status override
  - ⬜ metadata 独立更新
  - ⬜ node 级运维管理 API
  - ⬜ watch 流式接口
  - ⬜ 基于反馈的调度与弱网感知评分
  - ⬜ 稳定的多语言 SDK / 集成方案

## 当前阶段

当前项目属于原型验证阶段，主要用于设计收敛和早期集成讨论。

已经完成的部分：

- 在 .NET Web 应用中托管 EasyTier 进程
- 通过 `easytier-cli` 读取真实的 EasyTier 节点与 peer 状态
- 将 peer 观测结果映射为 discovery 候选信息
- 实现基于内存的真实服务实例注册表
- worker 通过 HTTP 向 registry 注册
- registry 地址支持从显式 `RegistryPeer` 自动回退到首个可发现远端 peer 的虚拟 IP
- `registry` 侧 `/discovery/services` 和 `/discovery/select`
- `worker` 侧基于 `Services[]` 的自注册
- 基于“单份内存数据源 + 并发更新 + 瞬时快照读取”的并发模型

## 当前设计假设

当前原型默认采用以下假设：

- 大多数远端 peer 都是 registry peer
- 还没有建模 relay-only / transit-only 节点
- 服务发现数据是最终一致的
- 节点状态与实例状态之间存在短时间不一致是可以接受的

这意味着在很短时间内连续两次读取，返回结果可能不同，这是当前设计允许的。

## 开源协议

当前计划将本仓库采用 `AGPL-3.0-only` 协议开源，详见 [LICENSE](./LICENSE)。

选择这个协议的主要原因：

- 项目本质上是面向网络服务场景的中间件
- 希望约束服务提供方对中间件内核进行不完全兼容的定制分叉后却不公开源码的情况
- 希望使用标准、成熟、可识别的开源协议，而不是自行编写“类似 AGPL”的自定义条款
- 相比 `OSL-3.0`，`AGPL-3.0-only` 在基础设施和中间件项目中更容易被理解和接受

更具体地说，本项目希望表达的诉求是：

- 如果有人修改了 EasyTier Discovery 本身，并以网络服务形式向第三方提供该修改版服务，那么这些针对 EasyTier Discovery 的修改应当对相应用户开放源码
- 这样做的重点，不是为了抽象地追求“所有 SaaS 都必须开源”，而是为了尽量减少服务提供方长期维护不透明定制分叉、却把兼容性和适配成本转嫁给开发者与集成方的情况
- 这个诉求应当通过标准协议文本实现，而不是依赖额外的自定义补充条款或模糊解释

一个典型风险是：

- 某些服务提供方基于开源中间件做大量私有改造，对外宣称“兼容某协议”或“等价替代某系统”，但实际兼容性并不完整
- 由于修改内容不公开，外部开发者只能围绕这些行为差异做反复适配，最终形成事实上的生态割裂
- 本项目希望尽量降低出现这种局面的空间

之所以直接采用 `AGPL-3.0-only`，而不是自定义“AGPL-like”协议，主要是为了避免两类常见问题：

- 一类问题是条款自定义后边界反而更模糊，导致使用者需要重新理解一份非标准协议
- 另一类问题是为了追求“更强”或“更精确”的网络 copyleft 而引入额外条件，结果破坏了协议的通用性、兼容性和可接受度

同时，这里也希望澄清一些常见顾虑：

- 本项目希望约束的是对 EasyTier Discovery 自身的修改与部署，而不是把“任何与之通过网络交互的独立业务系统”都扩大解释为必须整体改用同一协议
- 本项目不打算通过 README 增加超出 `AGPL-3.0-only` 文本之外的新义务，也不打算在文档中主张一种比协议正文更宽或更窄的特殊解释

因此，README 中的这些说明仅用于表达项目的许可选择动机和设计意图，真正具有法律效力的内容始终以协议文本本身为准。如需针对具体部署、分发或合规场景获得正式结论，应咨询专业法律意见。

## 仓库结构

- `EtDiscovery.Web/`
  - ASP.NET Core 宿主
  - EasyTier 进程管理
  - HTTP API
  - 注册协调逻辑
- `EtDiscovery.Core/`
  - 共享 discovery 模型
  - catalog 构建逻辑
  - 选择策略抽象
- `EtDiscovery.Tests/`
  - 当前原型的单元测试与集成导向测试
- `docs/`
  - 设计文档
  - 实现进展
  - 外部参考资料

## 早期开发者体验

这还不是一个可用于生产的稳定包。当前开发流程主要服务于原型验证和详细设计迭代。

> [!WARNING]
> 当前仓库仍处于极早期阶段，接口、配置结构、内部行为和部署方式都可能在没有兼容性承诺的前提下直接调整。
> 如果你现在基于它做接入，请预期 API 可能随时发生较大变化，且不会提供稳定版本保证或逐项迁移通知。

### 1. 构建

在仓库根目录执行：

```powershell
dotnet build EtDiscovery.Web/EtDiscovery.Web.csproj
```

### 2. 配置 Registry

可以直接使用 `EtDiscovery.Web/` 下的配置文件，或发布目录中的配置文件。

Registry 侧注意点：

- 使用 `roles=registry`
- 建议显式设置固定 `Ipv4`
- 如果 registry 不兼任 worker，则 `Services[]` 可以为空

### 3. 配置 Worker

Worker 侧注意点：

- 使用 `roles=worker`
- 配置一个或多个 `Services[]`
- 可选配置 `RegistryPeer`
- 如果 `RegistryPeer` 为空，当前会回退到首个可发现远端 peer 的虚拟 IP

示例配置：

```json
{
  "EtDiscovery": {
    "NetworkName": "etd-test",
    "NetworkSecret": "test-secret123!",
    "VirtualNetworkCidr": "10.1.1.0/24",
    "ListenUrl": "http://127.0.0.1:8080",
    "Peers": ["tcp://bootstrap.example.com:11010"],
    "RegistryPeer": "",
    "Services": [
      {
        "ServiceName": "test",
        "Port": 8080,
        "Protocol": "http"
      }
    ]
  }
}
```

### 4. 运行

Registry：

```powershell
dotnet run --project EtDiscovery.Web -- --roles registry
```

Worker：

```powershell
dotnet run --project EtDiscovery.Web -- --roles worker
```

如果想进一步了解设计和实现细节，可以从 [`docs/README.md`](./docs/README.md) 开始。

## 当前适合贡献的方向

当前仓库适合用于：

- 设计讨论
- 原型迭代
- API 形态评审
- 服务发现迁移场景反馈
- 弱网与拓扑感知调度讨论

当前还不适合用于：

- 稳定性承诺
- 向后兼容承诺
- 生产部署指南
- 固定不变的 SDK 契约

如果你是早期贡献者，现阶段最有价值的反馈主要是：

- 服务注册与发现 API 设计
- 许可证与贡献模式建议
- 与现有业务框架的集成预期
- 在虚拟 IP 变化和节点不稳定场景下的行为预期
