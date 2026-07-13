# 待讨论分歧点

只记录 **尚未冻结** 的设计项。已确认内容见 [plan §1](./service-registry-plan.md#1-已确认内容)。  
应用 ↔ runtime、mode、SDK、配置拆分等 **已冻结** 结论见 [应用 ↔ Runtime 交互](./service-registry-app-runtime-interaction.md)（不再在此复述）。

## 1. 语言与实现形态

- 核心逻辑是否统一用 Rust；registry 控制面是否也统一 Rust
- 首版是否同步产出 protobuf/IDL
- 是否首版就保留自定义评分器/健康检查器等插件点

倾向（未冻结）：核心算法库 + 独立控制面进程；多语言首版 Node.js / Java / .NET。

## 2. 传输与进程边界（实现细节）

契约已定；下列实现选型未冻结：

- 控制协议：长期 gRPC 为主 vs 继续以 HTTP/JSON 为主（当前原型与 Sdk 为 HTTP/JSON）
- `daemon` 连接已有 EasyTier 的配置面（rpc-portal、观测路径、权限）
- sidecar 与业务进程的本地鉴权（UDS、token 等）

## 3. Registry 职责上限

- 是否允许兜底代理转发，还是只做目录与评分
- 配置持久化：内建存储还是先抽象接口
- 区域协调、冲突合并建议、策略分发、审计的边界

倾向：首版允许弱兜底中转，不做强耦合网关；持久化先接口化。

## 4. 评分与策略可配置程度

- 权重：固定内置 vs 按命名空间/服务有限配置
- 策略：枚举 vs 脚本化扩展
- 网络策略与业务策略是否拆两层

倾向：少量内置策略 + 有限权重配置，不开放任意脚本。

## 5. Actor / Serverless 首版范围

- 是否只预留数据模型
- placement 归属控制面还是独立子系统
- sticky / 迁移 / activation lease 是否抬高首版复杂度

倾向：Actor 作扩展方向，不进首版闭环核心。

## 6. 移动端深度

- 字段预留 vs 正式 SDK
- 嵌入式 vs 仅 client 角色
- iOS/Android 上 EasyTier 生命周期与 TUN 权限归属

倾向：首版不做正式移动端支持。
