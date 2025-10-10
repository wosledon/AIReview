import { apiClient } from './api-client';

// LLM配置相关的类型定义
export interface LLMConfiguration {
  id: number;
  name: string;
  provider: string;
  apiEndpoint: string;
  apiKey: string;
  model: string;
  maxTokens: number;
  temperature: number;
  isActive: boolean;
  isDefault: boolean;
  createdAt: string;
  updatedAt: string;
  extraParameters?: string;
}

export interface CreateLLMConfigurationDto {
  name: string;
  provider: string;
  apiEndpoint: string;
  apiKey: string;
  model: string;
  maxTokens: number;
  temperature: number;
  isActive: boolean;
  isDefault: boolean;
  extraParameters?: string;
}

export interface UpdateLLMConfigurationDto {
  name: string;
  provider: string;
  apiEndpoint: string;
  apiKey: string;
  model: string;
  maxTokens: number;
  temperature: number;
  isActive: boolean;
  isDefault: boolean;
  extraParameters?: string;
}

export interface TestConnectionResult {
  isConnected: boolean;
  message: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message: string;
}

export class LLMConfigurationService {
  // 获取所有配置
  async getAll(): Promise<LLMConfiguration[]> {
    const response = await apiClient.get<ApiResponse<LLMConfiguration[]>>('/llmconfiguration');
    return response.data || [];
  }

  // 根据ID获取配置
  async getById(id: number): Promise<LLMConfiguration> {
    const response = await apiClient.get<ApiResponse<LLMConfiguration>>(`/llmconfiguration/${id}`);
    return response.data!;
  }

  // 获取默认配置
  async getDefault(): Promise<LLMConfiguration> {
    const response = await apiClient.get<ApiResponse<LLMConfiguration>>('/llmconfiguration/default');
    return response.data!;
  }

  // 创建配置
  async create(data: CreateLLMConfigurationDto): Promise<LLMConfiguration> {
    const response = await apiClient.post<ApiResponse<LLMConfiguration>>('/llmconfiguration', data);
    return response.data!;
  }

  // 更新配置
  async update(id: number, data: UpdateLLMConfigurationDto): Promise<LLMConfiguration> {
    const response = await apiClient.put<ApiResponse<LLMConfiguration>>(`/llmconfiguration/${id}`, data);
    return response.data!;
  }

  // 删除配置
  async delete(id: number): Promise<void> {
    await apiClient.delete<ApiResponse<null>>(`/llmconfiguration/${id}`);
  }

  // 设置为默认配置
  async setDefault(id: number): Promise<void> {
    await apiClient.post<ApiResponse<null>>(`/llmconfiguration/${id}/set-default`);
  }

  // 测试连接
  async testConnection(id: number): Promise<TestConnectionResult> {
    const response = await apiClient.post<ApiResponse<TestConnectionResult>>(`/llmconfiguration/${id}/test`);
    return response.data!;
  }

  // 获取支持的提供商列表
  async getSupportedProviders(): Promise<string[]> {
    const response = await apiClient.get<ApiResponse<string[]>>('/llmconfiguration/providers');
    return response.data || [];
  }
}

// 导出单例实例
export const llmConfigurationService = new LLMConfigurationService();