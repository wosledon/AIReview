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
  XCircleIcon,
  CheckCircleIcon,
  ExclamationTriangleIcon,
  CpuChipIcon
} from '@heroicons/react/24/outline';
import { projectService } from '../services/project.service';
import { reviewService } from '../services/review.service';
import { ReviewState } from '../types/review';
import type { Project, ProjectMember } from '../types/project';
import type { Review } from '../types/review';

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
            className="p-2 text-gray-400 hover:text-gray-600 dark:text-gray-300 dark:hover:text-gray-100 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800"
          >
            <ArrowLeftIcon className="h-5 w-5" />
          </button>
          <div>
            <div className="flex items-center space-x-3">
              <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">{project.name}</h1>
              <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
                project.isActive 
                  ? 'bg-green-100 text-green-800' 
                  : 'bg-gray-100 text-gray-800'
              }`}>
                {project.isActive ? '活跃' : '已归档'}
              </span>
            </div>
            {project.description && (
              <p className="text-gray-500 dark:text-gray-400 mt-1">{project.description}</p>
            )}
          </div>
        </div>

        <div className="flex items-center space-x-3">
          <Link
            to={`/projects/${projectId}/reviews/new`}
            className="btn btn-primary inline-flex items-center space-x-1"
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
      <div className="border-b border-gray-200 dark:border-gray-800">
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
                    : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:border-gray-300 dark:hover:border-gray-700'
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
        {activeTab === 'overview' && (
          <OverviewTab 
            project={project} 
            onSwitchToReviews={() => setActiveTab('reviews')}
            onSwitchToMembers={() => setActiveTab('members')}
            onSwitchToSettings={() => setActiveTab('settings')}
          />
        )}
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
  onSwitchToReviews: () => void;
  onSwitchToMembers: () => void;
  onSwitchToSettings: () => void;
}

const OverviewTab = ({ project, onSwitchToReviews, onSwitchToMembers, onSwitchToSettings }: OverviewTabProps) => {
  // 获取最近的评审记录
  const {
    data: recentReviewsData,
    isLoading: isRecentReviewsLoading
  } = useQuery({
    queryKey: ['project-recent-reviews', project.id],
    queryFn: () => reviewService.getReviewsForProject(project.id, { page: 1, pageSize: 5 }),
    enabled: !!project.id,
  });

  const getStatusIcon = (status: string) => {
    switch (status) {
      case ReviewState.Approved:
        return <CheckCircleIcon className="h-4 w-4 text-green-500" />;
      case ReviewState.Rejected:
        return <ExclamationTriangleIcon className="h-4 w-4 text-red-500" />;
      case ReviewState.AIReviewing:
        return <CpuChipIcon className="h-4 w-4 text-blue-500" />;
      case ReviewState.HumanReview:
        return <EyeIcon className="h-4 w-4 text-orange-500" />;
      default:
        return <ClockIcon className="h-4 w-4 text-gray-500" />;
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case ReviewState.Pending:
        return '待处理';
      case ReviewState.AIReviewing:
        return 'AI评审中';
      case ReviewState.HumanReview:
        return '人工评审';
      case ReviewState.Approved:
        return '已通过';
      case ReviewState.Rejected:
        return '需修改';
      default:
        return status;
    }
  };

  const recentReviews = recentReviewsData?.items || [];
  const reviewStats = recentReviewsData ? {
    total: recentReviewsData.totalCount,
    approved: 0, // 这些统计数据需要从API获取，或者基于当前数据计算
    rejected: 0,
    pending: 0
  } : { total: 0, approved: 0, rejected: 0, pending: 0 };

  // 计算简单的统计数据
  if (recentReviews.length > 0) {
    reviewStats.approved = recentReviews.filter(r => r.status === ReviewState.Approved).length;
    reviewStats.rejected = recentReviews.filter(r => r.status === ReviewState.Rejected).length;
    reviewStats.pending = recentReviews.filter(r => 
      r.status === ReviewState.Pending || 
      r.status === ReviewState.AIReviewing || 
      r.status === ReviewState.HumanReview
    ).length;
  }

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      {/* Project Info */}
      <div className="lg:col-span-2 space-y-6">
        <div className="card dark:bg-gray-900 dark:border-gray-800">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">项目信息</h3>
          <dl className="grid grid-cols-1 sm:grid-cols-2 gap-4">
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">项目名称</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{project.name}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">编程语言</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{project.language}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">仓库地址</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
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
                  <span className="text-gray-400 dark:text-gray-500">未设置</span>
                )}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">创建时间</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
                {new Date(project.createdAt).toLocaleDateString()}
              </dd>
            </div>
          </dl>
        </div>

        {/* Recent Reviews */}
        <div className="card dark:bg-gray-900 dark:border-gray-800">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">最近评审</h3>
              <button
                onClick={onSwitchToReviews}
              className="text-primary-600 hover:text-primary-700 text-sm font-medium"
            >
              查看全部
              </button>
          </div>

          {isRecentReviewsLoading ? (
            <div className="flex items-center justify-center py-8">
              <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-primary-600"></div>
            </div>
          ) : recentReviews.length === 0 ? (
            <div className="text-center py-8">
              <ClockIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <p className="text-gray-500 dark:text-gray-400">暂无评审记录</p>
              <Link 
                to={`/projects/${project.id}/reviews/new`}
                className="btn btn-primary mt-4"
              >
                开始第一次评审
              </Link>
            </div>
          ) : (
            <div className="space-y-3">
              {recentReviews.map((review: Review) => (
                <div key={review.id} className="flex items-center justify-between p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg row-hover-transition">
                  <div className="flex items-center space-x-3">
                    {getStatusIcon(review.status)}
                    <div>
                      <Link 
                        to={`/reviews/${review.id}`}
                        className="text-sm font-medium text-gray-900 dark:text-gray-100 hover:text-primary-600"
                      >
                        {review.title}
                      </Link>
                      <div className="flex items-center space-x-2 mt-1">
                        <span className="text-xs text-gray-500 dark:text-gray-400">{review.branch}</span>
                        <span className="text-xs text-gray-400 dark:text-gray-500">•</span>
                        <span className="text-xs text-gray-500 dark:text-gray-400">{review.authorName}</span>
                        <span className="text-xs text-gray-400 dark:text-gray-500">•</span>
                        <span className="text-xs text-gray-500 dark:text-gray-400">
                          {new Date(review.createdAt).toLocaleDateString()}
                        </span>
                      </div>
                    </div>
                  </div>
                  <div className="flex items-center space-x-2">
                    <span className="text-xs text-gray-500 dark:text-gray-400">{getStatusText(review.status)}</span>
                    <Link
                      to={`/reviews/${review.id}`}
                      className="text-primary-600 hover:text-primary-700"
                    >
                      <EyeIcon className="h-4 w-4" />
                    </Link>
                  </div>
                </div>
              ))}
            </div>
          )}
        </div>
      </div>

      {/* Stats & Quick Actions */}
      <div className="space-y-6">
        {/* Stats */}
        <div className="card dark:bg-gray-900 dark:border-gray-800">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">统计数据</h3>
          <div className="space-y-4">
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500 dark:text-gray-400">总评审次数</span>
              <span className="text-sm font-semibold text-gray-900 dark:text-gray-100">{reviewStats.total}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500 dark:text-gray-400">通过评审</span>
              <span className="text-sm font-semibold text-green-600">{reviewStats.approved}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500 dark:text-gray-400">需要修改</span>
              <span className="text-sm font-semibold text-orange-600">{reviewStats.rejected}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500 dark:text-gray-400">进行中</span>
              <span className="text-sm font-semibold text-blue-600">{reviewStats.pending}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500 dark:text-gray-400">项目成员</span>
              <span className="text-sm font-semibold text-gray-900 dark:text-gray-100">{project.memberCount || 0}</span>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="card">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">快速操作</h3>
          <div className="space-y-3">
            <Link
              to={`/projects/${project.id}/reviews/new`}
              className="btn btn-primary w-full inline-flex items-center space-x-1"
            >
              <PlayIcon className="h-5 w-5 mr-2" />
              开始新评审
            </Link>
            <button
              onClick={onSwitchToMembers}
              className="btn btn-secondary w-full inline-flex items-center space-x-1"
            >
              <UserPlusIcon className="h-5 w-5 mr-2" />
              管理成员
            </button>
            <button
              onClick={onSwitchToSettings}
              className="btn btn-secondary w-full inline-flex items-center space-x-1"
            >
              <CogIcon className="h-5 w-5 mr-2" />
              项目设置
            </button>
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
        <button className="btn btn-primary inline-flex items-center space-x-1">
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
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    角色
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
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
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-900 text-center">
                      {member.role}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 text-center">
                      {new Date(member.joinedAt).toLocaleDateString()}
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
  const {
    data: reviewsData,
    isLoading: isReviewsLoading,
    error: reviewsError
  } = useQuery({
    queryKey: ['project-reviews', projectId],
    queryFn: () => reviewService.getReviewsForProject(projectId, { page: 1, pageSize: 20 }),
    enabled: !!projectId,
  });

  const getStatusIcon = (status: string) => {
    switch (status) {
      case ReviewState.Approved:
        return <CheckCircleIcon className="h-5 w-5 text-green-500" />;
      case ReviewState.Rejected:
        return <ExclamationTriangleIcon className="h-5 w-5 text-red-500" />;
      case ReviewState.AIReviewing:
        return <CpuChipIcon className="h-5 w-5 text-blue-500" />;
      case ReviewState.HumanReview:
        return <EyeIcon className="h-5 w-5 text-orange-500" />;
      default:
        return <ClockIcon className="h-5 w-5 text-gray-500" />;
    }
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case ReviewState.Pending:
        return '待处理';
      case ReviewState.AIReviewing:
        return 'AI评审中';
      case ReviewState.HumanReview:
        return '人工评审';
      case ReviewState.Approved:
        return '已通过';
      case ReviewState.Rejected:
        return '需修改';
      default:
        return status;
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case ReviewState.Approved:
        return 'bg-green-100 text-green-800';
      case ReviewState.Rejected:
        return 'bg-red-100 text-red-800';
      case ReviewState.AIReviewing:
        return 'bg-blue-100 text-blue-800';
      case ReviewState.HumanReview:
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  if (isReviewsLoading) {
    return (
      <div className="flex items-center justify-center h-32">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (reviewsError) {
    return (
      <div className="card text-center py-8">
        <ExclamationTriangleIcon className="h-12 w-12 text-red-400 mx-auto mb-4" />
        <p className="text-red-500">加载评审记录时出错</p>
      </div>
    );
  }

  const reviews = reviewsData?.items || [];

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <div>
          <h3 className="text-lg font-semibold text-gray-900">评审记录</h3>
          {reviewsData && (
            <p className="text-sm text-gray-500 mt-1">
              共 {reviewsData.totalCount} 条评审记录
            </p>
          )}
        </div>
        <Link 
          to={`/projects/${projectId}/reviews/new`}
          className="btn btn-primary"
        >
          <PlayIcon className="h-5 w-5 mr-2 inline-flex items-center space-x-1" />
          开始新评审
        </Link>
      </div>

      {reviews.length === 0 ? (
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
      ) : (
        <div className="card">
          <div className="overflow-hidden">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    评审标题
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    分支
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    状态
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    创建者
                  </th>
                  <th className="px-6 py-3 text-center text-xs font-medium text-gray-500 uppercase tracking-wider">
                    创建时间
                  </th>
                  <th className="relative px-6 py-3">
                    <span className="sr-only">操作</span>
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {reviews.map((review: Review) => (
                  <tr key={review.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-left">
                        <div className="flex-shrink-0">
                          {getStatusIcon(review.status)}
                        </div>
                        <div className="ml-3 text-center">
                          <div className="text-sm font-medium text-gray-900">
                            {review.title}
                          </div>
                          {review.description && (
                            <div className="text-sm text-gray-500 truncate max-w-xs">
                              {review.description}
                            </div>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-center">
                      <div className="text-sm text-gray-900">
                        {review.branch} <span className="text-gray-400">←</span> <span className="text-gray-500">{review.baseBranch}</span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-center">
                      <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(review.status)}`}>
                        {getStatusText(review.status)}
                      </span>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-center text-sm text-gray-900">
                      {review.authorName}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-center text-sm text-gray-500">
                      {new Date(review.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-center text-sm font-medium">
                      <Link
                        to={`/reviews/${review.id}`}
                        className="btn btn-primary btn-sm inline-flex items-center space-x-1"
                      >
                        <EyeIcon className="h-4 w-4 mr-1" />
                        查看详情
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
          
          {/* Pagination */}
          {reviewsData && reviewsData.totalPages > 1 && (
            <div className="bg-white px-4 py-3 flex items-center justify-between border-t border-gray-200 sm:px-6">
              <div className="flex-1 flex justify-between sm:hidden">
                <button className="relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
                  上一页
                </button>
                <button className="ml-3 relative inline-flex items-center px-4 py-2 border border-gray-300 text-sm font-medium rounded-md text-gray-700 bg-white hover:bg-gray-50">
                  下一页
                </button>
              </div>
              <div className="hidden sm:flex-1 sm:flex sm:items-center sm:justify-between">
                <div>
                  <p className="text-sm text-gray-700">
                    显示第 <span className="font-medium">{((reviewsData.page - 1) * reviewsData.pageSize) + 1}</span> 到{' '}
                    <span className="font-medium">
                      {Math.min(reviewsData.page * reviewsData.pageSize, reviewsData.totalCount)}
                    </span>{' '}
                    条，共 <span className="font-medium">{reviewsData.totalCount}</span> 条记录
                  </p>
                </div>
                <div>
                  <nav className="relative z-0 inline-flex rounded-md shadow-sm -space-x-px" aria-label="Pagination">
                    <button className="relative inline-flex items-center px-2 py-2 rounded-l-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50">
                      上一页
                    </button>
                    <button className="relative inline-flex items-center px-2 py-2 rounded-r-md border border-gray-300 bg-white text-sm font-medium text-gray-500 hover:bg-gray-50">
                      下一页
                    </button>
                  </nav>
                </div>
              </div>
            </div>
          )}
        </div>
      )}
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
              <h4 className="text-sm font-medium text-gray-900 text-left">
                {project.isActive ? '归档项目' : '取消归档'}
              </h4>
              <p className="text-sm text-gray-500 text-left mt-1">
                {project.isActive 
                  ? '归档后项目将不再显示在活跃列表中，但数据仍会保留'
                  : '取消归档后项目将重新显示在活跃列表中'
                }
              </p>
            </div>
            <button
              onClick={onArchive}
              disabled={isArchiving}
              className="btn btn-secondary inline-flex items-center space-x-1"
            >
              <CogIcon className="h-5 w-5 mr-2" />
              {isArchiving ? '处理中...' : (project.isActive ? '归档' : '取消归档')}
            </button>
          </div>

          {/* Delete Project */}
          <div className="flex items-center justify-between py-4">
            <div>
              <h4 className="text-sm font-medium text-red-900 text-left">删除项目</h4>
              <p className="text-sm text-red-600 text-left mt-1">
                删除项目将永久移除所有相关数据，此操作不可撤销
              </p>
            </div>
            <button
              onClick={() => setShowDeleteConfirm(true)}
              disabled={isDeleting}
              className="btn btn-danger inline-flex items-center space-x-1"
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