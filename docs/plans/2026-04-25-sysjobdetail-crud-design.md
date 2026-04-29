# SysJobDetail CRUD API 设计文档

## 概述

为 `SysJobDetail` 任务详情实体实现完整的 CRUD API，包括创建、列表查询、详情查看、更新、删除、启用/禁用功能。JobType 关联的 HttpConfig/PluginConfig 在创建/更新时一并处理。

## 1. DTOs 设计

### 文件结构
```
DTOs/SysJobDetail/
├── SysJobDetailCreateInput.cs
├── SysJobDetailUpdateInput.cs
├── SysJobDetailOutput.cs
├── SysJobDetailQueryInput.cs
├── HttpConfigInput.cs
├── HttpConfigOutput.cs
├── PluginConfigInput.cs
└── PluginConfigOutput.cs
```

### 详细设计

**SysJobDetailCreateInput** — 创建入参
```csharp
public class SysJobDetailCreateInput
{
    public long GroupId { get; set; }              // 必填
    public string JobName { get; set; }            // 必填
    public int JobType { get; set; }              // 1=内置, 2=HTTP, 3=插件
    public string Description { get; set; }
    public int JobState { get; set; } = 1;         // 1=正常, 0=暂停
    public string TimezoneId { get; set; }
    public bool DisallowConcurrent { get; set; } = true;
    public int MisfireStrategy { get; set; } = 0;
    public int MaxRetry { get; set; } = 0;
    public int RetryBackoffSeconds { get; set; } = 0;

    // 关联配置 - JobType=2 时 HttpConfig 必填, JobType=3 时 PluginConfig 必填
    public HttpConfigInput HttpConfig { get; set; }
    public PluginConfigInput PluginConfig { get; set; }
}
```

**SysJobDetailUpdateInput** — 更新入参
```csharp
public class SysJobDetailUpdateInput
{
    public long Id { get; set; }                  // 必填
    public long GroupId { get; set; }
    public string JobName { get; set; }
    public int JobType { get; set; }
    public string Description { get; set; }
    public int JobState { get; set; } = 1;
    public string TimezoneId { get; set; }
    public bool DisallowConcurrent { get; set; } = true;
    public int MisfireStrategy { get; set; } = 0;
    public int MaxRetry { get; set; } = 0;
    public int RetryBackoffSeconds { get; set; } = 0;

    // 关联配置
    public HttpConfigInput HttpConfig { get; set; }
    public PluginConfigInput PluginConfig { get; set; }
}
```

**SysJobDetailOutput** — 出参
```csharp
public class SysJobDetailOutput
{
    public long Id { get; set; }
    public long GroupId { get; set; }
    public string JobKey { get; set; }
    public string JobName { get; set; }
    public int JobType { get; set; }
    public int JobState { get; set; }
    public string TimezoneId { get; set; }
    public bool DisallowConcurrent { get; set; }
    public int MisfireStrategy { get; set; }
    public int MaxRetry { get; set; }
    public int RetryBackoffSeconds { get; set; }
    public int? LastRunStatus { get; set; }
    public DateTime? LastRunTime { get; set; }
    public int? LastRunDurationMs { get; set; }
    public int Status { get; set; }
    public string Description { get; set; }
    public string CreateBy { get; set; }
    public DateTime CreateTime { get; set; }
    public string UpdateBy { get; set; }
    public DateTime? UpdateTime { get; set; }

    // 关联数据
    public SysJobGroupOutput JobGroup { get; set; }
    public HttpConfigOutput HttpConfig { get; set; }
    public PluginConfigOutput PluginConfig { get; set; }
}
```

**SysJobDetailQueryInput** — 列表查询入参
```csharp
public class SysJobDetailQueryInput
{
    public long? GroupId { get; set; }            // 按分组过滤
    public int? JobType { get; set; }            // 按任务类型过滤
    public int? Status { get; set; }             // 按状态过滤
    public string Keyword { get; set; }           // 搜索 JobName
    public int PageIndex { get; set; } = 1;
    public int PageSize { get; set; } = 20;
}
```

**HttpConfigInput / HttpConfigOutput** — HTTP 配置
```csharp
public class HttpConfigInput
{
    public string RequestMethod { get; set; }     // GET, POST, PUT, DELETE, PATCH
    public string ContentType { get; set; }
    public string RequestUrl { get; set; }
    public string RequestHeaders { get; set; }
    public string RequestBody { get; set; }
    public int TimeoutSeconds { get; set; }
    public int RetryOnFailure { get; set; }
    public int RetryCount { get; set; }
    public int RetryBackoffSeconds { get; set; }
}

public class HttpConfigOutput
{
    public long Id { get; set; }
    public long JobId { get; set; }
    public string RequestMethod { get; set; }
    public string ContentType { get; set; }
    public string RequestUrl { get; set; }
    public string RequestHeaders { get; set; }
    public string RequestBody { get; set; }
    public int TimeoutSeconds { get; set; }
    public int RetryOnFailure { get; set; }
    public int RetryCount { get; set; }
    public int RetryBackoffSeconds { get; set; }
    public DateTime CreateTime { get; set; }
}
```

