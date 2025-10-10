import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
import { 
  PlusIcon, 
  MagnifyingGlassIcon,
  FolderIcon,
  CalendarIcon,
  UserGroupIcon,
  EllipsisVerticalIcon
} from '@heroicons/react/24/outline';
import { projectService } from '../services/project.service';
import type { Project } from '../types/project';

export const ProjectsPage = () => {
  const [searchTerm, setSearchTerm] = useState('');
  const [showArchived, setShowArchived] = useState(false);

  const {
    data: projectsData,
    isLoading,
    error,
    refetch
  } = useQuery({
    queryKey: ['projects', { search: searchTerm, isActive: !showArchived }],
    queryFn: () => projectService.getProjects({
      search: searchTerm || undefined,
      isActive: !showArchived,
      pageSize: 20
    }),
  });

  const projects = projectsData?.items || [];

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">
          <p>加载项目时出错</p>
        </div>
        <button 
          onClick={() => refetch()}
          className="btn btn-primary"
        >
          重新加载
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">项目管理</h1>
          <p className="mt-1 text-gray-500">
            管理您的代码项目，配置AI评审规则
          </p>
        </div>
        <div className="mt-4 sm:mt-0">
          <Link to="/projects/new" className="btn btn-primary inline-flex items-center space-x-1">
            <PlusIcon className="h-5 w-5 mr-2" />
            创建项目
          </Link>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex flex-col sm:flex-row sm:items-center gap-4">
          <div className="flex-1">
            <div className="relative">
              <MagnifyingGlassIcon className="h-5 w-5 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                placeholder="搜索项目..."
                className="input pl-10"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </div>
          </div>
          <div className="flex items-center">
            <input
              id="show-archived"
              type="checkbox"
              className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
              checked={showArchived}
              onChange={(e) => setShowArchived(e.target.checked)}
            />
            <label htmlFor="show-archived" className="ml-2 text-sm text-gray-700">
              显示已归档项目
            </label>
          </div>
        </div>
      </div>

      {/* Projects Grid */}
      {projects.length === 0 ? (
        <div className="text-center py-12">
          <FolderIcon className="h-16 w-16 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            {searchTerm ? '未找到匹配的项目' : '还没有项目'}
          </h3>
          <p className="text-gray-500 mb-6">
            {searchTerm 
              ? '尝试调整搜索条件或创建新项目'
              : '创建您的第一个项目开始使用AI代码评审'
            }
          </p>
          {!searchTerm && (
            <Link to="/projects/new" className="btn btn-primary inline-flex items-center space-x-1">
              <PlusIcon className="h-5 w-5" />
              创建项目
            </Link>
          )}
        </div>
      ) : (
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-6">
          {projects.map((project: Project) => (
            <ProjectCard key={project.id} project={project} />
          ))}
        </div>
      )}

      {/* Pagination */}
      {projectsData && projectsData.totalPages > 1 && (
        <div className="flex items-center justify-center space-x-2">
          {/* Add pagination controls here */}
          <p className="text-sm text-gray-500">
            显示 {projects.length} 个项目，共 {projectsData.totalCount} 个
          </p>
        </div>
      )}
    </div>
  );
};

interface ProjectCardProps {
  project: Project;
}

const ProjectCard = ({ project }: ProjectCardProps) => {
  return (
    <div className="card hover:shadow-lg transition-shadow">
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-center">
          <div className="p-2 bg-primary-100 rounded-lg">
            <FolderIcon className="h-6 w-6 text-primary-600" />
          </div>
          <div className="ml-3">
            <h3 className="text-lg font-semibold text-gray-900">
              <Link 
                to={`/projects/${project.id}`}
                className="hover:text-primary-600"
              >
                {project.name}
              </Link>
            </h3>
            <p className="text-sm text-gray-500">{project.repositoryUrl}</p>
          </div>
        </div>
        <button className="p-1 text-gray-400 hover:text-gray-600">
          <EllipsisVerticalIcon className="h-5 w-5" />
        </button>
      </div>

      {project.description && (
        <p className="text-gray-600 text-sm mb-4 line-clamp-2">
          {project.description}
        </p>
      )}

      <div className="flex items-center justify-between text-sm text-gray-500">
        <div className="flex items-center">
          <CalendarIcon className="h-4 w-4 mr-1" />
          <span>
            创建于 {new Date(project.createdAt).toLocaleDateString('zh-CN')}
          </span>
        </div>
        <div className="flex items-center">
          <UserGroupIcon className="h-4 w-4 mr-1" />
          <span>
            {project.memberCount || 0} 人
          </span>
        </div>
      </div>

      <div className="mt-4 pt-4 border-t border-gray-200">
        <div className="flex items-center justify-between">
          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
            project.isActive 
              ? 'bg-green-100 text-green-800' 
              : 'bg-gray-100 text-gray-800'
          }`}>
            {project.isActive ? '活跃' : '已归档'}
          </span>
          
          <div className="flex space-x-2">
            <Link
              to={`/projects/${project.id}/reviews`}
              className="text-primary-600 hover:text-primary-700 text-sm font-medium"
            >
              查看评审
            </Link>
            <Link
              to={`/projects/${project.id}`}
              className="text-primary-600 hover:text-primary-700 text-sm font-medium"
            >
              项目设置
            </Link>
          </div>
        </div>
      </div>
    </div>
  );
};