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
    
    return await apiClient.get<PagedResult<Project>>(url);
  }

  async getProject(id: number): Promise<Project> {
    return await apiClient.get<Project>(`/projects/${id}`);
  }

  async createProject(request: CreateProjectRequest): Promise<Project> {
    return await apiClient.post<Project>('/projects', request);
  }

  async updateProject(id: number, request: UpdateProjectRequest): Promise<Project> {
    return await apiClient.put<Project>(`/projects/${id}`, request);
  }

  async deleteProject(id: number): Promise<void> {
    await apiClient.delete<void>(`/projects/${id}`);
  }

  async getProjectMembers(projectId: number): Promise<ProjectMember[]> {
    return await apiClient.get<ProjectMember[]>(`/projects/${projectId}/members`);
  }

  async addProjectMember(projectId: number, request: AddMemberRequest): Promise<ProjectMember> {
    return await apiClient.post<ProjectMember>(`/projects/${projectId}/members`, request);
  }

  async removeProjectMember(projectId: number, memberId: string): Promise<void> {
    await apiClient.delete<void>(`/projects/${projectId}/members/${memberId}`);
  }

  async updateProjectMemberRole(projectId: number, memberId: string, role: string): Promise<ProjectMember> {
    return await apiClient.put<ProjectMember>(`/projects/${projectId}/members/${memberId}`, { role });
  }

  async getMyProjects(): Promise<Project[]> {
    return await apiClient.get<Project[]>('/projects/my');
  }

  async archiveProject(id: number): Promise<Project> {
    return await apiClient.post<Project>(`/projects/${id}/archive`);
  }

  async unarchiveProject(id: number): Promise<Project> {
    return await apiClient.post<Project>(`/projects/${id}/unarchive`);
  }
}

export const projectService = new ProjectService();