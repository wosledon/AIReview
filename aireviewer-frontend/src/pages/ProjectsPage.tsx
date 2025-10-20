import { useState, useEffect, useMemo } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { 
  PlusIcon, 
  MagnifyingGlassIcon,
  FolderIcon,
  CalendarIcon,
  UserGroupIcon,
  EllipsisVerticalIcon,
  Cog6ToothIcon
} from '@heroicons/react/24/outline';
import { projectService } from '../services/project.service';
import type { Project } from '../types/project';
import type { PagedResult } from '../types/review';

export const ProjectsPage = () => {
  const { t } = useTranslation();
  const [searchTerm, setSearchTerm] = useState('');
  const [debouncedSearchTerm, setDebouncedSearchTerm] = useState('');
  const [showArchived, setShowArchived] = useState(false);

  // 防抖搜索
  useEffect(() => {
    const timer = setTimeout(() => {
      setDebouncedSearchTerm(searchTerm);
    }, 300);

    return () => clearTimeout(timer);
  }, [searchTerm]);

  const params = useMemo(() => ({
    search: debouncedSearchTerm || undefined,
    isActive: !showArchived,
    pageSize: 20
  }), [debouncedSearchTerm, showArchived]);

  const {
    data: projectsData,
    isLoading,
    isFetching,
    error,
    refetch
  } = useQuery<PagedResult<Project>, Error, PagedResult<Project>>({
    queryKey: ['projects', params] as const,
    queryFn: (): Promise<PagedResult<Project>> => projectService.getProjects(params),
    placeholderData: keepPreviousData,
    staleTime: 1000, // 1秒内数据不会重新获取
  });

  const projects = projectsData?.items || [];

  // 仅在首次加载且没有任何数据时展示整页加载
  if (isLoading && !projectsData) {
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
          <p>{t('projects.loading_error')}</p>
        </div>
        <button 
          onClick={() => refetch()}
          className="btn btn-primary"
        >
          {t('projects.reload')}
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 text-left">{t('projects.title')}</h1>
          <p className="mt-1 text-gray-500 mt-2">
            {t('projects.subtitle')}
          </p>
        </div>
        <div className="mt-4 sm:mt-0">
          <Link to="/projects/new" className="btn btn-primary inline-flex items-center space-x-1">
            <PlusIcon className="h-5 w-5 mr-2" />
            {t('projects.create')}
          </Link>
        </div>
      </div>

      {/* Filters */}
  <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
        <div className="flex flex-col sm:flex-row sm:items-center gap-4">
          <div className="flex-1">
            <div className="relative">
              <MagnifyingGlassIcon className="h-5 w-5 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                placeholder={t('projects.search_placeholder')}
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
              {t('projects.show_archived')}
            </label>
          </div>
          {isFetching && projectsData && (
            <div className="flex items-center text-gray-500 text-sm ml-auto">
              <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-primary-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
              </svg>
              {t('projects.updating')}
            </div>
          )}
        </div>
      </div>

      {/* Projects Grid */}
      {projects.length === 0 ? (
        <div className="text-center py-12">
          <FolderIcon className="h-16 w-16 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            {searchTerm ? t('projects.no_match') : t('projects.no_projects')}
          </h3>
          <p className="text-gray-500 mb-6">
            {searchTerm 
              ? t('projects.try_adjust')
              : t('projects.no_projects_desc')
            }
          </p>
          {!searchTerm && (
            <Link to="/projects/new" className="btn btn-primary inline-flex items-center space-x-1">
              <PlusIcon className="h-5 w-5" />
              {t('projects.create')}
            </Link>
          )}
        </div>
      ) : (
        <div className="grid grid-cols-1 sm:grid-cols-2 lg:grid-cols-3 xl:grid-cols-4 gap-6">
          {projects.map((project: Project) => (
            <ProjectCard key={project.id} project={project} t={t} />
          ))}
        </div>
      )}

      {/* Pagination */}
      {projectsData && projectsData.totalPages > 1 && (
        <div className="flex items-center justify-center space-x-2">
          {/* Add pagination controls here */}
          <p className="text-sm text-gray-500">
            {t('projects.showing', { count: projects.length, total: projectsData.totalCount })}
          </p>
        </div>
      )}
    </div>
  );
};

interface ProjectCardProps {
  project: Project;
  t: (key: string) => string;
}

const ProjectCard = ({ project, t }: ProjectCardProps) => {
  return (
  <div className="card hover:shadow-lg transition-shadow h-full flex flex-col dark:bg-gray-900 dark:border-gray-800">
      {/* Header with icon, title and menu */}
      <div className="flex items-start justify-between mb-4">
        <div className="flex items-start space-x-3 flex-1 min-w-0">
          <div className="p-2 bg-primary-100 rounded-lg flex-shrink-0">
            <FolderIcon className="h-6 w-6 text-primary-600" />
          </div>
          <div className="flex-1 min-w-0">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-1">
              <Link 
                to={`/projects/${project.id}`}
                className="hover:text-primary-600 block truncate"
                title={project.name}
              >
                {project.name}
              </Link>
            </h3>
            {project.repositoryUrl && (
              <p className="text-sm text-gray-500 dark:text-gray-400 truncate" title={project.repositoryUrl}>
                {project.repositoryUrl}
              </p>
            )}
          </div>
        </div>
        <div className="flex-shrink-0 ml-2">
          <button className="p-1 text-gray-400 hover:text-gray-600 rounded">
            <EllipsisVerticalIcon className="h-5 w-5" />
          </button>
        </div>
      </div>

      {/* Description */}
      <div className="flex-1">
        {project.description && (
          <p className="text-gray-600 dark:text-gray-300 text-sm mb-4 line-clamp-2">
            {project.description}
          </p>
        )}
      </div>

      {/* Stats */}
      <div className="flex items-center justify-between text-sm text-gray-500 mb-4">
        <div className="flex items-center min-w-0 mr-2">
          <CalendarIcon className="h-4 w-4 mr-1 flex-shrink-0" />
          <span className="truncate">
            {new Date(project.createdAt).toLocaleDateString()}
          </span>
        </div>
        <div className="flex items-center flex-shrink-0">
          <UserGroupIcon className="h-4 w-4 mr-1" />
          <span>
            {project.memberCount || 0} {t('projects.members')}
          </span>
        </div>
      </div>

      {/* Footer with status and actions */}
      <div className="pt-4 border-t border-gray-200 mt-auto">
        <div className="flex items-center justify-between mb-3">
          <span className={`inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
            project.isActive !== false
              ? 'bg-green-100 text-green-800' 
              : 'bg-gray-100 text-gray-800'
          }`}>
            {project.isActive !== false ? t('projects.active') : t('projects.archived')}
          </span>

          <Link
            to={`/projects/${project.id}`}
            className="text-primary-600 hover:text-primary-700 text-sm font-medium px-2 py-1 rounded hover:bg-primary-50 transition-colors flex items-center"
          >
            <Cog6ToothIcon className="h-4 w-4 mr-1" />
            {t('projects.project_settings')}
          </Link>
        </div>
      </div>
    </div>
  );
};