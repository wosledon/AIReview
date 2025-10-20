import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  PlusIcon,
  CodeBracketIcon,
  CloudArrowDownIcon,
  ArrowPathIcon,
  CheckCircleIcon,
  ExclamationTriangleIcon,
  PencilIcon,
  TrashIcon
} from '@heroicons/react/24/outline';
import { 
  gitService,
  type GitRepository 
} from '../../services/git.service';
import GitRepositoryForm from '../../components/git/GitRepositoryForm';
import ConfirmDeleteModal from '../../components/common/ConfirmDeleteModal';
import { useTranslation } from 'react-i18next';

const GitRepositoriesPage: React.FC = () => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  
  // 状态管理
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingRepository, setEditingRepository] = useState<GitRepository | null>(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [repositoryToDelete, setRepositoryToDelete] = useState<GitRepository | null>(null);

  // 获取仓库列表
  const { data: repositoriesResponse, isLoading } = useQuery({
    queryKey: ['git-repositories'],
    queryFn: () => gitService.getRepositories(),
  });

  const repositories = repositoriesResponse?.data || [];

  // 创建仓库
  const createMutation = useMutation({
    mutationFn: (data: Parameters<typeof gitService.createRepository>[0]) => 
      gitService.createRepository(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['git-repositories'] });
      setIsFormOpen(false);
      setEditingRepository(null);
      console.log('仓库创建成功');
    },
    onError: (error: Error) => {
      console.error('仓库创建失败:', error);
    },
  });

  // 更新仓库
  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof gitService.updateRepository>[1] }) => 
      gitService.updateRepository(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['git-repositories'] });
      setIsFormOpen(false);
      setEditingRepository(null);
      console.log('仓库更新成功');
    },
    onError: (error: Error) => {
      console.error('仓库更新失败:', error);
    },
  });

  // 删除仓库
  const deleteMutation = useMutation({
    mutationFn: (id: number) => gitService.deleteRepository(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['git-repositories'] });
      setIsDeleteModalOpen(false);
      setRepositoryToDelete(null);
      console.log('仓库删除成功');
    },
    onError: (error: Error) => {
      console.error('仓库删除失败:', error);
    },
  });

  // Git操作
  const cloneMutation = useMutation({
    mutationFn: (id: number) => gitService.cloneRepository(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['git-repositories'] });
      console.log('仓库克隆成功');
    },
    onError: (error: Error) => {
      console.error('仓库克隆失败:', error);
    },
  });

  const syncMutation = useMutation({
    mutationFn: (id: number) => gitService.syncRepository(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['git-repositories'] });
      console.log('仓库同步成功');
    },
    onError: (error: Error) => {
      console.error('仓库同步失败:', error);
    },
  });

  const testMutation = useMutation({
    mutationFn: (id: number) => gitService.testRepository(id),
    onSuccess: (response) => {
      // 后端 ApiResponse<object> 使用 success 标志表示结果
      console.log(response.success ? '仓库连接正常' : '仓库连接失败');
    },
    onError: (error: Error) => {
      console.error('仓库测试失败:', error);
    },
  });

  // 事件处理函数
  const handleCreate = () => {
    setEditingRepository(null);
    setIsFormOpen(true);
  };

  const handleEdit = (repository: GitRepository) => {
    setEditingRepository(repository);
    setIsFormOpen(true);
  };

  const handleDelete = (repository: GitRepository) => {
    setRepositoryToDelete(repository);
    setIsDeleteModalOpen(true);
  };

  const handleFormSubmit = (formData: Parameters<typeof gitService.createRepository>[0]) => {
    if (editingRepository) {
      // 将 Create 结构适配为 Update 结构，并补齐必填的 isActive 字段
      const updateData: Parameters<typeof gitService.updateRepository>[1] = {
        name: formData.name,
        url: formData.url,
        defaultBranch: formData.defaultBranch,
        username: formData.username,
        accessToken: formData.accessToken,
        isActive: editingRepository.isActive,
      };
      updateMutation.mutate({ id: editingRepository.id, data: updateData });
    } else {
      createMutation.mutate(formData);
    }
  };

  const handleDeleteConfirm = () => {
    if (repositoryToDelete) {
      deleteMutation.mutate(repositoryToDelete.id);
    }
  };

  const handleClone = (repository: GitRepository) => {
    cloneMutation.mutate(repository.id);
  };

  const handleSync = (repository: GitRepository) => {
    syncMutation.mutate(repository.id);
  };

  const handleTest = (repository: GitRepository) => {
    testMutation.mutate(repository.id);
  };

  const getProviderIcon = (url: string) => {
    if (url.includes('github')) return '🐙';
    if (url.includes('gitlab')) return '🦊';
    if (url.includes('gitea')) return '🍃';
    return '📦';
  };

  if (isLoading) {
    return (
      <div className="flex justify-center items-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600"></div>
      </div>
    );
  }

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      {/* 页面标题 */}
      <div className="sm:flex sm:items-center">
        <div className="sm:flex-auto">
          <h1 className="text-2xl font-semibold text-gray-900">{t('gitRepos.title')}</h1>
          <p className="mt-2 text-sm text-gray-700">
            {t('gitRepos.subtitle')}
          </p>
        </div>
        <div className="mt-4 sm:ml-16 sm:mt-0 sm:flex-none">
          <button
            type="button"
            onClick={handleCreate}
            className="inline-flex items-center rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600"
          >
            <PlusIcon className="-ml-0.5 mr-1.5 h-5 w-5" aria-hidden="true" />
            {t('gitRepos.actions.add')}
          </button>
        </div>
      </div>

      {/* 仓库列表 */}
      <div className="mt-8 flow-root">
        <div className="-mx-4 -my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
          <div className="inline-block min-w-full py-2 align-middle sm:px-6 lg:px-8">
            <div className="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
              <table className="min-w-full divide-y divide-gray-300">
                <thead className="bg-gray-50">
                  <tr>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">
                      {t('gitRepos.table.repoInfo')}
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">
                      {t('gitRepos.table.status')}
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">
                      {t('gitRepos.table.branchCount')}
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">
                      {t('gitRepos.table.lastSync')}
                    </th>
                    <th scope="col" className="relative px-6 py-3">
                      <span className="sr-only">{t('gitRepos.table.actions')}</span>
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 bg-white">
                  {repositories.map((repository: GitRepository) => (
                    <tr key={repository.id} className="hover:bg-gray-50">
                      <td className="whitespace-nowrap px-6 py-4">
                        <div className="flex items-center">
                          <div className="flex-shrink-0">
                            <span className="text-2xl">{getProviderIcon(repository.url)}</span>
                          </div>
                          <div className="ml-4">
                            <div className="text-sm font-medium text-gray-900">
                              {repository.name}
                            </div>
                            <div className="text-sm text-gray-500">
                              {repository.url}
                            </div>
                            {repository.projectName && (
                              <div className="text-xs text-blue-600">
                                {t('gitRepos.labels.project')}: {repository.projectName}
                              </div>
                            )}
                          </div>
                        </div>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4">
                        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                          repository.isActive 
                            ? 'bg-green-100 text-green-800' 
                            : 'bg-red-100 text-red-800'
                        }`}>
                          {repository.isActive ? t('gitRepos.status.active') : t('gitRepos.status.inactive')}
                        </span>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4">
                        <div className="flex items-center">
                          <CodeBracketIcon className="h-4 w-4 text-gray-400 mr-1" />
                          <span className="text-sm text-gray-900">{repository.branchCount}</span>
                        </div>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4">
                        <div className="text-sm text-gray-900">
                          {repository.lastSyncAt 
                            ? new Date(repository.lastSyncAt).toLocaleString('zh-CN')
                            : t('gitRepos.status.neverSynced')
                          }
                        </div>
                      </td>
                      <td className="relative whitespace-nowrap py-4 pl-3 pr-4 text-right text-sm font-medium sm:pr-6">
                        <div className="flex items-center justify-end space-x-2">
                          {/* 克隆/同步 */}
                          <button
                            onClick={() => repository.localPath ? handleSync(repository) : handleClone(repository)}
                            disabled={!repository.isActive || cloneMutation.isPending || syncMutation.isPending}
                            className="text-blue-600 hover:text-blue-900 disabled:text-gray-400"
                            title={repository.localPath ? t('gitRepos.actions.sync') : t('gitRepos.actions.clone')}
                          >
                            {repository.localPath ? (
                              <ArrowPathIcon className="h-5 w-5" />
                            ) : (
                              <CloudArrowDownIcon className="h-5 w-5" />
                            )}
                          </button>
                          
                          {/* 测试连接 */}
                          <button
                            onClick={() => handleTest(repository)}
                            disabled={testMutation.isPending}
                            className="text-green-600 hover:text-green-900 disabled:text-gray-400"
                            title={t('gitRepos.actions.testConnection')}
                          >
                            <CheckCircleIcon className="h-5 w-5" />
                          </button>
                          
                          {/* 编辑 */}
                          <button
                            onClick={() => handleEdit(repository)}
                            className="text-blue-600 hover:text-blue-900"
                            title={t('gitRepos.actions.edit')}
                          >
                            <PencilIcon className="h-5 w-5" />
                          </button>
                          
                          {/* 删除 */}
                          <button
                            onClick={() => handleDelete(repository)}
                            className="text-red-600 hover:text-red-900"
                            title={t('gitRepos.actions.delete')}
                          >
                            <TrashIcon className="h-5 w-5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              
              {repositories.length === 0 && (
                <div className="text-center py-12">
                  <ExclamationTriangleIcon className="mx-auto h-12 w-12 text-gray-400" />
                  <h3 className="mt-2 text-sm font-medium text-gray-900">{t('gitRepos.empty.title')}</h3>
                  <p className="mt-1 text-sm text-gray-500">
                    {t('gitRepos.empty.description')}
                  </p>
                  <div className="mt-6">
                    <button
                      type="button"
                      onClick={handleCreate}
                      className="inline-flex items-center rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-500"
                    >
                      <PlusIcon className="-ml-0.5 mr-1.5 h-5 w-5" aria-hidden="true" />
                      {t('gitRepos.actions.add')}
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* Git仓库表单模态框 */}
      <GitRepositoryForm
        isOpen={isFormOpen}
        onClose={() => {
          setIsFormOpen(false);
          setEditingRepository(null);
        }}
        onSubmit={handleFormSubmit}
        editingRepository={editingRepository}
        isLoading={createMutation.isPending || updateMutation.isPending}
      />

      {/* 删除确认模态框 */}
      <ConfirmDeleteModal
        isOpen={isDeleteModalOpen}
        onClose={() => {
          setIsDeleteModalOpen(false);
          setRepositoryToDelete(null);
        }}
        onConfirm={handleDeleteConfirm}
        resourceLabel="Git仓库"
        itemName={repositoryToDelete?.name}
        isLoading={deleteMutation.isPending}
      />
    </div>
  );
};

export default GitRepositoriesPage;