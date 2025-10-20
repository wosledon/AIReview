import { useState } from 'react';
import { useNavigate, useSearchParams, useParams } from 'react-router-dom';
import { useMutation, useQuery } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { 
  ArrowLeftIcon,
  ExclamationTriangleIcon,
  InformationCircleIcon
} from '@heroicons/react/24/outline';
import { reviewService } from '../services/review.service';
import { projectService } from '../services/project.service';
import type { CreateReviewRequest } from '../types/review';
import type { Project } from '../types/project';

export const CreateReviewPage = () => {
  const navigate = useNavigate();
  const { t } = useTranslation();
  const [searchParams] = useSearchParams();
  const params = useParams();
  
  // 项目ID可能来自URL参数（/projects/:id/reviews/new）或查询参数（/reviews/new?projectId=1）
  const projectIdFromParams = params.id ? parseInt(params.id) : null;
  const projectIdFromQuery = searchParams.get('projectId');
  const initialProjectId = projectIdFromParams || (projectIdFromQuery ? parseInt(projectIdFromQuery) : 0);

  const [formData, setFormData] = useState<CreateReviewRequest>({
    projectId: initialProjectId,
    title: '',
    description: '',
    branch: '',
    baseBranch: 'main',
    pullRequestNumber: undefined
  });

  const [errors, setErrors] = useState<{
    projectId?: string;
    title?: string;
    branch?: string;
    general?: string;
  }>({});

  // 获取项目列表
  const {
    data: projectsData,
    isLoading: isProjectsLoading
  } = useQuery({
    queryKey: ['projects'],
    queryFn: () => projectService.getProjects({ pageSize: 100 }),
  });

  const projects = projectsData?.items || [];

  // 创建评审
  const createReviewMutation = useMutation({
    mutationFn: (data: CreateReviewRequest) => reviewService.createReview(data),
    onSuccess: (review) => {
      navigate(`/reviews/${review.id}`);
    },
    onError: (error: Error) => {
      setErrors({
        general: error.message || t('createReview.error_create_failed')
      });
    }
  });

  const validateForm = (): boolean => {
    const newErrors: typeof errors = {};

    if (!formData.projectId) {
      newErrors.projectId = t('createReview.project_required');
    }

    if (!formData.title.trim()) {
      newErrors.title = t('createReview.title_required');
    }

    if (!formData.branch.trim()) {
      newErrors.branch = t('createReview.branch_required');
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    createReviewMutation.mutate(formData);
  };

  const handleInputChange = (field: keyof CreateReviewRequest, value: string | number | undefined) => {
    setFormData(prev => ({ ...prev, [field]: value }));
    
    // 清除对应字段的错误
    if (errors[field as keyof typeof errors]) {
      setErrors(prev => ({ ...prev, [field]: undefined }));
    }
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center space-x-4">
        <button
          onClick={() => navigate(projectIdFromParams ? `/projects/${projectIdFromParams}` : '/reviews')}
          className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100"
        >
          <ArrowLeftIcon className="h-5 w-5" />
        </button>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">{t('createReview.title')}</h1>
          <p className="text-gray-500">{t('createReview.subtitle')}</p>
        </div>
      </div>

      {/* Form */}
      <div className="card">
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* General Error */}
          {errors.general && (
            <div className="bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-md text-sm flex items-center">
              <ExclamationTriangleIcon className="h-5 w-5 mr-2 flex-shrink-0" />
              {errors.general}
            </div>
          )}

          {/* Project Selection */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              {t('createReview.project_label')} *
            </label>
            <select
              className={`input ${errors.projectId ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
              value={formData.projectId}
              onChange={(e) => handleInputChange('projectId', parseInt(e.target.value))}
              disabled={isProjectsLoading || createReviewMutation.isPending}
            >
              <option value={0}>{t('createReview.project_placeholder')}</option>
              {projects.map((project: Project) => (
                <option key={project.id} value={project.id}>
                  {project.name}
                </option>
              ))}
            </select>
            {errors.projectId && (
              <p className="mt-1 text-sm text-red-600">{errors.projectId}</p>
            )}
          </div>

          {/* Review Title */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              {t('createReview.title_label')} *
            </label>
            <input
              type="text"
              className={`input ${errors.title ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
              placeholder={t('createReview.title_placeholder')}
              value={formData.title}
              onChange={(e) => handleInputChange('title', e.target.value)}
              disabled={createReviewMutation.isPending}
            />
            {errors.title && (
              <p className="mt-1 text-sm text-red-600">{errors.title}</p>
            )}
          </div>

          {/* Description */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              {t('createReview.description_label')}
            </label>
            <textarea
              rows={4}
              className="input resize-none"
              placeholder={t('createReview.description_placeholder')}
              value={formData.description}
              onChange={(e) => handleInputChange('description', e.target.value)}
              disabled={createReviewMutation.isPending}
            />
          </div>

          {/* Branch Information */}
          <div className="grid grid-cols-2 gap-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                {t('createReview.branch_label')} *
              </label>
              <input
                type="text"
                className={`input ${errors.branch ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                placeholder={t('createReview.branch_placeholder')}
                value={formData.branch}
                onChange={(e) => handleInputChange('branch', e.target.value)}
                disabled={createReviewMutation.isPending}
              />
              {errors.branch && (
                <p className="mt-1 text-sm text-red-600">{errors.branch}</p>
              )}
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                {t('createReview.base_branch_label')}
              </label>
              <input
                type="text"
                className="input"
                placeholder={t('createReview.base_branch_placeholder')}
                value={formData.baseBranch}
                onChange={(e) => handleInputChange('baseBranch', e.target.value)}
                disabled={createReviewMutation.isPending}
              />
            </div>
          </div>

          {/* Pull Request Number */}
          <div>
            <label className="block text-sm font-medium text-gray-700 mb-2">
              {t('createReview.pr_number_label')}
            </label>
            <input
              type="number"
              className="input"
              placeholder={t('createReview.pr_number_placeholder')}
              value={formData.pullRequestNumber || ''}
              onChange={(e) => handleInputChange('pullRequestNumber', e.target.value ? parseInt(e.target.value) : undefined)}
              disabled={createReviewMutation.isPending}
            />
            <p className="mt-1 text-sm text-gray-500">
              {t('createReview.pr_number_hint')}
            </p>
          </div>

          {/* Info Box */}
          <div className="bg-blue-50 border border-blue-200 text-blue-700 px-4 py-3 rounded-md text-sm flex items-start">
            <InformationCircleIcon className="h-5 w-5 mr-2 flex-shrink-0 mt-0.5" />
            <div>
              <p className="font-medium mb-1">{t('createReview.info_title')}</p>
              <p>{t('createReview.info_content')}</p>
            </div>
          </div>

          {/* Submit Buttons */}
          <div className="flex items-center justify-end space-x-3 pt-6 border-t border-gray-200">
            <button
              type="button"
              onClick={() => navigate(projectIdFromParams ? `/projects/${projectIdFromParams}` : '/reviews')}
              className="btn btn-secondary"
              disabled={createReviewMutation.isPending}
            >
              {t('createReview.cancel')}
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={createReviewMutation.isPending}
            >
              {createReviewMutation.isPending ? t('createReview.submitting') : t('createReview.submit')}
            </button>
          </div>
        </form>
      </div>
    </div>
  );
};