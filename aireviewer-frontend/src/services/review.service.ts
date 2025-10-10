import { apiClient } from './api-client';
import type { 
  Review, 
  CreateReviewRequest, 
  UpdateReviewRequest, 
  ReviewComment,
  AddCommentRequest,
  UpdateCommentRequest,
  AIReviewResult,
  ReviewQueryParameters,
  PagedResult 
} from '../types/review';

export class ReviewService {
  async getReviews(params?: ReviewQueryParameters): Promise<PagedResult<Review>> {
    const queryParams = new URLSearchParams();
    
    if (params?.projectId) queryParams.append('projectId', params.projectId.toString());
    if (params?.status) queryParams.append('status', params.status);
    if (params?.authorId) queryParams.append('authorId', params.authorId);
    if (params?.createdAfter) queryParams.append('createdAfter', params.createdAfter);
    if (params?.createdBefore) queryParams.append('createdBefore', params.createdBefore);
    if (params?.page) queryParams.append('page', params.page.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());

    const query = queryParams.toString();
    const url = query ? `/reviews?${query}` : '/reviews';
    
    // 获取 API 响应并提取 data 字段
    const response = await apiClient.get<{ success: boolean; data: PagedResult<Review> }>(url);
    return response.data;
  }

  async getReview(id: number): Promise<Review> {
    const response = await apiClient.get<{ success: boolean; data: Review }>(`/reviews/${id}`);
    return response.data;
  }

  async createReview(request: CreateReviewRequest): Promise<Review> {
    const response = await apiClient.post<{ success: boolean; data: Review }>('/reviews', request);
    return response.data;
  }

  async updateReview(id: number, request: UpdateReviewRequest): Promise<Review> {
    const response = await apiClient.put<{ success: boolean; data: Review }>(`/reviews/${id}`, request);
    return response.data;
  }

  async deleteReview(id: number): Promise<void> {
    await apiClient.delete<void>(`/reviews/${id}`);
  }

  async getReviewComments(reviewId: number): Promise<ReviewComment[]> {
    const response = await apiClient.get<{ success: boolean; data: ReviewComment[] }>(`/reviews/${reviewId}/comments`);
    return response.data;
  }

  async addComment(reviewId: number, request: AddCommentRequest): Promise<ReviewComment> {
    const response = await apiClient.post<{ success: boolean; data: ReviewComment }>(`/reviews/${reviewId}/comments`, request);
    return response.data;
  }

  async updateReviewComment(reviewId: number, commentId: number, request: UpdateCommentRequest): Promise<ReviewComment> {
    const response = await apiClient.put<{ success: boolean; data: ReviewComment }>(`/reviews/${reviewId}/comments/${commentId}`, request);
    return response.data;
  }

  async deleteReviewComment(reviewId: number, commentId: number): Promise<void> {
    await apiClient.delete<void>(`/reviews/${reviewId}/comments/${commentId}`);
  }

  async startAIReview(reviewId: number): Promise<void> {
    await apiClient.post<void>(`/reviews/${reviewId}/ai-review`);
  }

  async getAIReviewResult(reviewId: number): Promise<AIReviewResult> {
    const response = await apiClient.get<{ success: boolean; data: AIReviewResult }>(`/reviews/${reviewId}/ai-result`);
    return response.data;
  }

  async approveReview(reviewId: number): Promise<Review> {
    const response = await apiClient.post<{ success: boolean; data: Review }>(`/reviews/${reviewId}/approve`);
    return response.data;
  }

  async rejectReview(reviewId: number, reason?: string): Promise<Review> {
    const response = await apiClient.post<{ success: boolean; data: Review }>(`/reviews/${reviewId}/reject`, { reason });
    return response.data;
  }

  async requestChanges(reviewId: number, reason: string): Promise<Review> {
    const response = await apiClient.post<{ success: boolean; data: Review }>(`/reviews/${reviewId}/request-changes`, { reason });
    return response.data;
  }

  async getMyReviews(): Promise<Review[]> {
    const response = await apiClient.get<{ success: boolean; data: Review[] }>('/reviews/my');
    return response.data;
  }

  async getReviewsForProject(projectId: number, params?: Omit<ReviewQueryParameters, 'projectId'>): Promise<PagedResult<Review>> {
    const queryParams = new URLSearchParams();
    
    if (params?.status) queryParams.append('status', params.status);
    if (params?.authorId) queryParams.append('authorId', params.authorId);
    if (params?.createdAfter) queryParams.append('createdAfter', params.createdAfter);
    if (params?.createdBefore) queryParams.append('createdBefore', params.createdBefore);
    if (params?.page) queryParams.append('page', params.page.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());

    const query = queryParams.toString();
    const url = query ? `/projects/${projectId}/reviews?${query}` : `/projects/${projectId}/reviews`;
    
    // 获取 API 响应并提取 data 字段
    const response = await apiClient.get<{ success: boolean; data: PagedResult<Review> }>(url);
    return response.data;
  }
}

export const reviewService = new ReviewService();