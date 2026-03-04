# Avalonia 多设备多通道信号采集与无损压缩存储系统设计方案（V3）

## 1. 项目概述

本方案用于指导一个基于 Avalonia 的桌面系统建设，目标在 Windows 7 与 Windows 10/11 上运行，首期实现“可扩展骨架 + 可运行基础页面 + 完整接口留白”，后续可平滑补齐业务功能与算法细节。

系统核心目标：

1. 提供多类型信号源（正弦、噪声、直流、音频、缓变）。
2. 支持多设备、多通道并发传输（SDK 回调形式）。
3. 信号处理页接收 SDK 数据后进行预处理与压缩组合。
4. 实现无损压缩存储，支持多算法和参数化配置。
5. 构建可移植、可拓展、可插件化架构。

---

## 2. 需求汇总

### 2.1 功能需求

#### A. 数据源模块
- 支持信号类型：
  - 正弦信号
  - 噪声信号
  - 直流信号
  - 音频信号
  - 缓变信号
- 支持多设备、多通道配置与并发传输。
- 数据输出采用 SDK 回调机制。

#### B. 信号处理模块
- 能实时接收 SDK 回调数据。
- 支持预处理技术：
  - 一阶差分编码
  - 二阶差分编码
  - 线性预测编码（LPC）
- 支持压缩算法：
  - ZSTD
  - LZ4
  - Snappy
  - Zlib
  - LZ4_HC
  - Bzip2
- 支持预处理与压缩算法组合。
- 支持算法参数配置（如 ZSTD 压缩等级、窗口大小等）。
- 支持无损恢复校验。

#### C. 页面需求（当前阶段）
- 仅搭建基础页面与交互骨架。
- 预留完整接口，业务逻辑后续补充。

---

### 2.2 非功能需求

- **兼容性**：Windows 7 / 10 / 11。
- **性能**：满足多设备多通道实时写入与处理。
- **可靠性**：断电/异常恢复、块级校验、日志追踪。
- **可扩展性**：算法与设备协议插件化。
- **可维护性**：分层清晰、接口驱动、配置化。

---

## 3. 技术选型与兼容策略

- UI 框架：Avalonia（MVVM）
- 语言：C#
- 运行时建议：.NET 6（兼顾 Win7）
- 架构风格：分层架构 + 依赖注入 + 插件化接口

兼容建议：

1. 发布采用 self-contained，降低目标环境依赖。
2. 提供 x86 / x64 双架构包。
3. 对音频采集与高性能压缩库做“能力探测 + 降级策略”。
4. 对原生库（尤其压缩算法依赖）提前做 Win7 可用性验证。

---

## 4. 总体架构设计

### 4.1 分层结构

1. **Presentation（界面层）**
   - Avalonia View + ViewModel
   - 配置下发、状态展示、指标可视化

2. **Application（应用服务层）**
   - 流程编排、任务生命周期、错误处理
   - 管道配置管理（Profile）

3. **Domain/Contracts（契约层）**
   - 统一数据模型与接口定义
   - 设备、通道、帧、处理链契约

4. **Infrastructure（基础设施层）**
   - SDK 适配器
   - 压缩器实现
   - 存储引擎实现
   - 日志、配置持久化

---

### 4.2 核心数据流

`Signal SDK -> Frame Buffer -> Preprocessor -> Compressor -> Storage Writer -> Index/Metadata`

---

## 5. 数据源 SDK 设计

### 5.1 多设备多通道模型

- Device：设备实例（采集卡/虚拟源/音频设备）
- Channel：设备下通道（物理/逻辑）
- SignalFrame：最小传输单元（帧）

每帧必须包含：
- `DeviceId`
- `ChannelId`
- `TimestampUtc`
- `Sequence`
- `SampleRate`
- `DataType`
- `Payload`

### 5.2 SDK 接口建议

- `ISignalSourceSdk`
  - `Initialize(SourceSdkConfig config)`
  - `Start()`
  - `Stop()`
  - `OnFramesReceived(...)`（支持批量）
  - `OnStatusChanged(...)`

### 5.3 传输与稳定性机制

- 回调 + 内部有界队列（防抖/削峰）
- 背压策略可选：
  - 阻塞上游
  - 丢旧
  - 丢新
- 每通道独立序号，便于检测丢帧与乱序
- 状态事件包含延迟、吞吐、丢帧计数

---

## 6. 信号处理引擎设计

### 6.1 处理管线

`Frame -> PreprocessChain -> Compress -> Verify -> Persist`

### 6.2 预处理算法（无损前提）

- 一阶差分：$d_1[n] = x[n] - x[n-1]$
- 二阶差分：$d_2[n] = x[n] - 2x[n-1] + x[n-2]$
- LPC 残差：$\hat{x}[n]=\sum_{k=1}^{p} a_kx[n-k],\ e[n]=x[n]-\hat{x}[n]$

说明：预处理结果必须可逆，禁止有损量化进入无损主链路。

### 6.3 压缩算法抽象

统一接口：
- `ICompressor`
  - `Compress(input, options)`
  - `Decompress(input, options)`
  - `ValidateLossless(original, restored)`

支持实现：
- ZSTD / LZ4 / Snappy / Zlib / LZ4_HC / Bzip2

### 6.4 组合策略

支持：
- None + Compress
- Diff1 + Compress
- Diff2 + Compress
- LPC + Compress

支持按：
- 全局
- 设备
- 通道

三种粒度配置。

---

## 7. 参数配置设计

### 7.1 预处理参数

- `Method`: None / Diff1 / Diff2 / LPC
- `LpcOrder`
- `BlockSamples`
- `EnableReversibleCheck`

