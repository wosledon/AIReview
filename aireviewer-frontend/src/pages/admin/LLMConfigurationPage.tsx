import React, { useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { 
  CheckCircleIcon,
  ExclamationTriangleIcon,
  CogIcon,
  PlusIcon,
  PencilIcon,
  TrashIcon
} from '@heroicons/react/24/outline';
import { 
  llmConfigurationService,
  type LLMConfiguration 
} from '../../services/llm-configuration.service';
import LLMConfigForm from '../../components/admin/LLMConfigForm';
import DeleteConfirmModal from '../../components/admin/DeleteConfirmModal';
import { useTranslation } from 'react-i18next';

const LLMConfigurationPage: React.FC = () => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  
  // 状态管理
  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editingConfig, setEditingConfig] = useState<LLMConfiguration | null>(null);
  const [isDeleteModalOpen, setIsDeleteModalOpen] = useState(false);
  const [configToDelete, setConfigToDelete] = useState<LLMConfiguration | null>(null);

  // 获取配置列表
  const { data: configurationsResponse, isLoading } = useQuery({
    queryKey: ['llm-configurations'],
    queryFn: () => llmConfigurationService.getAll(),
  });

  const configurations = configurationsResponse || [];

  // 创建配置
  const createMutation = useMutation({
    mutationFn: (data: Parameters<typeof llmConfigurationService.create>[0]) => 
      llmConfigurationService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['llm-configurations'] });
      setIsFormOpen(false);
      setEditingConfig(null);
      console.log('配置创建成功');
    },
    onError: (error: Error) => {
      console.error('配置创建失败:', error);
    },
  });

  // 更新配置
  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: Parameters<typeof llmConfigurationService.update>[1] }) => 
      llmConfigurationService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['llm-configurations'] });
      setIsFormOpen(false);
      setEditingConfig(null);
      console.log('配置更新成功');
    },
    onError: (error: Error) => {
      console.error('配置更新失败:', error);
    },
  });

  // 删除配置
  const deleteMutation = useMutation({
    mutationFn: (id: number) => llmConfigurationService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['llm-configurations'] });
      setIsDeleteModalOpen(false);
      setConfigToDelete(null);
      console.log('配置删除成功');
    },
    onError: (error: Error) => {
      console.error('配置删除失败:', error);
    },
  });

  // 设置默认配置
  const setDefaultMutation = useMutation({
    mutationFn: (id: number) => llmConfigurationService.setDefault(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['llm-configurations'] });
      console.log('默认配置设置成功');
    },
    onError: (error: Error) => {
      console.error('默认配置设置失败:', error);
    },
  });

  // 测试连接
  const testConnectionMutation = useMutation({
    mutationFn: (id: number) => llmConfigurationService.testConnection(id),
    onSuccess: (response, id) => {
      const config = configurations.find((c: LLMConfiguration) => c.id === id);
        if (response.isConnected) {
        console.log(`${config?.name} 连接测试成功`);
      } else {
          console.error(`${config?.name} 连接测试失败: ${response.message}`);
      }
    },
    onError: (error: Error, id) => {
      const config = configurations.find((c: LLMConfiguration) => c.id === id);
      console.error(`${config?.name} 连接测试失败:`, error);
    },
  });

  // 事件处理函数
  const handleCreate = () => {
    setEditingConfig(null);
    setIsFormOpen(true);
  };

  const handleEdit = (config: LLMConfiguration) => {
    setEditingConfig(config);
    setIsFormOpen(true);
  };

  const handleDelete = (config: LLMConfiguration) => {
    setConfigToDelete(config);
    setIsDeleteModalOpen(true);
  };

  const handleFormSubmit = (formData: Parameters<typeof llmConfigurationService.create>[0]) => {
    if (editingConfig) {
      updateMutation.mutate({ id: editingConfig.id, data: formData });
    } else {
      createMutation.mutate(formData);
    }
  };

  const handleDeleteConfirm = () => {
    if (configToDelete) {
      deleteMutation.mutate(configToDelete.id);
    }
  };

  const handleSetDefault = (config: LLMConfiguration) => {
    if (!config.isDefault) {
      setDefaultMutation.mutate(config.id);
    }
  };

  const handleTestConnection = (config: LLMConfiguration) => {
    testConnectionMutation.mutate(config.id);
  };

  const getProviderIcon = (provider: string) => {
    switch (provider) {
      case 'OpenAI':
        return '🤖';
      case 'DeepSeek':
        return '🧠';
      default:
        return '⚡';
    }
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
          <h1 className="text-2xl font-semibold text-gray-900">{t('llmConfig.title')}</h1>
          <p className="mt-2 text-sm text-gray-700">
            {t('llmConfig.subtitle')}
          </p>
        </div>
        <div className="mt-4 sm:ml-16 sm:mt-0 sm:flex-none">
          <button
            type="button"
            onClick={handleCreate}
            className="inline-flex items-center rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-500 focus-visible:outline focus-visible:outline-2 focus-visible:outline-offset-2 focus-visible:outline-blue-600"
          >
            <PlusIcon className="-ml-0.5 mr-1.5 h-5 w-5" aria-hidden="true" />
            {t('llmConfig.actions.add')}
          </button>
        </div>
      </div>

      {/* 配置列表 */}
      <div className="mt-8 flow-root">
        <div className="-mx-4 -my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
          <div className="inline-block min-w-full py-2 align-middle sm:px-6 lg:px-8">
            <div className="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
              <table className="min-w-full divide-y divide-gray-300">
                <thead className="bg-gray-50">
                  <tr>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">
                      {t('llmConfig.table.configInfo')}
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">
                      {t('llmConfig.table.provider')}
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">
                      {t('llmConfig.table.modelParams')}
                    </th>
                    <th scope="col" className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">
                      {t('llmConfig.table.status')}
                    </th>
                    <th scope="col" className="relative px-6 py-3">
                      <span className="sr-only">{t('llmConfig.table.actions')}</span>
                    </th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 bg-white">
                  {configurations.map((config: LLMConfiguration) => (
                    <tr key={config.id} className="hover:bg-gray-50">
                      <td className="whitespace-nowrap px-6 py-4">
                        <div className="flex items-center">
                          <div className="flex-shrink-0">
                            <span className="text-2xl">{getProviderIcon(config.provider)}</span>
                          </div>
                          <div className="ml-4">
                            <div className="flex items-center">
                              <div className="text-sm font-medium text-gray-900">
                                {config.name}
                              </div>
                              {config.isDefault && (
                                <span className="ml-2 inline-flex items-center rounded-full bg-green-100 px-2.5 py-0.5 text-xs font-medium text-green-800">
                                  {t('llmConfig.status.default')}
                                </span>
                              )}
                            </div>
                            <div className="text-sm text-gray-500">
                              {config.apiEndpoint}
                            </div>
                          </div>
                        </div>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4">
                        <div className="text-sm text-gray-900">{config.provider}</div>
                        <div className="text-sm text-gray-500">{config.model}</div>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4">
                        <div className="text-sm text-gray-900">
                          {t('llmConfig.params.maxTokens')}: {config.maxTokens}
                        </div>
                        <div className="text-sm text-gray-500">
                          {t('llmConfig.params.temperature')}: {config.temperature}
                        </div>
                      </td>
                      <td className="whitespace-nowrap px-6 py-4">
                        <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${
                          config.isActive 
                            ? 'bg-green-100 text-green-800' 
                            : 'bg-red-100 text-red-800'
                        }`}>
                          {config.isActive ? t('llmConfig.status.active') : t('llmConfig.status.inactive')}
                        </span>
                      </td>
                      <td className="relative whitespace-nowrap py-4 pl-3 pr-4 text-right text-sm font-medium sm:pr-6">
                        <div className="flex items-center justify-end space-x-2">
                          {/* 编辑 */}
                          <button
                            onClick={() => handleEdit(config)}
                            className="text-blue-600 hover:text-blue-900"
                            title={t('llmConfig.actions.edit')}
                          >
                            <PencilIcon className="h-5 w-5" />
                          </button>
                          
                          {/* 测试连接 */}
                          <button
                            onClick={() => handleTestConnection(config)}
                            disabled={!config.isActive || testConnectionMutation.isPending}
                            className="text-blue-600 hover:text-blue-900 disabled:text-gray-400"
                            title={t('llmConfig.actions.testConnection')}
                          >
                            <CogIcon className="h-5 w-5" />
                          </button>
                          
                          {/* 设为默认 */}
                          {!config.isDefault && config.isActive && (
                            <button
                              onClick={() => handleSetDefault(config)}
                              disabled={setDefaultMutation.isPending}
                              className="text-green-600 hover:text-green-900"
                              title={t('llmConfig.actions.setDefault')}
                            >
                              <CheckCircleIcon className="h-5 w-5" />
                            </button>
                          )}
                          
                          {/* 删除 */}
                          <button
                            onClick={() => handleDelete(config)}
                            disabled={config.isDefault}
                            className="text-red-600 hover:text-red-900 disabled:text-gray-400"
                            title={config.isDefault ? t('llmConfig.messages.cannotDeleteDefault') : t('llmConfig.actions.delete')}
                          >
                            <TrashIcon className="h-5 w-5" />
                          </button>
                        </div>
                      </td>
                    </tr>
                  ))}
                </tbody>
              </table>
              
              {configurations.length === 0 && (
                <div className="text-center py-12">
                  <ExclamationTriangleIcon className="mx-auto h-12 w-12 text-gray-400" />
                  <h3 className="mt-2 text-sm font-medium text-gray-900">{t('llmConfig.empty.title')}</h3>
                  <p className="mt-1 text-sm text-gray-500">
                    {t('llmConfig.empty.description')}
                  </p>
                  <div className="mt-6">
                    <button
                      type="button"
                      onClick={handleCreate}
                      className="inline-flex items-center rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-500"
                    >
                      <PlusIcon className="-ml-0.5 mr-1.5 h-5 w-5" aria-hidden="true" />
                      {t('llmConfig.actions.add')}
                    </button>
                  </div>
                </div>
              )}
            </div>
          </div>
        </div>
      </div>

      {/* LLM配置表单模态框 */}
      <LLMConfigForm
        isOpen={isFormOpen}
        onClose={() => {
          setIsFormOpen(false);
          setEditingConfig(null);
        }}
        onSubmit={handleFormSubmit}
        editingConfig={editingConfig}
        isLoading={createMutation.isPending || updateMutation.isPending}
      />

      {/* 删除确认模态框 */}
      <DeleteConfirmModal
        isOpen={isDeleteModalOpen}
        onClose={() => {
          setIsDeleteModalOpen(false);
          setConfigToDelete(null);
        }}
        onConfirm={handleDeleteConfirm}
        config={configToDelete}
        isLoading={deleteMutation.isPending}
      />
    </div>
  );
};

export default LLMConfigurationPage;