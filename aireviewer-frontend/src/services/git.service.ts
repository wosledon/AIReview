import { apiClient } from './api-client';

// Git仓库相关类型定义
export interface GitRepository {
  id: number;
  name: string;
  url: string;
  localPath?: string;
  defaultBranch?: string;
  username?: string;
  isActive: boolean;
  createdAt: string;
  updatedAt: string;
  lastSyncAt?: string;
  projectId?: number;
  projectName?: string;
  branchCount: number;
}

export interface CreateGitRepository {
  name: string;
  url: string;
  defaultBranch?: string;
  username?: string;
  accessToken?: string;
  projectId?: number;
}

export interface UpdateGitRepository {
  name: string;
  url: string;
  defaultBranch?: string;
  username?: string;
  accessToken?: string;
  isActive: boolean;
}

export interface GitBranch {
  id: number;
  name: string;
  commitSha: string;
  isDefault: boolean;
  createdAt: string;
  updatedAt: string;
}

export interface GitCommit {
  id: number;
  sha: string;
  message: string;
  authorName: string;
  authorEmail: string;
  authorDate: string;
  committerName?: string;
  committerEmail?: string;
  committerDate?: string;
  branchName?: string;
  fileChangesCount: number;
}

export interface GitCommitDetail extends GitCommit {
  fileChanges: GitFileChange[];
}

export interface GitFileChange {
  id: number;
  fileName: string;
  changeType: string;
  addedLines: number;
  deletedLines: number;
}

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message: string;
}

export class GitService {
  // 仓库管理
  async getRepositories(projectId?: number): Promise<ApiResponse<GitRepository[]>> {
    const params = projectId ? `?projectId=${projectId}` : '';
    return await apiClient.get<ApiResponse<GitRepository[]>>(`/git/repositories${params}`);
  }

  async getRepository(id: number): Promise<ApiResponse<GitRepository>> {
    return await apiClient.get<ApiResponse<GitRepository>>(`/git/repositories/${id}`);
  }

  async createRepository(data: CreateGitRepository): Promise<ApiResponse<GitRepository>> {
    return await apiClient.post<ApiResponse<GitRepository>>('/git/repositories', data);
  }

  async updateRepository(id: number, data: UpdateGitRepository): Promise<ApiResponse<GitRepository>> {
    return await apiClient.put<ApiResponse<GitRepository>>(`/git/repositories/${id}`, data);
  }

  async deleteRepository(id: number): Promise<ApiResponse<null>> {
    return await apiClient.delete<ApiResponse<null>>(`/git/repositories/${id}`);
  }

  // Git操作
  async cloneRepository(id: number): Promise<ApiResponse<null>> {
    return await apiClient.post<ApiResponse<null>>(`/git/repositories/${id}/clone`);
  }

  async pullRepository(id: number, branch?: string): Promise<ApiResponse<null>> {
    const params = branch ? `?branch=${encodeURIComponent(branch)}` : '';
    return await apiClient.post<ApiResponse<null>>(`/git/repositories/${id}/pull${params}`);
  }

  async syncRepository(id: number): Promise<ApiResponse<null>> {
    return await apiClient.post<ApiResponse<null>>(`/git/repositories/${id}/sync`);
  }

  async testRepository(id: number): Promise<ApiResponse<null>> {
    return await apiClient.post<ApiResponse<null>>(`/git/repositories/${id}/test`);
  }

  // 分支管理
  async getBranches(repositoryId: number): Promise<ApiResponse<GitBranch[]>> {
    return await apiClient.get<ApiResponse<GitBranch[]>>(`/git/repositories/${repositoryId}/branches`);
  }

  // 提交管理
  async getCommits(
    repositoryId: number, 
    branch?: string, 
    skip: number = 0, 
    take: number = 50
  ): Promise<ApiResponse<GitCommit[]>> {
    const params = new URLSearchParams({
      skip: skip.toString(),
      take: take.toString()
    });
    
    if (branch) {
      params.append('branch', branch);
    }
    
    return await apiClient.get<ApiResponse<GitCommit[]>>(`/git/repositories/${repositoryId}/commits?${params}`);
  }

  async getCommit(repositoryId: number, sha: string): Promise<ApiResponse<GitCommitDetail>> {
    return await apiClient.get<ApiResponse<GitCommitDetail>>(`/git/repositories/${repositoryId}/commits/${sha}`);
  }
}

// 导出单例实例
export const gitService = new GitService();