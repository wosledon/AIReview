export interface Project {
  id: number;
  name: string;
  description?: string;
  repositoryUrl?: string;
  language: string;
  isActive: boolean;
  memberCount?: number;
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