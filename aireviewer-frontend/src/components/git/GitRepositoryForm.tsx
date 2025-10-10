import React, { useState, useEffect } from 'react';
import Modal from '../common/Modal';
import type { GitRepository, CreateGitRepository } from '../../services/git.service';

interface GitRepositoryFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (repository: CreateGitRepository) => void;
  editingRepository?: GitRepository | null;
  isLoading?: boolean;
}

const GitRepositoryForm: React.FC<GitRepositoryFormProps> = ({
  isOpen,
  onClose,
  onSubmit,
  editingRepository,
  isLoading = false
}) => {
  const [formData, setFormData] = useState({
    name: '',
    url: '',
    defaultBranch: 'main',
    username: '',
    accessToken: '',
    projectId: undefined as number | undefined
  });

  useEffect(() => {
    if (editingRepository) {
      setFormData({
        name: editingRepository.name,
        url: editingRepository.url,
        defaultBranch: editingRepository.defaultBranch || 'main',
        username: editingRepository.username || '',
        accessToken: '', // 不显示现有token
        projectId: editingRepository.projectId
      });
    } else {
      setFormData({
        name: '',
        url: '',
        defaultBranch: 'main',
        username: '',
        accessToken: '',
        projectId: undefined
      });
    }
  }, [editingRepository, isOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    
    const submitData: CreateGitRepository = {
      name: formData.name,
      url: formData.url,
      defaultBranch: formData.defaultBranch || 'main',
      username: formData.username || undefined,
      accessToken: formData.accessToken || undefined,
      projectId: formData.projectId
    };
    
    onSubmit(submitData);
  };

  const handleUrlChange = (url: string) => {
    setFormData(prev => {
      const newData = { ...prev, url };
      
      // 自动填充仓库名称
      if (url && !prev.name) {
        const repoName = url.split('/').pop()?.replace('.git', '') || '';
        newData.name = repoName;
      }
      
      return newData;
    });
  };

  const getProviderInfo = (url: string) => {
    if (url.includes('github.com')) {
      return { name: 'GitHub', icon: '🐙', defaultBranch: 'main' };
    }
    if (url.includes('gitlab.com')) {
      return { name: 'GitLab', icon: '🦊', defaultBranch: 'main' };
    }
    if (url.includes('gitea')) {
      return { name: 'Gitea', icon: '🍃', defaultBranch: 'main' };
    }
    return { name: 'Git', icon: '📦', defaultBranch: 'main' };
  };

  const providerInfo = getProviderInfo(formData.url);

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={editingRepository ? '编辑Git仓库' : '添加Git仓库'}
      size="lg"
    >
      <form onSubmit={handleSubmit} className="space-y-6">
        {/* 仓库URL */}
        <div>
          <label htmlFor="url" className="block text-sm font-medium text-gray-700">
            仓库URL *
          </label>
          <div className="mt-1 flex items-center">
            <span className="text-2xl mr-2">{providerInfo.icon}</span>
            <input
              type="url"
              id="url"
              required
              value={formData.url}
              onChange={(e) => handleUrlChange(e.target.value)}
              className="flex-1 rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
              placeholder="https://github.com/username/repository.git"
            />
          </div>
          {formData.url && (
            <p className="mt-1 text-sm text-gray-500">
              检测到 {providerInfo.name} 仓库
            </p>
          )}
        </div>

        {/* 仓库名称 */}
        <div>
          <label htmlFor="name" className="block text-sm font-medium text-gray-700">
            仓库名称 *
          </label>
          <input
            type="text"
            id="name"
            required
            value={formData.name}
            onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            placeholder="仓库显示名称"
          />
        </div>

        {/* 默认分支 */}
        <div>
          <label htmlFor="defaultBranch" className="block text-sm font-medium text-gray-700">
            默认分支
          </label>
          <input
            type="text"
            id="defaultBranch"
            value={formData.defaultBranch}
            onChange={(e) => setFormData(prev => ({ ...prev, defaultBranch: e.target.value }))}
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            placeholder="main"
          />
          <p className="mt-1 text-sm text-gray-500">
            通常是 main 或 master
          </p>
        </div>

        {/* 认证信息 */}
        <div className="border-t border-gray-200 pt-6">
          <h3 className="text-lg font-medium text-gray-900 mb-4">认证信息</h3>
          <p className="text-sm text-gray-500 mb-4">
            如果是私有仓库，请提供访问凭据
          </p>
          
          <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
            <div>
              <label htmlFor="username" className="block text-sm font-medium text-gray-700">
                用户名
              </label>
              <input
                type="text"
                id="username"
                value={formData.username}
                onChange={(e) => setFormData(prev => ({ ...prev, username: e.target.value }))}
                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                placeholder="Git用户名"
              />
            </div>

            <div>
              <label htmlFor="accessToken" className="block text-sm font-medium text-gray-700">
                访问令牌
              </label>
              <input
                type="password"
                id="accessToken"
                value={formData.accessToken}
                onChange={(e) => setFormData(prev => ({ ...prev, accessToken: e.target.value }))}
                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
                placeholder="Personal Access Token"
              />
            </div>
          </div>
          
          <div className="mt-2 text-sm text-gray-500">
            <p>• GitHub: 使用 Personal Access Token</p>
            <p>• GitLab: 使用 Personal Access Token</p>
            <p>• Gitea: 使用应用令牌</p>
          </div>
        </div>

        {/* 项目关联 */}
        <div>
          <label htmlFor="projectId" className="block text-sm font-medium text-gray-700">
            关联项目（可选）
          </label>
          <input
            type="number"
            id="projectId"
            value={formData.projectId || ''}
            onChange={(e) => setFormData(prev => ({ 
              ...prev, 
              projectId: e.target.value ? parseInt(e.target.value) : undefined 
            }))}
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            placeholder="项目ID"
          />
          <p className="mt-1 text-sm text-gray-500">
            将此仓库关联到特定项目
          </p>
        </div>

        {/* 操作按钮 */}
        <div className="flex justify-end space-x-3 pt-6">
          <button
            type="button"
            onClick={onClose}
            className="inline-flex justify-center rounded-md border border-gray-300 bg-white px-4 py-2 text-sm font-medium text-gray-700 shadow-sm hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2"
          >
            取消
          </button>
          <button
            type="submit"
            disabled={isLoading}
            className="inline-flex justify-center rounded-md border border-transparent bg-blue-600 px-4 py-2 text-sm font-medium text-white shadow-sm hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-blue-500 focus:ring-offset-2 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {isLoading ? '保存中...' : (editingRepository ? '更新' : '创建')}
          </button>
        </div>
      </form>
    </Modal>
  );
};

export default GitRepositoryForm;