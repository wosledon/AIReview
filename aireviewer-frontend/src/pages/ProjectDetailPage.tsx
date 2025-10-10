import { useState } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  ArrowLeftIcon,
  PencilIcon,
  TrashIcon,
  UserPlusIcon,
  CogIcon,
  EyeIcon,
  PlayIcon,
  ClockIcon,
  XCircleIcon
} from '@heroicons/react/24/outline';
import { projectService } from '../services/project.service';
import type { Project, ProjectMember } from '../types/project';

export const ProjectDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const [activeTab, setActiveTab] = useState<'overview' | 'members' | 'reviews' | 'settings'>('overview');

  const projectId = parseInt(id!, 10);

  const {
    data: project,
    isLoading: isProjectLoading,
    error: projectError
  } = useQuery({
    queryKey: ['project', projectId],
    queryFn: () => projectService.getProject(projectId),
    enabled: !!projectId,
  });

  const {
    data: members,
    isLoading: isMembersLoading
  } = useQuery({
    queryKey: ['project-members', projectId],
    queryFn: () => projectService.getProjectMembers(projectId),
    enabled: !!projectId,
  });

  const deleteProjectMutation = useMutation({
    mutationFn: () => projectService.deleteProject(projectId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['projects'] });
      navigate('/projects');
    }
  });

  const archiveProjectMutation = useMutation({
    mutationFn: () => project?.isActive 
      ? projectService.archiveProject(projectId)
      : projectService.unarchiveProject(projectId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['project', projectId] });
    }
  });

  if (isProjectLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (projectError || !project) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">
          <p>加载项目详情时出错</p>
        </div>
        <button 
          onClick={() => navigate('/projects')}
          className="btn btn-primary"
        >
          返回项目列表
        </button>
      </div>
    );
  }

  const tabs = [
    { id: 'overview', name: '概览', icon: EyeIcon },
    { id: 'members', name: '成员', icon: UserPlusIcon },
    { id: 'reviews', name: '评审记录', icon: ClockIcon },
    { id: 'settings', name: '设置', icon: CogIcon },
  ] as const;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <button
            onClick={() => navigate('/projects')}
            className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100"
          >
            <ArrowLeftIcon className="h-5 w-5" />
          </button>
          <div>
            <div className="flex items-center space-x-3">
              <h1 className="text-2xl font-bold text-gray-900">{project.name}</h1>
              <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                project.isActive 
                  ? 'bg-green-100 text-green-800' 
                  : 'bg-gray-100 text-gray-800'
              }`}>
                {project.isActive ? '活跃' : '已归档'}
              </span>
            </div>
            {project.description && (
              <p className="text-gray-500 mt-1">{project.description}</p>
            )}
          </div>
        </div>

        <div className="flex items-center space-x-3">
          <Link
            to={`/projects/${projectId}/reviews/new`}
            className="btn btn-primary"
          >
            <PlayIcon className="h-5 w-5 mr-2" />
            开始评审
          </Link>
          <button
            onClick={() => navigate(`/projects/${projectId}/edit`)}
            className="btn btn-secondary"
          >
            <PencilIcon className="h-5 w-5" />
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200">
        <nav className="-mb-px flex space-x-8">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`${
                  activeTab === tab.id
                    ? 'border-primary-500 text-primary-600'
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                } whitespace-nowrap py-2 px-1 border-b-2 font-medium text-sm flex items-center space-x-2`}
              >
                <Icon className="h-5 w-5" />
                <span>{tab.name}</span>
              </button>
            );
          })}
        </nav>
      </div>

      {/* Tab Content */}
      <div className="mt-6">
        {activeTab === 'overview' && <OverviewTab project={project} />}
        {activeTab === 'members' && <MembersTab members={members || []} isLoading={isMembersLoading} />}
        {activeTab === 'reviews' && <ReviewsTab projectId={projectId} />}
        {activeTab === 'settings' && (
          <SettingsTab 
            project={project} 
            onArchive={() => archiveProjectMutation.mutate()}
            onDelete={() => deleteProjectMutation.mutate()}
            isArchiving={archiveProjectMutation.isPending}
            isDeleting={deleteProjectMutation.isPending}
          />
        )}
      </div>
    </div>
  );
};

interface OverviewTabProps {
  project: Project;
}

const OverviewTab = ({ project }: OverviewTabProps) => {
  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      {/* Project Info */}
      <div className="lg:col-span-2 space-y-6">
        <div className="card">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">项目信息</h3>
          <dl className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <dt className="text-sm font-medium text-gray-500">项目名称</dt>
              <dd className="mt-1 text-sm text-gray-900">{project.name}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">编程语言</dt>
              <dd className="mt-1 text-sm text-gray-900">{project.language}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">仓库地址</dt>
              <dd className="mt-1 text-sm text-gray-900">
                {project.repositoryUrl ? (
                  <a 
                    href={project.repositoryUrl} 
                    target="_blank" 
                    rel="noopener noreferrer"
                    className="text-primary-600 hover:text-primary-700"
                  >
                    {project.repositoryUrl}
                  </a>
                ) : (
                  <span className="text-gray-400">未设置</span>
                )}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">创建时间</dt>
              <dd className="mt-1 text-sm text-gray-900">
                {new Date(project.createdAt).toLocaleDateString('zh-CN')}
              </dd>
            </div>
          </dl>
        </div>

        {/* Recent Reviews */}
        <div className="card">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-gray-900">最近评审</h3>
            <Link 
              to={`/projects/${project.id}/reviews`}
              className="text-primary-600 hover:text-primary-700 text-sm font-medium"
            >
              查看全部
            </Link>
          </div>
          <div className="text-center py-8">
            <ClockIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <p className="text-gray-500">暂无评审记录</p>
            <Link 
              to={`/projects/${project.id}/reviews/new`}
              className="btn btn-primary mt-4"
            >
              开始第一次评审
            </Link>
          </div>
        </div>
      </div>

      {/* Stats & Quick Actions */}
      <div className="space-y-6">
        {/* Stats */}
        <div className="card">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">统计数据</h3>
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500">总评审次数</span>
              <span className="text-sm font-semibold text-gray-900">0</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500">通过评审</span>
              <span className="text-sm font-semibold text-green-600">0</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500">需要修改</span>
              <span className="text-sm font-semibold text-orange-600">0</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500">项目成员</span>
              <span className="text-sm font-semibold text-gray-900">{project.memberCount || 0}</span>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="card">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">快速操作</h3>
          <div className="space-y-3">
            <Link
              to={`/projects/${project.id}/reviews/new`}
              className="btn btn-primary w-full"
            >
              <PlayIcon className="h-5 w-5 mr-2" />
              开始新评审
            </Link>
            <Link
              to={`/projects/${project.id}/members`}
              className="btn btn-secondary w-full"
            >
              <UserPlusIcon className="h-5 w-5 mr-2" />
              管理成员
            </Link>
            <Link
              to={`/projects/${project.id}/settings`}
              className="btn btn-secondary w-full"
            >
              <CogIcon className="h-5 w-5 mr-2" />
              项目设置
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};

interface MembersTabProps {
  projectId: number;
  members: ProjectMember[];
  isLoading: boolean;
}

const MembersTab = ({ members, isLoading }: Omit<MembersTabProps, 'projectId'>) => {
  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-32">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-gray-900">项目成员</h3>
        <button className="btn btn-primary">
          <UserPlusIcon className="h-5 w-5 mr-2" />
          邀请成员
        </button>
      </div>

      {members.length === 0 ? (
        <div className="card text-center py-8">
          <UserPlusIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <p className="text-gray-500">还没有项目成员</p>
          <button className="btn btn-primary mt-4">
            邀请第一位成员
          </button>
        </div>
      ) : (
        <div className="card">
          <div className="overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    成员
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    角色
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    加入时间
                  </th>
                  <th className="relative px-6 py-3">
                    <span className="sr-only">操作</span>
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {members.map((member) => (
                  <tr key={member.id}>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center">
                        <div className="h-10 w-10 rounded-full bg-primary-100 flex items-center justify-center">
                          <span className="text-primary-600 font-medium text-sm">
                            {member.userName.charAt(0).toUpperCase()}
                          </span>
                        </div>
                        <div className="ml-4">
                          <div className="text-sm font-medium text-gray-900">
                            {member.userName}
                          </div>
                          <div className="text-sm text-gray-500">
                            {member.userEmail}
                          </div>
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900">
                      {member.role}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(member.joinedAt).toLocaleDateString('zh-CN')}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <button className="text-red-600 hover:text-red-700">
                        移除
                      </button>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}
    </div>
  );
};

interface ReviewsTabProps {
  projectId: number;
}

const ReviewsTab = ({ projectId }: ReviewsTabProps) => {
  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-gray-900">评审记录</h3>
        <Link 
          to={`/projects/${projectId}/reviews/new`}
          className="btn btn-primary"
        >
          <PlayIcon className="h-5 w-5 mr-2" />
          开始新评审
        </Link>
      </div>

      <div className="card text-center py-8">
        <ClockIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <p className="text-gray-500">还没有评审记录</p>
        <Link 
          to={`/projects/${projectId}/reviews/new`}
          className="btn btn-primary mt-4"
        >
          开始第一次评审
        </Link>
      </div>
    </div>
  );
};

interface SettingsTabProps {
  project: Project;
  onArchive: () => void;
  onDelete: () => void;
  isArchiving: boolean;
  isDeleting: boolean;
}

const SettingsTab = ({ project, onArchive, onDelete, isArchiving, isDeleting }: SettingsTabProps) => {
  const [showDeleteConfirm, setShowDeleteConfirm] = useState(false);

  return (
    <div className="space-y-6">
      <div className="card">
        <h3 className="text-lg font-semibold text-gray-900 mb-4">项目设置</h3>
        
        <div className="space-y-6">
          {/* Archive/Unarchive */}
          <div className="flex items-center justify-between py-4 border-b border-gray-200">
            <div>
              <h4 className="text-sm font-medium text-gray-900">
                {project.isActive ? '归档项目' : '取消归档'}
              </h4>
              <p className="text-sm text-gray-500">
                {project.isActive 
                  ? '归档后项目将不再显示在活跃列表中，但数据仍会保留'
                  : '取消归档后项目将重新显示在活跃列表中'
                }
              </p>
            </div>
            <button
              onClick={onArchive}
              disabled={isArchiving}
              className="btn btn-secondary"
            >
              {isArchiving ? '处理中...' : (project.isActive ? '归档' : '取消归档')}
            </button>
          </div>

          {/* Delete Project */}
          <div className="flex items-center justify-between py-4">
            <div>
              <h4 className="text-sm font-medium text-red-900">删除项目</h4>
              <p className="text-sm text-red-600">
                删除项目将永久移除所有相关数据，此操作不可撤销
              </p>
            </div>
            <button
              onClick={() => setShowDeleteConfirm(true)}
              disabled={isDeleting}
              className="btn btn-danger"
            >
              <TrashIcon className="h-5 w-5 mr-2" />
              删除项目
            </button>
          </div>
        </div>
      </div>

      {/* Delete Confirmation Modal */}
      {showDeleteConfirm && (
        <div className="fixed inset-0 bg-gray-500 bg-opacity-75 flex items-center justify-center z-50">
          <div className="bg-white rounded-lg p-6 max-w-md w-full mx-4">
            <div className="flex items-center mb-4">
              <XCircleIcon className="h-6 w-6 text-red-600 mr-3" />
              <h3 className="text-lg font-semibold text-gray-900">确认删除项目</h3>
            </div>
            <p className="text-gray-600 mb-6">
              您确定要删除项目 "{project.name}" 吗？此操作将永久删除所有相关数据，包括评审记录、成员信息等，且无法恢复。
            </p>
            <div className="flex items-center justify-end space-x-3">
              <button
                onClick={() => setShowDeleteConfirm(false)}
                className="btn btn-secondary"
                disabled={isDeleting}
              >
                取消
              </button>
              <button
                onClick={() => {
                  onDelete();
                  setShowDeleteConfirm(false);
                }}
                disabled={isDeleting}
                className="btn btn-danger"
              >
                {isDeleting ? '删除中...' : '确认删除'}
              </button>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};