export interface JobDetailsDto {
  id: string;
  state: string;
  createdAt?: string;
  startedAt?: string;
  finishedAt?: string;
  errorMessage?: string;
}

export interface RiskAssessment {
  id: number;
  reviewRequestId: number;
  overallRiskScore: number;
  complexityRisk: number;
  securityRisk: number;
  performanceRisk: number;
  maintainabilityRisk: number;
  testCoverageRisk: number;
  changedFilesCount: number;
  changedLinesCount: number;
  riskDescription?: string;
  mitigationSuggestions?: string;
  aiModelVersion?: string;
  confidenceScore?: number;
  createdAt: string;
  updatedAt: string;
}

// 辅助函数：根据综合风险评分计算风险等级
export const getRiskLevel = (overallRiskScore: number): RiskLevel => {
  if (overallRiskScore >= 80) return 'Critical';
  if (overallRiskScore >= 60) return 'High';
  if (overallRiskScore >= 40) return 'Medium';
  return 'Low';
};

export interface ImprovementSuggestion {
  id: number;
  reviewRequestId: number;
  filePath?: string;
  startLine?: number;
  endLine?: number;
  type: ImprovementType;
  priority: Priority;
  title: string;
  description: string;
  originalCode?: string;
  suggestedCode?: string;
  reasoning?: string;
  expectedBenefits?: string;
  implementationComplexity: number;
  impactAssessment?: string;
  isAccepted?: boolean;
  isIgnored?: boolean;
  userFeedback?: string;
  aiModelVersion?: string;
  confidenceScore?: number;
  createdAt: string;
  updatedAt: string;
}

export interface PullRequestChangeSummary {
  id: number;
  reviewRequestId: number;
  changeType: ChangeType;
  summary: string;
  detailedDescription?: string;
  keyChanges?: string;
  impactAnalysis?: string;
  businessImpact: BusinessImpact;
  technicalImpact: TechnicalImpact;
  breakingChangeRisk: BreakingChangeRisk;
  testingRecommendations?: string;
  deploymentConsiderations?: string;
  dependencyChanges?: string;
  performanceImpact?: string;
  securityImpact?: string;
  backwardCompatibility?: string;
  documentationRequirements?: string;
  changeStatistics?: ChangeStatistics;
  aiModelVersion?: string;
  confidenceScore?: number;
  createdAt: string;
  updatedAt: string;
}

export const RiskLevel = {
  Low: 'Low',
  Medium: 'Medium',
  High: 'High',
  Critical: 'Critical'
} as const;

export type RiskLevel = typeof RiskLevel[keyof typeof RiskLevel];

export const ImprovementType = {
  BugFix: 'BugFix',
  Performance: 'Performance',
  Security: 'Security',
  CodeQuality: 'CodeQuality',
  Maintainability: 'Maintainability',
  Testing: 'Testing',
  Documentation: 'Documentation',
  Refactoring: 'Refactoring'
} as const;

export type ImprovementType = typeof ImprovementType[keyof typeof ImprovementType];

export const Priority = {
  Low: 'Low',
  Medium: 'Medium',
  High: 'High',
  Critical: 'Critical'
} as const;

export type Priority = typeof Priority[keyof typeof Priority];

export const ChangeType = {
  Feature: 'Feature',
  BugFix: 'BugFix',
  Refactor: 'Refactor',
  Performance: 'Performance',
  Security: 'Security',
  Documentation: 'Documentation',
  Test: 'Test',
  Configuration: 'Configuration',
  Dependency: 'Dependency',
  Breaking: 'Breaking'
} as const;

export type ChangeType = typeof ChangeType[keyof typeof ChangeType];

export const BusinessImpact = {
  None: 'None',
  Low: 'Low',
  Medium: 'Medium',
  High: 'High',
  Critical: 'Critical'
} as const;

export type BusinessImpact = typeof BusinessImpact[keyof typeof BusinessImpact];

export const TechnicalImpact = {
  None: 'None',
  Low: 'Low',
  Medium: 'Medium',
  High: 'High',
  Critical: 'Critical'
} as const;

export type TechnicalImpact = typeof TechnicalImpact[keyof typeof TechnicalImpact];

export const BreakingChangeRisk = {
  None: 'None',
  Low: 'Low',
  Medium: 'Medium',
  High: 'High',
  Critical: 'Critical'
} as const;

export type BreakingChangeRisk = typeof BreakingChangeRisk[keyof typeof BreakingChangeRisk];

export interface ChangeStatistics {
  addedLines: number;
  deletedLines: number;
  modifiedFiles: number;
  addedFiles: number;
  deletedFiles: number;
}

export interface AnalysisData {
  riskAssessment?: RiskAssessment;
  improvementSuggestions: ImprovementSuggestion[];
  pullRequestSummary?: PullRequestChangeSummary;
}