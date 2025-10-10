export const ReviewState = {
  Pending: 'Pending',
  AIReviewing: 'AIReviewing',
  HumanReview: 'HumanReview',
  Approved: 'Approved',
  Rejected: 'Rejected'
} as const;

export type ReviewState = typeof ReviewState[keyof typeof ReviewState];

export const ReviewCommentSeverity = {
  Info: 'Info',
  Warning: 'Warning',
  Error: 'Error',
  Critical: 'Critical'
} as const;

export type ReviewCommentSeverity = typeof ReviewCommentSeverity[keyof typeof ReviewCommentSeverity];

export const ReviewCommentCategory = {
  Quality: 'Quality',
  Security: 'Security',
  Performance: 'Performance',
  Style: 'Style',
  Documentation: 'Documentation'
} as const;

export type ReviewCommentCategory = typeof ReviewCommentCategory[keyof typeof ReviewCommentCategory];

export interface Review {
  id: number;
  projectId: number;
  projectName: string;
  authorId: string;
  authorName: string;
  title: string;
  description?: string;
  branch: string;
  baseBranch: string;
  status: ReviewState;
  pullRequestNumber?: number;
  createdAt: string;
  updatedAt: string;
}

export interface CreateReviewRequest {
  projectId: number;
  title: string;
  description?: string;
  branch: string;
  baseBranch: string;
  pullRequestNumber?: number;
}

export interface UpdateReviewRequest {
  title?: string;
  description?: string;
  status?: ReviewState;
}

export interface ReviewComment {
  id: number;
  reviewRequestId: number;
  authorId: string;
  authorName: string;
  filePath?: string;
  lineNumber?: number;
  content: string;
  severity: ReviewCommentSeverity;
  category: ReviewCommentCategory;
  isAIGenerated: boolean;
  suggestion?: string;
  createdAt: string;
}

export interface AddCommentRequest {
  content: string;
  filePath?: string;
  lineNumber?: number;
  severity: ReviewCommentSeverity;
  category: ReviewCommentCategory;
  suggestion?: string;
}

export interface UpdateCommentRequest {
  content?: string;
  severity?: ReviewCommentSeverity;
  category?: ReviewCommentCategory;
  suggestion?: string;
}

export interface AIReviewResult {
  reviewId: number;
  overallScore: number;
  summary: string;
  comments: ReviewComment[];
  actionableItems: string[];
  generatedAt: string;
}

export interface ReviewQueryParameters {
  projectId?: number;
  status?: string;
  authorId?: string;
  createdAfter?: string;
  createdBefore?: string;
  page?: number;
  pageSize?: number;
}

export interface PagedResult<T> {
  items: T[];
  totalCount: number;
  page: number;
  pageSize: number;
  totalPages: number;
}