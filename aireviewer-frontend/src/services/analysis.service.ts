import { apiClient } from './api-client';
import type { AnalysisData, RiskAssessment, ImprovementSuggestion, PullRequestChangeSummary, JobDetailsDto } from '../types/analysis';

export const analysisService = {
  // 获取评审的完整分析数据
  async getAnalysisData(reviewId: number): Promise<AnalysisData> {
    const [riskAssessment, improvementSuggestions, pullRequestSummary] = await Promise.allSettled([
      this.getRiskAssessment(reviewId).catch(() => null),
      this.getImprovementSuggestions(reviewId).catch(() => []),
      this.getPullRequestSummary(reviewId).catch(() => null),
    ]);

    return {
      riskAssessment: riskAssessment.status === 'fulfilled' && riskAssessment.value ? riskAssessment.value : undefined,
      improvementSuggestions: improvementSuggestions.status === 'fulfilled' ? improvementSuggestions.value : [],
      pullRequestSummary: pullRequestSummary.status === 'fulfilled' && pullRequestSummary.value ? pullRequestSummary.value : undefined,
    };
  },

  // 风险评估
  async getRiskAssessment(reviewId: number): Promise<RiskAssessment | null> {
    const response = await apiClient.get<RiskAssessment>(`/analysis/reviews/${reviewId}/risk-assessment`);
    return response;
  },

  async generateRiskAssessment(reviewId: number): Promise<RiskAssessment> {
    const response = await apiClient.post<RiskAssessment>(`/analysis/reviews/${reviewId}/risk-assessment`);
    return response;
  },

  // 改进建议
  async getImprovementSuggestions(reviewId: number): Promise<ImprovementSuggestion[]> {
    const response = await apiClient.get<ImprovementSuggestion[]>(`/analysis/reviews/${reviewId}/improvement-suggestions`);
    return response;
  },

  async generateImprovementSuggestions(reviewId: number): Promise<ImprovementSuggestion[]> {
    const response = await apiClient.post<ImprovementSuggestion[]>(`/analysis/reviews/${reviewId}/improvement-suggestions`);
    return response;
  },

  async updateSuggestionFeedback(suggestionId: number, feedback: {
    isAccepted?: boolean;
    isIgnored?: boolean;
    userFeedback?: string;
  }): Promise<ImprovementSuggestion> {
    const response = await apiClient.patch<ImprovementSuggestion>(`/analysis/improvement-suggestions/${suggestionId}`, feedback);
    return response;
  },

  // PR变更摘要
  async getPullRequestSummary(reviewId: number): Promise<PullRequestChangeSummary | null> {
    const response = await apiClient.get<PullRequestChangeSummary>(`/analysis/reviews/${reviewId}/change-summary`);
    return response;
  },

  async generatePullRequestSummary(reviewId: number): Promise<PullRequestChangeSummary> {
    const response = await apiClient.post<PullRequestChangeSummary>(`/analysis/reviews/${reviewId}/change-summary`);
    return response;
  },

  // 生成综合分析报告
  async generateComprehensiveAnalysis(reviewId: number): Promise<AnalysisData> {
    const response = await apiClient.post<AnalysisData>(`/analysis/reviews/${reviewId}/comprehensive`);
    return response;
  },

  // 异步分析方法 - 返回作业ID而不是直接结果
  async generateRiskAssessmentAsync(reviewId: number): Promise<JobDetailsDto> {
    const response = await apiClient.post<JobDetailsDto>(`/analysis/reviews/${reviewId}/risk-assessment`);
    return response;
  },

  async generateImprovementSuggestionsAsync(reviewId: number): Promise<JobDetailsDto> {
    const response = await apiClient.post<JobDetailsDto>(`/analysis/reviews/${reviewId}/improvement-suggestions`);
    return response;
  },

  async generatePullRequestSummaryAsync(reviewId: number): Promise<JobDetailsDto> {
    const response = await apiClient.post<JobDetailsDto>(`/analysis/reviews/${reviewId}/change-summary`);
    return response;
  },

  async generateComprehensiveAnalysisAsync(reviewId: number): Promise<JobDetailsDto> {
    const response = await apiClient.post<JobDetailsDto>(`/analysis/reviews/${reviewId}/comprehensive`);
    return response;
  },

  // 作业管理方法
  async getJobStatus(jobId: string): Promise<JobDetailsDto> {
    const response = await apiClient.get<JobDetailsDto>(`/analysis/job/${jobId}/status`);
    return response;
  },

  async cancelJob(jobId: string): Promise<{ message: string }> {
    const response = await apiClient.post<{ message: string }>(`/analysis/job/${jobId}/cancel`);
    return response;
  },

  // 轮询作业状态直到完成
  async pollJobStatus(jobId: string, onProgress?: (job: JobDetailsDto) => void): Promise<JobDetailsDto> {
    const poll = async (): Promise<JobDetailsDto> => {
      const job = await this.getJobStatus(jobId);
      
      if (onProgress) {
        onProgress(job);
      }

      // 如果作业已完成（成功或失败），返回最终状态
      if (job.state === 'Succeeded' || job.state === 'Failed') {
        return job;
      }

      // 等待2秒后继续轮询
      await new Promise(resolve => setTimeout(resolve, 2000));
      return poll();
    };

    return poll();
  },
};