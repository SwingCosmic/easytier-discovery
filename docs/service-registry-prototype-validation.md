# 原型验证 Runbook

本文档说明最小 Web 原型的 **启动方式、平台权限与排查要点**。  
“做到哪了”见 [plan](./service-registry-plan.md)；模块树与硬约定见 [AGENTS.md](../AGENTS.md)；目标设计见核心设计 / 应用层 / Bootstrap。

---

## 1. 验证目标

- 用 C# 原型托管 `easytier-core`，经 `easytier-cli` 读真实组网状态
- discovery 核心逻辑可在 `EtDiscovery.Core` 中单测，不依赖真实网络
- 完成 `registry + worker` 最小真实链路（发现 registry + 注册实例）

---

## 2. 启动要点

- **必须**提供 `--roles`（`registry` / `worker` / `client`，可组合）
- 其余优先读配置；`EtDiscovery` 与 `EasyTier` 分节
- `easytier-core`：生成 TOML + `-c`，另加 `--rpc-portal`
- 角色元数据只由 `--roles` 写入 `node_type_*`

配置字段见 [bootstrap 配置模型](./service-registry-bootstrap-discovery.md#5-配置模型)。  
硬约束见 [AGENTS.md §3](../AGENTS.md#3-代码与行为硬约定)；回归备忘见 [plan §3](./service-registry-plan.md#3-当前限制与运维假设)。

---

## 3. 推荐验证路径

### 3.1 Windows

1. `registry`：固定 `EasyTier.Ipv4`，`EtDiscovery.ListenUrl=http://0.0.0.0:8080`
2. 以管理员权限运行发布产物 `EtDiscovery.Web.exe --roles registry`（不要用 `dotnet xxx.dll` 期望 UAC）
3. `worker`：另一实例，`--roles worker`；`EasyTier.Peers` 指向 registry 入网地址；`RegistryCandidates` 可空以测 route metadata 发现
4. worker 可先 DHCP；不稳则写死 worker VIP
5. 检查：
   - `curl http://<registry-vip>:8080/health`
   - `curl http://<registry-vip>:8080/discovery/registry`
   - worker 日志中 selected registry；registry 上服务列表含 worker 实例

### 3.2 Linux

1. 确认 `/dev/net/tun` 与 `tun` 模块（`modprobe tun`）
2. 需要本机 VIP 时通常要 root 或等效权限
3. 再按上节启动 registry / worker

### 3.3 Docker / Kubernetes（真实 Linux）

镜像与样例在本仓库根：

- `Dockerfile` — multi-stage 构建 `EtDiscovery.Web` + 打入 `easytier-core` / `easytier-cli`
- `docker/entrypoint.sh` — 规范化 `ETDISCOVERY_ROLES` / `ETDISCOVERY_CONFIG_FILE`
- `docker/k8s/registry-sample.yaml` — ConfigMap + Deployment + Service 样例

**验证环境约定：**

- 只在 **真实 Linux 服务器** 上测镜像（不以 Docker Desktop / Windows 容器为通过标准）
- **优先 Kubernetes** 部署，确认节点/Pod 能否打通 EasyTier **虚拟 IP**
- 集群需 **kube-proxy 内核代理**（iptables 或 ipvs）正常工作；另需 TUN 与网络能力

构建（本仓库根目录）：

```bash
docker build -t etdiscovery:local .
```

运行要点：

| 项 | 说明 |
| --- | --- |
| 角色 | `ETDISCOVERY_ROLES=registry` 或 `--roles registry`（必填） |
| 运行模式 | `ETDISCOVERY_MODE=embedded`（registry 镜像默认；或 `--mode`）。取值 `embedded` / `sidecar` / `daemon`。契约见 [交互文档](./service-registry-app-runtime-interaction.md) |
| 配置 | 挂载 ConfigMap 到 `/config/appsettings.json`，或 `ETDISCOVERY_CONFIG_FILE`（运维面；业务用 Sdk 瘦配置） |
| 层级 env | `EtDiscovery__NetworkName`、`EasyTier__Ipv4`、`EasyTier__Peers__0` 等 |
| 设备/权限 | `/dev/net/tun`；`NET_ADMIN`（样例使用 `privileged: true`，可按节点策略收紧） |
| 健康检查 | `GET /health`（HTTP 8080） |

K8s 最小步骤示意：

```bash
# 节点已加载 tun；kube-proxy 为 iptables/ipvs 内核模式
kubectl apply -f docker/k8s/registry-sample.yaml
kubectl port-forward deploy/etdiscovery-registry 8080:8080
curl -s http://127.0.0.1:8080/health
```

关注 health 中的 `observedLocalVirtualIp`、`easyTier`、`privilegeChecklist`。  
Worker / 多服务联调契约见 [应用 ↔ Runtime 交互](./service-registry-app-runtime-interaction.md)；`/runtime/v1` 服务端进度见 [plan](./service-registry-plan.md)。

### 3.4 EasyTier 元数据抽查

```bash
easytier-cli -p <rpc> -n <instance> -o json node info
easytier-cli -p <rpc> -n <instance> -o json -v peer list
```

关注：

- 生成 TOML 含 `listeners`、`node_type_app_id` / `node_type_flags` 且与角色一致
- registry 的 listeners 含 `tcp://...:11010` 一类，而非只有 `ring://`
- 远端 peer list 可见 registry 的 app_id=1 与 registry bit
- 无标记 peer 不会被当成 registry

---

## 4. 权限与平台排查

硬约束摘要见 [AGENTS.md §3.3](../AGENTS.md#33-运维与联调约束)。此处只记排查现象：

- **Windows**：VIP/静态 Ipv4/DHCP 常需管理员；不足时见 `Failed to create adapter`、本机 `virtualIp` 空、DHCP 超时。仅 `EtDiscovery.Web.exe` 触发 UAC。
- **Linux**：本机 VIP 须 TUN；可选再需 `CAP_NET_ADMIN`。
- **DHCP**：`worker + dhcp` 是 EasyTier 本机自动选址，不是 registry 传统 DHCP；registry 单点勿盲目自开 DHCP。

---

## 5. 排查接口与字段

### 5.1 常用 HTTP

完整路径见 [应用层 API](./service-registry-application-layer.md#4-应用层-api)。验证常用：`/health`、`/easytier/peers`、`/discovery/registry`、`/discovery/services`、`/discovery/select`、`POST|DELETE /discovery/instances...`。

### 5.2 观测字段提示

- peers：`networkName`、`virtualIp`、`sameNetwork`、`inVirtualNetworkCidr`、`eligibleForDiscovery`、roles / `node_type_*`
- health：`configuredVirtualIp`、`dhcpEnabled`、权限 checklist、EasyTier 最近 stdout/stderr

### 5.3 网络筛选

`/discovery/*` 候选通常要求同网且 VIP 落在 `virtual-network-cidr` 内（以当前实现为准）。

---

## 6. 已知问题索引

联调回归表见 [plan §3.1](./service-registry-plan.md#31-联调已修问题备忘防回归)；硬约束见 [AGENTS.md §3](../AGENTS.md#3-代码与行为硬约定)。
