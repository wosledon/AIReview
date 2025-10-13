using System.ComponentModel.DataAnnotations;

namespace AIReview.Shared.Enums;

public enum ReviewState
{
    Pending = 0,
    AIReviewing = 1,
    HumanReview = 2,
    Approved = 3,
    Rejected = 4,
    Merged = 5
}

public enum ReviewCommentSeverity
{
    Info = 0,
    Warning = 1,
    Error = 2
}

public enum ReviewCommentCategory
{
    Quality = 0,
    Security = 1,
    Performance = 2,
    Style = 3,
    Bug = 4
}

public enum ProjectMemberRole
{
    Owner = 0,
    Admin = 1,
    Developer = 2,
    Viewer = 3
}

/// <summary>
/// 改进建议类型
/// </summary>
public enum ImprovementType
{
    CodeQuality = 0,
    Performance = 1,
    Security = 2,
    Maintainability = 3,
    Testing = 4,
    Documentation = 5,
    Architecture = 6,
    BestPractices = 7,
    Refactoring = 8,
    ErrorHandling = 9
}

/// <summary>
/// 改进建议优先级
/// </summary>
public enum ImprovementPriority
{
    Low = 0,
    Medium = 1,
    High = 2,
    Critical = 3
}

/// <summary>
/// 变更类型
/// </summary>
public enum ChangeType
{
    Feature = 0,        // 新功能
    BugFix = 1,         // Bug修复
    Refactor = 2,       // 重构
    Performance = 3,    // 性能优化
    Security = 4,       // 安全修复
    Documentation = 5,  // 文档更新
    Test = 6,          // 测试相关
    Configuration = 7,  // 配置变更
    Dependency = 8,     // 依赖更新
    Breaking = 9        // 破坏性变更
}

/// <summary>
/// 业务影响级别
/// </summary>
public enum BusinessImpact
{
    None = 0,      // 无影响
    Low = 1,       // 低影响
    Medium = 2,    // 中等影响
    High = 3,      // 高影响
    Critical = 4   // 关键影响
}

/// <summary>
/// 技术影响级别
/// </summary>
public enum TechnicalImpact
{
    None = 0,      // 无影响
    Low = 1,       // 低影响
    Medium = 2,    // 中等影响
    High = 3,      // 高影响
    Critical = 4   // 关键影响
}

/// <summary>
/// 破坏性变更风险级别
/// </summary>
public enum BreakingChangeRisk
{
    None = 0,      // 无风险
    Low = 1,       // 低风险
    Medium = 2,    // 中等风险
    High = 3,      // 高风险
    Critical = 4   // 关键风险
}