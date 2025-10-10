import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import { useMutation } from '@tanstack/react-query';
import { 
  ArrowLeftIcon,
  FolderIcon,
  LinkIcon,
  CodeBracketIcon 
} from '@heroicons/react/24/outline';
import { projectService } from '../services/project.service';
import type { CreateProjectRequest } from '../types/project';

const PROGRAMMING_LANGUAGES = [
  { value: 'javascript', label: 'JavaScript' },
  { value: 'typescript', label: 'TypeScript' },
  { value: 'python', label: 'Python' },
  { value: 'java', label: 'Java' },
  { value: 'csharp', label: 'C#' },
  { value: 'cpp', label: 'C++' },
  { value: 'go', label: 'Go' },
  { value: 'rust', label: 'Rust' },
  { value: 'php', label: 'PHP' },
  { value: 'ruby', label: 'Ruby' },
  { value: 'swift', label: 'Swift' },
  { value: 'kotlin', label: 'Kotlin' },
  { value: 'other', label: '其他' }
];

export const CreateProjectPage = () => {
  const navigate = useNavigate();
  const [formData, setFormData] = useState<CreateProjectRequest>({
    name: '',
    description: '',
    repositoryUrl: '',
    language: ''
  });
  const [errors, setErrors] = useState<Record<string, string>>({});

  const createProjectMutation = useMutation({
    mutationFn: (data: CreateProjectRequest) => projectService.createProject(data),
    onSuccess: (project) => {
      navigate(`/projects/${project.id}`);
    },
    onError: (error) => {
      console.error('Failed to create project:', error);
      setErrors({ submit: '创建项目失败，请稍后重试' });
    }
  });

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement | HTMLTextAreaElement | HTMLSelectElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value
    }));
    
    // Clear error when user starts typing
    if (errors[name]) {
      setErrors(prev => ({
        ...prev,
        [name]: ''
      }));
    }
  };

  const validateForm = (): boolean => {
    const newErrors: Record<string, string> = {};

    if (!formData.name.trim()) {
      newErrors.name = '项目名称不能为空';
    }

    if (!formData.language) {
      newErrors.language = '请选择编程语言';
    }

    if (formData.repositoryUrl && !isValidUrl(formData.repositoryUrl)) {
      newErrors.repositoryUrl = '请输入有效的仓库URL';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const isValidUrl = (url: string): boolean => {
    try {
      new URL(url);
      return true;
    } catch {
      return false;
    }
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) {
      return;
    }

    createProjectMutation.mutate(formData);
  };

  return (
    <div className="max-w-2xl mx-auto space-y-6">
      {/* Header */}
      <div className="flex items-center space-x-4">
        <button
          onClick={() => navigate('/projects')}
          className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100"
        >
          <ArrowLeftIcon className="h-5 w-5" />
        </button>
        <div>
          <h1 className="text-2xl font-bold text-gray-900">创建新项目</h1>
          <p className="text-gray-500">设置项目信息，开始使用AI代码评审</p>
        </div>
      </div>

      {/* Form */}
      <div className="card">
        <form onSubmit={handleSubmit} className="space-y-6">
          {/* Project Name */}
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700 mb-2">
              项目名称 *
            </label>
            <div className="relative">
              <FolderIcon className="h-5 w-5 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <input
                id="name"
                name="name"
                type="text"
                required
                className={`input pl-10 ${errors.name ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                placeholder="输入项目名称"
                value={formData.name}
                onChange={handleInputChange}
                disabled={createProjectMutation.isPending}
              />
            </div>
            {errors.name && (
              <p className="mt-1 text-sm text-red-600">{errors.name}</p>
            )}
          </div>

          {/* Description */}
          <div>
            <label htmlFor="description" className="block text-sm font-medium text-gray-700 mb-2">
              项目描述
            </label>
            <textarea
              id="description"
              name="description"
              rows={3}
              className="input resize-none"
              placeholder="简要描述项目的功能和特点（可选）"
              value={formData.description}
              onChange={handleInputChange}
              disabled={createProjectMutation.isPending}
            />
          </div>

          {/* Repository URL */}
          <div>
            <label htmlFor="repositoryUrl" className="block text-sm font-medium text-gray-700 mb-2">
              仓库地址
            </label>
            <div className="relative">
              <LinkIcon className="h-5 w-5 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <input
                id="repositoryUrl"
                name="repositoryUrl"
                type="url"
                className={`input pl-10 ${errors.repositoryUrl ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                placeholder="https://github.com/username/repository"
                value={formData.repositoryUrl}
                onChange={handleInputChange}
                disabled={createProjectMutation.isPending}
              />
            </div>
            {errors.repositoryUrl && (
              <p className="mt-1 text-sm text-red-600">{errors.repositoryUrl}</p>
            )}
            <p className="mt-1 text-sm text-gray-500">
              支持 GitHub、GitLab、Bitbucket 等代码托管平台
            </p>
          </div>

          {/* Programming Language */}
          <div>
            <label htmlFor="language" className="block text-sm font-medium text-gray-700 mb-2">
              主要编程语言 *
            </label>
            <div className="relative">
              <CodeBracketIcon className="h-5 w-5 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <select
                id="language"
                name="language"
                required
                className={`input pl-10 ${errors.language ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                value={formData.language}
                onChange={handleInputChange}
                disabled={createProjectMutation.isPending}
              >
                <option value="">选择编程语言</option>
                {PROGRAMMING_LANGUAGES.map((lang) => (
                  <option key={lang.value} value={lang.value}>
                    {lang.label}
                  </option>
                ))}
              </select>
            </div>
            {errors.language && (
              <p className="mt-1 text-sm text-red-600">{errors.language}</p>
            )}
          </div>

          {/* Error Message */}
          {errors.submit && (
            <div className="bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-md text-sm">
              {errors.submit}
            </div>
          )}

          {/* Actions */}
          <div className="flex items-center justify-end space-x-3 pt-6 border-t border-gray-200">
            <button
              type="button"
              onClick={() => navigate('/projects')}
              className="btn btn-secondary"
              disabled={createProjectMutation.isPending}
            >
              取消
            </button>
            <button
              type="submit"
              className="btn btn-primary"
              disabled={createProjectMutation.isPending}
            >
              {createProjectMutation.isPending ? '创建中...' : '创建项目'}
            </button>
          </div>
        </form>
      </div>

      {/* Tips */}
      <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
        <h3 className="text-sm font-medium text-blue-900 mb-2">💡 提示</h3>
        <ul className="text-sm text-blue-800 space-y-1">
          <li>• 项目创建后，您可以邀请团队成员参与代码评审</li>
          <li>• AI评审引擎会根据选择的编程语言优化评审规则</li>
          <li>• 支持配置自定义评审规则和质量标准</li>
        </ul>
      </div>
    </div>
  );
};