### 7.2 压缩参数

- `Algorithm`
- `Level`（如 ZSTD 级别）
- `WindowSize`
- `BlockSize`
- `Checksum`
- `ThreadCount`

### 7.3 管道参数

- `Scope`: Global / Device / Channel
- `StoragePath`
- `ChunkSize`
- `RotationPolicy`
- `IntegrityPolicy`

---

## 8. 存储方案设计（重点）

### 8.1 推荐结论

主存储采用**自定义分块二进制容器格式**（建议扩展名：`.sdf`，Signal Data Format）。

原因：
- 高吞吐实时写入
- 支持多设备多通道混合封装
- 块级参数与算法元数据可追溯
- 支持块级校验与快速定位
- 更易实现 Win7 稳定部署

---

### 8.2 `.sdf` 逻辑结构

1. `FileHeader`
2. `MetadataBlock`
3. `DataChunk[]`
4. `ChunkIndex`
5. `Footer`

#### A. FileHeader（固定头）
- Magic
- FormatVersion
- Endian
- CreateTimeUtc
- Project/TaskId
- MetadataOffset

#### B. MetadataBlock（可变）
- 设备列表与通道拓扑
- 每通道采样率、数据类型、量程
- 默认处理链配置
- 运行环境信息（可选）

#### C. DataChunk（核心块）
每块包含：
- `ChunkId`
- `DeviceId` / `ChannelId`（或通道组标识）
- `TimeStart` / `TimeEnd`
- `SampleCount`
- `PreprocessType` + 参数
- `CompressionType` + 参数
- `CompressedPayload`
- `Checksum`

#### D. ChunkIndex（索引）
- 按时间、设备、通道映射到文件偏移
- 支持快速回放和按条件抽取

#### E. Footer（尾）
- 索引起始偏移
- 全文件摘要
- EndMarker

---

### 8.3 分块与轮转建议

- Chunk 大小：256KB ~ 1MB（默认 512KB）
- 文件轮转：
  - 按大小（如 1GB）
  - 或按时间（如 5 分钟）
- 命名建议：
  - `TaskId_yyyyMMdd_HHmmss_partNN.sdf`

---

### 8.4 无损校验策略

- 写入前后随机抽样校验（在线）
- 批量离线全量校验（离线）
- 校验维度：
  - 解压后字节一致
  - 通道序号连续
  - 时间戳单调

---

## 9. 页面设计（基础版）

### 9.1 主页面
- 左侧导航：数据源 / 处理 / 存储 / 日志
- 右侧内容区：模块页切换
- 底部状态栏：运行状态、错误摘要、吞吐信息

### 9.2 数据源页面
- 设备管理：添加/删除/启停
- 通道配置：采样率、数据类型、信号类型
- 信号参数：频率、幅值、偏置、斜率、音频输入设备
- SDK 状态：连接、回调频率、丢帧、延迟

### 9.3 信号处理页面
- 输入监控：设备/通道数据到达统计
- 预处理配置：Diff1/Diff2/LPC 参数
- 压缩配置：算法切换与高级参数
- 组合配置：Profile 保存与加载
- 输出监控：压缩比、写入速率、CPU 占用、错误日志

---

## 10. 目录与模块边界建议

- `Host`：启动与 DI
- `Contracts`：接口与数据模型
- `SignalSource.SDK.Abstractions`：SDK 契约
- `SignalSource.SDK.Impl`：各信号源实现
- `Processing.Abstractions`：预处理/压缩/管道接口
- `Processing.Impl`：算法实现
- `Storage.Sdf`：`.sdf` 写入与索引
- `UI.Avalonia`：页面与 ViewModel

---

## 11. 关键接口清单（建议）

- `ISignalSourceSdk`
- `ISignalDevice`
- `ISignalChannel`
- `IPreprocessor`
- `ICompressor`
- `IProcessingPipeline`
- `IStorageWriter`
- `IChunkIndexer`
- `IIntegrityValidator`
- `IPipelineProfileRepository`

---

## 12. 异常处理与可靠性设计

- 统一错误码：采集错误、处理错误、写入错误、校验错误
- 关键链路重试：可配置次数与退避策略
- 队列积压告警：阈值触发
- 文件损坏恢复：通过 ChunkIndex 与块校验做局部恢复

---

## 13. 性能与测试建议

### 13.1 指标
- 输入吞吐（frames/s, MB/s）
- 端到端延迟
- 压缩比
- CPU / 内存占用
- 丢帧率

### 13.2 测试场景
1. 单设备单通道基线测试
2. 多设备多通道压力测试
3. 不同算法组合压缩率对比
4. 断电/崩溃恢复测试
5. 解压一致性回归测试

---

## 14. 分阶段实施计划

### P1（骨架阶段）
- 项目结构、接口、基础页面
- 5 类信号源模拟输出
- SDK 回调链路打通

### P2（处理阶段）
- Diff1/Diff2/LPC 基础实现
- 6 种压缩算法接入
- 组合配置与参数下发

### P3（存储阶段）
- `.sdf` 写入器与索引
- 轮转策略与恢复工具
- 无损校验能力完善

### P4（增强阶段）
- 插件化热加载
- 更完整监控面板
- 性能优化与发布打包

---

## 15. 最终结论

该方案以“**多设备多通道 + SDK 回调 + 可组合无损压缩 + 分块二进制容器**”为主线，能满足当前骨架建设目标，并为后续算法迭代、设备接入和跨平台迁移留足扩展空间。  
在存储格式上，`.sdf` 作为主格式最平衡实时性、可追溯性与工程可控性，优于通用文本或单场景格式用于本项目主链路。