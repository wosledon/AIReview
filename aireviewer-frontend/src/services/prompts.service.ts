import { apiClient } from './api-client';
import type { PromptDto, CreatePromptRequest, UpdatePromptRequest, EffectivePromptResponse, PromptType } from '../types/prompt';

export class PromptsService {
  private base = '/prompts';

  // user prompts
  async listUserPrompts(): Promise<PromptDto[]> {
    const res = await apiClient.get<{ success: boolean; data?: PromptDto[]; message?: string }>(`${this.base}/user`);
    return res.data || [];
  }

  // built-in prompts
  async listBuiltInPrompts(): Promise<PromptDto[]> {
    const res = await apiClient.get<{ success: boolean; data?: PromptDto[]; message?: string }>(`${this.base}/built-in`);
    return res.data || [];
  }

  // project prompts
  async listProjectPrompts(projectId: number): Promise<PromptDto[]> {
    const res = await apiClient.get<{ success: boolean; data?: PromptDto[]; message?: string }>(`${this.base}/project/${projectId}`);
    return res.data || [];
  }

  // create
  async create(req: CreatePromptRequest): Promise<PromptDto> {
    const res = await apiClient.post<{ success: boolean; data?: PromptDto; message?: string }>(`${this.base}`, req);
    return res.data!;
  }

  // update
  async update(id: number, req: UpdatePromptRequest): Promise<PromptDto> {
    const res = await apiClient.put<{ success: boolean; data?: PromptDto; message?: string }>(`${this.base}/${id}`, req);
    return res.data!;
  }

  // delete
  async delete(id: number): Promise<void> {
    await apiClient.delete<{ success: boolean; message?: string }>(`${this.base}/${id}`);
  }

  // effective
  async getEffective(type: PromptType, projectId?: number): Promise<EffectivePromptResponse> {
    const qs = new URLSearchParams();
    qs.set('type', type);
    if (projectId != null) qs.set('projectId', String(projectId));
    const res = await apiClient.get<{ success: boolean; data?: EffectivePromptResponse }>(`${this.base}/effective?${qs.toString()}`);
    return res.data!;
  }
}

export const promptsService = new PromptsService();
