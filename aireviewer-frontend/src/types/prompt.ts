// Prompt DTOs and enums matching backend
export type PromptType = 'Review' | 'RiskAnalysis' | 'PullRequestSummary' | 'ImprovementSuggestions';

export interface PromptDto {
  id: number;
  type: PromptType;
  name: string;
  content: string;
  userId?: string | null;
  projectId?: number | null;
  createdAt: string;
  updatedAt: string;
}

export interface CreatePromptRequest {
  type: PromptType;
  name: string;
  content: string;
  userId?: string | null;
  projectId?: number | null;
}

export interface UpdatePromptRequest {
  name?: string;
  content?: string;
}

export interface EffectivePromptResponse {
  type: PromptType;
  content: string;
  source: 'project' | 'user' | 'built-in';
}
