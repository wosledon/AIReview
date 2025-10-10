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