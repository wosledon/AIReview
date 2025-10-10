import React, { useState, useEffect } from 'react';
import Modal from '../common/Modal';
import type { LLMConfiguration } from '../../services/llm-configuration.service';

interface LLMConfigFormProps {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (config: {
    name: string;
    provider: string;
    apiKey: string;
    apiEndpoint: string;
    model: string;
    maxTokens: number;
    temperature: number;
    isActive: boolean;
    isDefault: boolean;
  }) => void;
  editingConfig?: LLMConfiguration | null;
  isLoading?: boolean;
}

const providerOptions = [
  { value: 'OpenAI', label: 'OpenAI' },
  { value: 'DeepSeek', label: 'DeepSeek' },
];

const LLMConfigForm: React.FC<LLMConfigFormProps> = ({
  isOpen,
  onClose,
  onSubmit,
  editingConfig,
  isLoading = false
}) => {
  const [formData, setFormData] = useState({
    name: '',
    provider: 'OpenAI',
    apiKey: '',
    apiEndpoint: '',
    model: '',
    maxTokens: 2048,
    temperature: 0.7,
    isActive: true,
    isDefault: false
  });

  useEffect(() => {
    if (editingConfig) {
      setFormData({
        name: editingConfig.name,
        provider: editingConfig.provider,
        apiKey: editingConfig.apiKey,
        apiEndpoint: editingConfig.apiEndpoint,
        model: editingConfig.model,
        maxTokens: editingConfig.maxTokens,
        temperature: editingConfig.temperature,
        isActive: editingConfig.isActive,
        isDefault: editingConfig.isDefault
      });
    } else {
      setFormData({
        name: '',
        provider: 'OpenAI',
        apiKey: '',
        apiEndpoint: '',
        model: '',
        maxTokens: 2048,
        temperature: 0.7,
        isActive: true,
        isDefault: false
      });
    }
  }, [editingConfig, isOpen]);

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    onSubmit(formData);
  };

  const handleProviderChange = (provider: string) => {
    setFormData(prev => ({
      ...prev,
      provider,
      // 根据提供商设置默认值
      apiEndpoint: provider === 'OpenAI' 
        ? 'https://api.openai.com/v1' 
        : provider === 'DeepSeek'
        ? 'https://api.deepseek.com/v1'
        : '',
      model: provider === 'OpenAI'
        ? 'gpt-3.5-turbo'
        : provider === 'DeepSeek'
        ? 'deepseek-chat'
        : ''
    }));
  };

  return (
    <Modal
      isOpen={isOpen}
      onClose={onClose}
      title={editingConfig ? '编辑LLM配置' : '添加LLM配置'}
      size="lg"
    >
      <form onSubmit={handleSubmit} className="space-y-6">
        {/* 基本信息 */}
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
          <div>
            <label htmlFor="name" className="block text-sm font-medium text-gray-700">
              配置名称 *
            </label>
            <input
              type="text"
              id="name"
              required
              value={formData.name}
              onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
              placeholder="例如：主要OpenAI配置"
            />
          </div>

          <div>
            <label htmlFor="provider" className="block text-sm font-medium text-gray-700">
              提供商 *
            </label>
            <select
              id="provider"
              required
              value={formData.provider}
              onChange={(e) => handleProviderChange(e.target.value)}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            >
              {providerOptions.map(option => (
                <option key={option.value} value={option.value}>
                  {option.label}
                </option>
              ))}
            </select>
          </div>
        </div>

        {/* API配置 */}
        <div>
          <label htmlFor="apiKey" className="block text-sm font-medium text-gray-700">
            API密钥 *
          </label>
          <input
            type="password"
            id="apiKey"
            required
            value={formData.apiKey}
            onChange={(e) => setFormData(prev => ({ ...prev, apiKey: e.target.value }))}
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            placeholder="sk-..."
          />
        </div>

        <div>
          <label htmlFor="apiEndpoint" className="block text-sm font-medium text-gray-700">
            API端点 *
          </label>
          <input
            type="url"
            id="apiEndpoint"
            required
            value={formData.apiEndpoint}
            onChange={(e) => setFormData(prev => ({ ...prev, apiEndpoint: e.target.value }))}
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            placeholder="https://api.openai.com/v1"
          />
        </div>

        <div>
          <label htmlFor="model" className="block text-sm font-medium text-gray-700">
            模型名称 *
          </label>
          <input
            type="text"
            id="model"
            required
            value={formData.model}
            onChange={(e) => setFormData(prev => ({ ...prev, model: e.target.value }))}
            className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            placeholder="gpt-3.5-turbo"
          />
        </div>

        {/* 模型参数 */}
        <div className="grid grid-cols-1 gap-6 sm:grid-cols-2">
          <div>
            <label htmlFor="maxTokens" className="block text-sm font-medium text-gray-700">
              最大令牌数
            </label>
            <input
              type="number"
              id="maxTokens"
              min="1"
              max="32000"
              value={formData.maxTokens}
              onChange={(e) => setFormData(prev => ({ ...prev, maxTokens: parseInt(e.target.value) }))}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            />
          </div>

          <div>
            <label htmlFor="temperature" className="block text-sm font-medium text-gray-700">
              温度 (0-2)
            </label>
            <input
              type="number"
              id="temperature"
              min="0"
              max="2"
              step="0.1"
              value={formData.temperature}
              onChange={(e) => setFormData(prev => ({ ...prev, temperature: parseFloat(e.target.value) }))}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 sm:text-sm"
            />
          </div>
        </div>

        {/* 状态设置 */}
        <div className="flex items-center space-x-6">
          <div className="flex items-center">
            <input
              id="isActive"
              type="checkbox"
              checked={formData.isActive}
              onChange={(e) => setFormData(prev => ({ ...prev, isActive: e.target.checked }))}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
            <label htmlFor="isActive" className="ml-2 block text-sm text-gray-900">
              启用配置
            </label>
          </div>

          <div className="flex items-center">
            <input
              id="isDefault"
              type="checkbox"
              checked={formData.isDefault}
              onChange={(e) => setFormData(prev => ({ ...prev, isDefault: e.target.checked }))}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
            <label htmlFor="isDefault" className="ml-2 block text-sm text-gray-900">
              设为默认
            </label>
          </div>
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
            {isLoading ? '保存中...' : (editingConfig ? '更新' : '创建')}
          </button>
        </div>
      </form>
    </Modal>
  );
};

export default LLMConfigForm;