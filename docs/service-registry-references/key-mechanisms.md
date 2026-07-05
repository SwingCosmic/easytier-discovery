# 关键算法与机制清单

本文档列出当前设计里最值得持续跟踪和补资料的机制主题。

## 1. 三段式租约与状态迁移

关注点：

- `ttl_healthy`
- `ttl_suspect`
- `ttl_delete`
- `healthy -> degraded -> suspect -> unreachable -> dead`

## 2. 多观察者怀疑投票

关注点：

- 观察者分配策略
- 票据过期
- 同故障域降权
- 续约仍存在时的保护规则

## 3. endpoint 评分拆解

关注点：

- 可用性评分
- 角色因子
- 网络因子
- 拓扑/地域因子
- 业务权重
- 熔断因子
- 粘性因子

## 4. watch + 本地缓存 + 空保护

关注点：

- 断连后的缓存可用性
- watch 重连与补偿
- 健康列表归零时的保护策略

## 5. 发现接口与业务调用边界

关注点：

- `selectOneHealthyInstance`
- `selectManyHealthyInstances`
- `reportCallResult`
- 是否只推荐，不代理调用
