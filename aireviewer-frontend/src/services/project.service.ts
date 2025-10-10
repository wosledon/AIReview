import { apiClient } from './api-client';
import type { 
  Project, 
  CreateProjectRequest, 
  UpdateProjectRequest, 
  ProjectMember, 
  AddMemberRequest 
} from '../types/project';
import type { PagedResult } from '../types/review';

export interface ProjectQueryParameters {
  search?: string;
  isActive?: boolean;
  page?: number;
  pageSize?: number;
}

export class ProjectService {
  async getProjects(params?: ProjectQueryParameters): Promise<PagedResult<Project>> {
    const queryParams = new URLSearchParams();
    
    if (params?.search) queryParams.append('search', params.search);
    if (params?.isActive !== undefined) queryParams.append('isActive', params.isActive.toString());
    if (params?.page) queryParams.append('page', params.page.toString());
    if (params?.pageSize) queryParams.append('pageSize', params.pageSize.toString());

    const query = queryParams.toString();
    const url = query ? `/projects?${query}` : '/projects';
    
    // 获取 API 响应
    const response = await apiClient.get<{ success: boolean; data: Project[] }>(url);
    
    // 将 API 响应转换为 PagedResult 格式
    const projects = response.data || [];
    
    // 为了兼容现有的分页逻辑，我们创建一个简单的 PagedResult
    return {
      items: projects,
      totalCount: projects.length,
      page: params?.page || 1,
      pageSize: params?.pageSize || 20,
      totalPages: Math.ceil(projects.length / (params?.pageSize || 20))
    };
  }

  async getProject(id: number): Promise<Project> {
    const response = await apiClient.get<{ success: boolean; data: Project }>(`/projects/${id}`);
    return response.data;
  }

  async createProject(request: CreateProjectRequest): Promise<Project> {
    const response = await apiClient.post<{ success: boolean; data: Project }>('/projects', request);
    return response.data;
  }

  async updateProject(id: number, request: UpdateProjectRequest): Promise<Project> {
    const response = await apiClient.put<{ success: boolean; data: Project }>(`/projects/${id}`, request);
    return response.data;
  }

  async deleteProject(id: number): Promise<void> {
    await apiClient.delete<void>(`/projects/${id}`);
  }

  async getProjectMembers(projectId: number): Promise<ProjectMember[]> {
    const response = await apiClient.get<{ success: boolean; data: ProjectMember[] }>(`/projects/${projectId}/members`);
    return response.data;
  }

  async addProjectMember(projectId: number, request: AddMemberRequest): Promise<ProjectMember> {
    const response = await apiClient.post<{ success: boolean; data: ProjectMember }>(`/projects/${projectId}/members`, request);
    return response.data;
  }

  async removeProjectMember(projectId: number, memberId: string): Promise<void> {
    await apiClient.delete<void>(`/projects/${projectId}/members/${memberId}`);
  }

  async updateProjectMemberRole(projectId: number, memberId: string, role: string): Promise<ProjectMember> {
    const response = await apiClient.put<{ success: boolean; data: ProjectMember }>(`/projects/${projectId}/members/${memberId}`, { role });
    return response.data;
  }

  async getMyProjects(): Promise<Project[]> {
    const response = await apiClient.get<{ success: boolean; data: Project[] }>('/projects/my');
    return response.data;
  }

  async archiveProject(id: number): Promise<Project> {
    const response = await apiClient.post<{ success: boolean; data: Project }>(`/projects/${id}/archive`);
    return response.data;
  }

  async unarchiveProject(id: number): Promise<Project> {
    const response = await apiClient.post<{ success: boolean; data: Project }>(`/projects/${id}/unarchive`);
    return response.data;
  }
}

export const projectService = new ProjectService();