// Token Usage & Cost tracking DTOs (keep in sync with backend AIReview.Shared/DTOs/TokenUsageDto.cs)

export interface TokenUsageRecordDto {
  id: number;
  userId?: string;
  projectId?: number;
  reviewRequestId?: number;
  provider: string;
  model: string;
  operationType: string;
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  promptCost: number;
  completionCost: number;
  totalCost: number;
  responseTimeMs?: number;
  isCached?: boolean;
  createdAt: string;
}

export interface TokenUsageStatisticsDto {
  totalRequests: number;
  totalTokens: number;
  totalCost: number;
  avgTokensPerRequest: number;
  avgCostPerRequest: number;
}

export interface ProviderUsageStatisticsDto {
  provider: string;
  model: string;
  requests: number;
  tokens: number;
  cost: number;
}

export interface OperationUsageStatisticsDto {
  operationType: string;
  requests: number;
  tokens: number;
  cost: number;
}

export interface DailyUsageTrendDto {
  date: string; // yyyy-MM-dd
  requests: number;
  tokens: number;
  cost: number;
}

export interface TokenUsageDashboardDto {
  statistics: TokenUsageStatisticsDto;
  providerStats: ProviderUsageStatisticsDto[];
  operationStats: OperationUsageStatisticsDto[];
  dailyTrends: DailyUsageTrendDto[];
}

export interface CostEstimateRequestDto {
  provider: string;
  model: string;
  promptTokens?: number;
  completionTokens?: number;
  promptChars?: number;
  completionChars?: number;
}

export interface CostEstimateResponseDto {
  promptTokens: number;
  completionTokens: number;
  totalTokens: number;
  promptCost: number;
  completionCost: number;
  totalCost: number;
}
