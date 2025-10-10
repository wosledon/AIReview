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
    
    return await apiClient.get<PagedResult<Review>>(url);
  }

  async getReview(id: number): Promise<Review> {
    return await apiClient.get<Review>(`/reviews/${id}`);
  }

  async createReview(request: CreateReviewRequest): Promise<Review> {
    return await apiClient.post<Review>('/reviews', request);
  }

  async updateReview(id: number, request: UpdateReviewRequest): Promise<Review> {
    return await apiClient.put<Review>(`/reviews/${id}`, request);
  }

  async deleteReview(id: number): Promise<void> {
    await apiClient.delete<void>(`/reviews/${id}`);
  }

  async getReviewComments(reviewId: number): Promise<ReviewComment[]> {
    return await apiClient.get<ReviewComment[]>(`/reviews/${reviewId}/comments`);
  }

  async addComment(reviewId: number, request: AddCommentRequest): Promise<ReviewComment> {
    return await apiClient.post<ReviewComment>(`/reviews/${reviewId}/comments`, request);
  }

  async updateReviewComment(reviewId: number, commentId: number, request: UpdateCommentRequest): Promise<ReviewComment> {
    return await apiClient.put<ReviewComment>(`/reviews/${reviewId}/comments/${commentId}`, request);
  }

  async deleteReviewComment(reviewId: number, commentId: number): Promise<void> {
    await apiClient.delete<void>(`/reviews/${reviewId}/comments/${commentId}`);
  }

  async startAIReview(reviewId: number): Promise<void> {
    await apiClient.post<void>(`/reviews/${reviewId}/ai-review`);
  }

  async getAIReviewResult(reviewId: number): Promise<AIReviewResult> {
    return await apiClient.get<AIReviewResult>(`/reviews/${reviewId}/ai-result`);
  }

  async approveReview(reviewId: number): Promise<Review> {
    return await apiClient.post<Review>(`/reviews/${reviewId}/approve`);
  }

  async rejectReview(reviewId: number, reason?: string): Promise<Review> {
    return await apiClient.post<Review>(`/reviews/${reviewId}/reject`, { reason });
  }

  async requestChanges(reviewId: number, reason: string): Promise<Review> {
    return await apiClient.post<Review>(`/reviews/${reviewId}/request-changes`, { reason });
  }

  async getMyReviews(): Promise<Review[]> {
    return await apiClient.get<Review[]>('/reviews/my');
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
    
    return await apiClient.get<PagedResult<Review>>(url);
  }
}

export const reviewService = new ReviewService();