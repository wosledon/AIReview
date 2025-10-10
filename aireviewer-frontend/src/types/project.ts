export interface Project {
  id: number;
  name: string;
  description?: string;
  repositoryUrl?: string;
  language: string;
  isActive?: boolean; // 改为可选，因为后端可能不返回这个字段
  memberCount?: number; // 改为可选，因为后端可能不返回这个字段
  createdAt: string;
  updatedAt: string;
}

export interface CreateProjectRequest {
  name: string;
  description?: string;
  repositoryUrl?: string;
  language: string;
}

export interface UpdateProjectRequest {
  name?: string;
  description?: string;
  repositoryUrl?: string;
  language?: string;
}

export interface ProjectMember {
  id: number;
  projectId: number;
  userId: string;
  userName: string;
  userEmail: string;
  role: string;
  joinedAt: string;
}

export interface AddMemberRequest {
  email: string;
  role: string;
}