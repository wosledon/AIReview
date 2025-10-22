import { apiClient } from './api-client';

// Git凭证相关类型定义
export interface GitCredential {
  id: number;
  name: string;
  type: string; // SSH/Token/UsernamePassword
  provider?: string;
  username?: string;
  publicKey?: string;
  isDefault: boolean;
  isActive: boolean;
  isVerified: boolean;
  lastVerifiedAt?: string;
  createdAt: string;
  updatedAt: string;
}

export interface CreateGitCredentialRequest {
  name: string;
  type: string;
  provider?: string;
  username?: string;
  secret?: string;
  privateKey?: string;
  isDefault: boolean;
}

export interface UpdateGitCredentialRequest {
  name?: string;
  username?: string;
  secret?: string;
  privateKey?: string;
  isDefault?: boolean;
  isActive?: boolean;
}

export interface SshKeyPair {
  privateKey: string;
  publicKey: string;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message: string;
}

export class GitCredentialService {
  // 获取用户的所有凭证
  async getUserCredentials(): Promise<ApiResponse<GitCredential[]>> {
    return await apiClient.get<ApiResponse<GitCredential[]>>('/gitcredentials');
  }

  // 获取单个凭证
  async getCredential(id: number): Promise<ApiResponse<GitCredential>> {
    return await apiClient.get<ApiResponse<GitCredential>>(`/gitcredentials/${id}`);
  }

  // 创建凭证
  async createCredential(data: CreateGitCredentialRequest): Promise<ApiResponse<GitCredential>> {
    return await apiClient.post<ApiResponse<GitCredential>>('/gitcredentials', data);
  }

  // 更新凭证
  async updateCredential(id: number, data: UpdateGitCredentialRequest): Promise<ApiResponse<GitCredential>> {
    return await apiClient.put<ApiResponse<GitCredential>>(`/gitcredentials/${id}`, data);
  }

  // 删除凭证
  async deleteCredential(id: number): Promise<ApiResponse<null>> {
    return await apiClient.delete<ApiResponse<null>>(`/gitcredentials/${id}`);
  }

  // 生成SSH密钥对
  async generateSshKeyPair(): Promise<ApiResponse<SshKeyPair>> {
    return await apiClient.post<ApiResponse<SshKeyPair>>('/gitcredentials/generate-ssh-key');
  }

  // 验证凭证
  async verifyCredential(id: number, repositoryUrl: string): Promise<ApiResponse<null>> {
    return await apiClient.post<ApiResponse<null>>(`/gitcredentials/${id}/verify?repositoryUrl=${encodeURIComponent(repositoryUrl)}`);
  }

  // 设置默认凭证
  async setDefaultCredential(id: number): Promise<ApiResponse<null>> {
    return await apiClient.post<ApiResponse<null>>(`/gitcredentials/${id}/set-default`);
  }
}

// 导出单例实例
export const gitCredentialService = new GitCredentialService();