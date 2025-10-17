import { apiClient } from './api-client';
import type {
  TokenUsageDashboardDto,
  TokenUsageRecordDto,
  TokenUsageStatisticsDto,
  ProviderUsageStatisticsDto,
  OperationUsageStatisticsDto,
  DailyUsageTrendDto,
  CostEstimateRequestDto,
  CostEstimateResponseDto,
} from '../types/tokenUsage';

export const tokenUsageService = {
  // 当前用户仪表盘(可选日期范围)
  async getMyDashboard(params?: { startDate?: string; endDate?: string }) {
    const res = await apiClient.get<TokenUsageDashboardDto>('/TokenUsage/dashboard', { params });
    return res;
  },

  // 当前用户记录列表(分页)
  async getMyRecords(params?: { page?: number; pageSize?: number; startDate?: string; endDate?: string }) {
    const res = await apiClient.get<TokenUsageRecordDto[]>('/TokenUsage/records', { params });
    return res;
  },

  // 项目仪表盘
  async getProjectDashboard(projectId: number, params?: { startDate?: string; endDate?: string }) {
    const res = await apiClient.get<TokenUsageDashboardDto>(`/TokenUsage/projects/${projectId}/statistics`, { params });
    return res;
  },

  // 项目记录
  async getProjectRecords(projectId: number, params?: { page?: number; pageSize?: number; startDate?: string; endDate?: string }) {
    const res = await apiClient.get<TokenUsageRecordDto[]>(`/TokenUsage/projects/${projectId}/records`, { params });
    return res;
  },

  // Provider 统计
  async getProviderStats(params?: { startDate?: string; endDate?: string }) {
    const res = await apiClient.get<ProviderUsageStatisticsDto[]>('/TokenUsage/providers/statistics', { params });
    return res;
  },

  // Operation 统计
  async getOperationStats(params?: { startDate?: string; endDate?: string }) {
    const res = await apiClient.get<OperationUsageStatisticsDto[]>('/TokenUsage/operations/statistics', { params });
    return res;
  },

  // 趋势
  async getDailyTrends(params?: { startDate?: string; endDate?: string }) {
    const res = await apiClient.get<DailyUsageTrendDto[]>('/TokenUsage/trends/daily', { params });
    return res;
  },

  // 全局统计(管理员)
  async getGlobalStats(params?: { startDate?: string; endDate?: string }) {
    const res = await apiClient.get<TokenUsageStatisticsDto>('/TokenUsage/global/statistics', { params });
    return res;
  },

  // 费用估算
  async estimateCost(body: CostEstimateRequestDto) {
    const res = await apiClient.post<CostEstimateResponseDto>('/TokenUsage/estimate', body);
    return res;
  },
};

export type TokenUsageService = typeof tokenUsageService;
