using AIReview.Shared.DTOs;

namespace AIReview.Core.Interfaces;

// 注意：该接口与现有 IGitService 能力重叠，后续将不再使用。
// 请统一通过 IGitService 完成克隆/拉取/同步等操作，
// 仓库状态查询请使用单独的状态服务或控制器查询接口。
// 保留该接口仅为兼容早期引用，勿继续扩展。
public interface IGitPullService
{
    /// <summary>
    /// 获取项目的仓库状态
    /// </summary>
    Task<GitRepositoryStatusDto?> GetRepositoryStatusAsync(int projectId);
    
    /// <summary>
    /// 拉取仓库代码
    /// </summary>
    Task<GitRepositoryStatusDto> PullRepositoryAsync(PullRepositoryRequest request, string userId);
    
    /// <summary>
    /// 克隆新仓库
    /// </summary>
    Task<GitRepositoryStatusDto> CloneRepositoryAsync(int projectId, int? credentialId, string userId);
    
    /// <summary>
    /// 取消正在进行的拉取
    /// </summary>
    Task CancelPullAsync(int projectId);
    
    /// <summary>
    /// 更新仓库状态
    /// </summary>
    Task UpdateRepositoryStatusAsync(int projectId, Action<GitRepositoryStatusDto> updateAction);
}
