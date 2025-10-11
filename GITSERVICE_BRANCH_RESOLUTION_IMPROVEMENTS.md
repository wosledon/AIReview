# GitService 分支解析改进

## 问题描述

用户遇到了以下 Git 错误：
```
fatal: ambiguous argument 'dev...main': unknown revision or path not in the working tree.
Use '--' to separate paths from revisions, like this:
'git <command> [<revision>...] -- [<file>...]'
```

这个错误表明在执行 `git diff dev...main` 时，Git 无法找到 `dev` 或 `main` 分支。

## 根本原因

1. **本地分支不存在**: 仓库可能没有本地的 `dev` 或 `main` 分支
2. **远程分支未同步**: 远程分支存在但本地没有对应的跟踪分支
3. **分支名称不匹配**: 实际分支名可能是 `origin/dev` 而不是 `dev`
4. **仓库状态不同步**: 本地仓库可能需要从远程同步最新信息

## 解决方案

我们为 `GitService.GetDiffBetweenRefsAsync` 方法实现了智能的分支解析和错误恢复机制：

### 1. 添加分支验证方法

```csharp
private async Task<bool> ValidateRefExistsAsync(string localPath, string refName)
{
    // 尝试解析本地引用
    var result = await ExecuteGitCommandAsync($"rev-parse --verify {refName}", localPath);
    if (result.Success) return true;

    // 检查远程分支
    var remoteResult = await ExecuteGitCommandAsync($"rev-parse --verify origin/{refName}", localPath);
    if (remoteResult.Success) return true;

    // 检查其他远程分支格式
    var remoteBranchResult = await ExecuteGitCommandAsync($"branch -r --list '*/{refName}'", localPath);
    return remoteBranchResult.Success && !string.IsNullOrWhiteSpace(remoteBranchResult.Output);
}
```

### 2. 添加远程分支获取方法

```csharp
private async Task<bool> TryFetchRemoteBranchAsync(string localPath, string branchName)
{
    // 从远程获取所有分支信息
    var fetchResult = await ExecuteGitCommandAsync("fetch origin", localPath);
    
    // 检查远程分支是否存在
    var remoteBranchCheck = await ExecuteGitCommandAsync($"branch -r --list 'origin/{branchName}'", localPath);
    
    // 如果存在，创建本地跟踪分支
    if (remoteBranchCheck.Success && !string.IsNullOrWhiteSpace(remoteBranchCheck.Output))
    {
        var checkoutResult = await ExecuteGitCommandAsync($"checkout -b {branchName} origin/{branchName}", localPath);
        return checkoutResult.Success;
    }
    
    return false;
}
```

### 3. 添加分支名称规范化方法

```csharp
private async Task<string> NormalizeRefNameAsync(string localPath, string refName)
{
    // 检查原始名称是否有效
    var directResult = await ExecuteGitCommandAsync($"rev-parse --verify {refName}", localPath);
    if (directResult.Success) return refName;

    // 尝试添加 origin/ 前缀
    var originResult = await ExecuteGitCommandAsync($"rev-parse --verify origin/{refName}", localPath);
    if (originResult.Success) return $"origin/{refName}";

    // 都失败则返回原始名称
    return refName;
}
```

### 4. 改进的差异获取流程

```csharp
public async Task<string?> GetDiffBetweenRefsAsync(int repositoryId, string @base, string head)
{
    // 1. 从远程同步最新信息
    await ExecuteGitCommandAsync("fetch origin", repository.LocalPath);

    // 2. 规范化分支引用名称
    var normalizedBase = await NormalizeRefNameAsync(repository.LocalPath, @base);
    var normalizedHead = await NormalizeRefNameAsync(repository.LocalPath, head);

    // 3. 验证分支存在性
    var baseExists = await ValidateRefExistsAsync(repository.LocalPath, normalizedBase);
    var headExists = await ValidateRefExistsAsync(repository.LocalPath, normalizedHead);

    // 4. 尝试获取缺失的分支
    if (!baseExists) await TryFetchRemoteBranchAsync(repository.LocalPath, @base);
    if (!headExists) await TryFetchRemoteBranchAsync(repository.LocalPath, head);

    // 5. 多重diff策略
    // 三点语法 (base...head) - 显示head分支的独有更改
    // 两点语法 (base..head) - 显示从base到head的所有更改  
    // 直接diff (base head) - 简单的两个引用比较
}
```

## 改进效果

### 🔧 **错误恢复能力**
- **自动远程同步**: 遇到分支不存在时自动从远程获取
- **智能分支解析**: 自动处理 `dev` vs `origin/dev` 的差异
- **多重回退策略**: 三种不同的diff语法确保兼容性

### 📝 **详细日志记录**
- **分支规范化日志**: 记录分支名称转换过程
- **操作成功日志**: 记录每种diff策略的执行结果
- **错误诊断日志**: 详细记录失败原因便于排查

### 🚀 **用户体验提升**
- **自动化处理**: 用户无需手动处理分支同步问题
- **透明恢复**: 系统自动尝试多种方法获取diff
- **详细反馈**: 清晰的日志帮助理解处理过程

## 支持的场景

1. **本地分支存在**: `dev`, `main` 直接使用
2. **远程分支存在**: `origin/dev`, `origin/main` 自动识别
3. **分支需要获取**: 自动从远程创建本地跟踪分支
4. **提交SHA**: 支持直接使用提交哈希值进行比较
5. **标签引用**: 支持使用Git标签作为比较基准

## 测试建议

为了验证改进效果，建议测试以下场景：

1. **正常分支比较**: `main` vs `dev`
2. **远程分支比较**: `origin/main` vs `origin/feature-branch`
3. **混合引用比较**: `main` vs `origin/dev`
4. **提交SHA比较**: `abc123` vs `def456`
5. **不存在分支**: 触发自动获取逻辑

通过这些改进，GitService 现在能够智能处理各种Git分支和引用解析问题，显著提升了系统的健壮性和用户体验。