**PluginConfigInput / PluginConfigOutput** — 插件配置
```csharp
public class PluginConfigInput
{
    public string PackageName { get; set; }
    public string Version { get; set; }
    public string StoragePath { get; set; }
    public string Hash { get; set; }
    public string AssemblyPath { get; set; }
    public string TypeFullName { get; set; }
    public string MethodName { get; set; }
    public int Status { get; set; }
    public string Remark { get; set; }
    public List<PluginParamInput> Params { get; set; }
}

public class PluginParamInput
{
    public string ParamName { get; set; }
    public string ParamType { get; set; }
    public string ParamValueJson { get; set; }
}

public class PluginConfigOutput
{
    public long Id { get; set; }
    public long JobId { get; set; }
    public string PackageName { get; set; }
    public string Version { get; set; }
    public string StoragePath { get; set; }
    public string Hash { get; set; }
    public string AssemblyPath { get; set; }
    public string TypeFullName { get; set; }
    public string MethodName { get; set; }
    public int Status { get; set; }
    public string Remark { get; set; }
    public DateTime? UpdateTime { get; set; }
    public DateTime CreateTime { get; set; }
    public List<PluginParamOutput> Params { get; set; }
}

public class PluginParamOutput
{
    public long Id { get; set; }
    public string ParamName { get; set; }
    public string ParamType { get; set; }
    public string ParamValueJson { get; set; }
}
```

## 2. Service 层

### 接口设计
```csharp
public interface ISysJobDetailService
{
    Task<PageList<SysJobDetailOutput>> GetPageListAsync(SysJobDetailQueryInput query, CancellationToken ct = default);
    Task<SysJobDetailOutput?> GetByIdAsync(long id, CancellationToken ct = default);
    Task<SysJobDetailOutput> CreateAsync(SysJobDetailCreateInput input, CancellationToken ct = default);
    Task<SysJobDetailOutput> UpdateAsync(SysJobDetailUpdateInput input, CancellationToken ct = default);
    Task<bool> DeleteAsync(long id, CancellationToken ct = default);
    Task<bool> EnableAsync(long id, CancellationToken ct = default);
    Task<bool> DisableAsync(long id, CancellationToken ct = default);
}
```

### 核心逻辑

**CreateAsync**:
1. 校验 `GroupId` 存在且未被软删除
2. 校验 `JobName` 不重复（同 GroupId 下）
3. 根据 `JobType` 校验关联配置必填/可选
4. 生成 `JobKey = $"{GroupName}:{JobName}"`
5. 事务插入 `SysJobDetail` + `SysJobHttpConfig` 或 `SysJobPlugin`
6. 查询返回完整 Output（含 JobGroup 导航属性）

**UpdateAsync**:
1. 校验 JobDetail 存在且未软删除
2. 校验 JobName 不与其他记录冲突
3. 更新基础字段
4. 关联配置：先删除旧配置，再根据 JobType 插入新配置
5. 返回更新后完整 Output

**DeleteAsync**:
- 软删除（`IsDeleted = true`）

**EnableAsync / DisableAsync**:
- 仅更新 `Status` 字段（1=启用，0=禁用）

**GetPageListAsync**:
- 支持分页 + 多条件过滤
- 查询包含导航属性 `JobGroup`
- 按 `CreateTime` 倒序

## 3. Controller 层

**路由**: `api/sys-job-detail`

| 方法 | 路由 | 说明 | 返回类型 |
|------|------|------|----------|
| GET | `/list` | 分页列表 | `PageList<SysJobDetailOutput>` |
| GET | `/{id}` | 详情 | `SysJobDetailOutput?` |
| POST | `` | 创建 | `SysJobDetailOutput` |
| PUT | `` | 更新 | `SysJobDetailOutput` |
| DELETE | `/{id}` | 删除 | `bool` |
| POST | `/{id}/enable` | 启用 | `bool` |
| POST | `/{id}/disable` | 禁用 | `bool` |

## 4. 文件清单

| 文件 | 路径 |
|------|------|
| DTOs | `DTOs/SysJobDetail/*.cs` |
| Interface | `Services/Abstractions/ISysJobDetailService.cs` |
| Implementation | `Services/SysJobDetailService.cs` |
| Controller | `Controllers/SysJobDetailController.cs` |

## 5. 实现顺序

1. DTOs（入参/出参）
2. MapsterConfig 映射配置
3. Service 接口 + 实现
4. Controller
5. 单元测试