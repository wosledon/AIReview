import React, { useState, useEffect } from 'react';
import { 
  PlusIcon, 
  KeyIcon, 
  TrashIcon, 
  EyeIcon,
  EyeSlashIcon
} from '@heroicons/react/24/outline';
import { gitCredentialService, type GitCredential, type CreateGitCredentialRequest, type SshKeyPair } from '../../services/git-credential.service';
import Modal from '../common/Modal';

const CredentialManagement: React.FC = () => {
  const [credentials, setCredentials] = useState<GitCredential[]>([]);
  const [loading, setLoading] = useState(true);
  const [showCreateModal, setShowCreateModal] = useState(false);
  const [showKeyModal, setShowKeyModal] = useState(false);
  const [generatedKeyPair, setGeneratedKeyPair] = useState<SshKeyPair | null>(null);
  const [error, setError] = useState<string | null>(null);

  // 表单状态
  const [formData, setFormData] = useState<CreateGitCredentialRequest>({
    name: '',
    type: 'Token',
    provider: '',
    username: '',
    secret: '',
    privateKey: '',
    isDefault: false
  });

  const [showSecret, setShowSecret] = useState(false);

  const fetchCredentials = async () => {
    try {
      setLoading(true);
      const response = await gitCredentialService.getUserCredentials();
      if (response.success && response.data) {
        setCredentials(response.data);
      } else {
        setError(response.message || '获取凭证失败');
      }
    } catch (err) {
      setError('获取凭证列表失败');
      console.error('Error fetching credentials:', err);
    } finally {
      setLoading(false);
    }
  };

  const handleCreate = async () => {
    try {
      const response = await gitCredentialService.createCredential(formData);
      if (response.success) {
        setShowCreateModal(false);
        setFormData({
          name: '',
          type: 'Token',
          provider: '',
          username: '',
          secret: '',
          privateKey: '',
          isDefault: false
        });
        fetchCredentials();
      } else {
        setError(response.message || '创建凭证失败');
      }
    } catch (err) {
      setError('创建凭证失败');
      console.error('Error creating credential:', err);
    }
  };

  const handleDelete = async (id: number) => {
    if (!confirm('确定要删除此凭证吗？')) return;
    
    try {
      const response = await gitCredentialService.deleteCredential(id);
      if (response.success) {
        fetchCredentials();
      } else {
        setError(response.message || '删除凭证失败');
      }
    } catch (err) {
      setError('删除凭证失败');
      console.error('Error deleting credential:', err);
    }
  };

  const handleSetDefault = async (id: number) => {
    try {
      const response = await gitCredentialService.setDefaultCredential(id);
      if (response.success) {
        fetchCredentials();
      } else {
        setError(response.message || '设置默认失败');
      }
    } catch (err) {
      setError('设置默认失败');
      console.error('Error setting default:', err);
    }
  };

  const handleGenerateSSH = async () => {
    try {
      const response = await gitCredentialService.generateSshKeyPair();
      if (response.success && response.data) {
        setGeneratedKeyPair(response.data);
        setShowKeyModal(true);
      } else {
        setError(response.message || '生成SSH密钥失败');
      }
    } catch (err) {
      setError('生成SSH密钥失败');
      console.error('Error generating SSH key:', err);
    }
  };

  const handleUseGeneratedKey = () => {
    if (generatedKeyPair) {
      setFormData(prev => ({
        ...prev,
        type: 'SSH',
        privateKey: generatedKeyPair.privateKey
      }));
      setShowKeyModal(false);
      setShowCreateModal(true);
    }
  };

  useEffect(() => {
    fetchCredentials();
  }, []);

  const getTypeIcon = (type: string) => {
    switch (type) {
      case 'SSH':
        return <KeyIcon className="h-5 w-5 text-blue-500" />;
      default:
        return <KeyIcon className="h-5 w-5 text-gray-500" />;
    }
  };

  const getStatusBadge = (credential: GitCredential) => {
    if (credential.isDefault) {
      return <span className="inline-flex px-2 py-1 text-xs font-medium bg-blue-100 text-blue-800 rounded-full">默认</span>;
    }
    if (credential.isVerified) {
      return <span className="inline-flex px-2 py-1 text-xs font-medium bg-green-100 text-green-800 rounded-full">已验证</span>;
    }
    return <span className="inline-flex px-2 py-1 text-xs font-medium bg-gray-100 text-gray-800 rounded-full">未验证</span>;
  };

  return (
    <div className="space-y-6">
      <div className="flex justify-between items-center">
        <h2 className="text-2xl font-bold text-gray-900">Git 凭证管理</h2>
        <div className="flex space-x-2">
          <button
            onClick={handleGenerateSSH}
            className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50"
          >
            <KeyIcon className="h-4 w-4 mr-2" />
            生成SSH密钥
          </button>
          <button
            onClick={() => setShowCreateModal(true)}
            className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700"
          >
            <PlusIcon className="h-4 w-4 mr-2" />
            添加凭证
          </button>
        </div>
      </div>

      {error && (
        <div className="bg-red-50 border border-red-200 rounded-md p-4">
          <div className="text-sm text-red-600">{error}</div>
          <button
            onClick={() => setError(null)}
            className="mt-2 text-red-600 hover:text-red-800 text-sm underline"
          >
            关闭
          </button>
        </div>
      )}

      {loading ? (
        <div className="flex justify-center py-8">
          <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-blue-600"></div>
        </div>
      ) : (
        <div className="bg-white shadow overflow-hidden sm:rounded-md">
          <ul className="divide-y divide-gray-200">
            {credentials.length === 0 ? (
              <li className="px-6 py-8 text-center text-gray-500">
                暂无凭证，点击"添加凭证"开始配置
              </li>
            ) : (
              credentials.map((credential) => (
                <li key={credential.id} className="px-6 py-4">
                  <div className="flex items-center justify-between">
                    <div className="flex items-center space-x-3">
                      {getTypeIcon(credential.type)}
                      <div>
                        <h3 className="text-sm font-medium text-gray-900">{credential.name}</h3>
                        <p className="text-sm text-gray-500">
                          {credential.type} • {credential.username || credential.provider}
                        </p>
                        <p className="text-xs text-gray-400">
                          创建于 {new Date(credential.createdAt).toLocaleDateString()}
                        </p>
                      </div>
                    </div>
                    
                    <div className="flex items-center space-x-4">
                      {getStatusBadge(credential)}
                      
                      <div className="flex space-x-2">
                        {!credential.isDefault && (
                          <button
                            onClick={() => handleSetDefault(credential.id)}
                            className="text-blue-600 hover:text-blue-800 text-sm"
                          >
                            设为默认
                          </button>
                        )}
                        
                        <button
                          onClick={() => handleDelete(credential.id)}
                          className="text-red-600 hover:text-red-800"
                        >
                          <TrashIcon className="h-4 w-4" />
                        </button>
                      </div>
                    </div>
                  </div>
                </li>
              ))
            )}
          </ul>
        </div>
      )}

      {/* 创建凭证模态框 */}
      <Modal isOpen={showCreateModal} onClose={() => setShowCreateModal(false)} title="添加 Git 凭证">
        <div className="space-y-4">
          <div>
            <label className="block text-sm font-medium text-gray-700">名称</label>
            <input
              type="text"
              value={formData.name}
              onChange={(e) => setFormData(prev => ({ ...prev, name: e.target.value }))}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
              placeholder="凭证名称"
            />
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">类型</label>
            <select
              value={formData.type}
              onChange={(e) => setFormData(prev => ({ ...prev, type: e.target.value }))}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
            >
              <option value="Token">访问令牌</option>
              <option value="SSH">SSH 密钥</option>
              <option value="UsernamePassword">用户名密码</option>
            </select>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-700">提供商</label>
            <input
              type="text"
              value={formData.provider}
              onChange={(e) => setFormData(prev => ({ ...prev, provider: e.target.value }))}
              className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
              placeholder="GitHub, GitLab, 等"
            />
          </div>

          {formData.type !== 'SSH' && (
            <div>
              <label className="block text-sm font-medium text-gray-700">用户名</label>
              <input
                type="text"
                value={formData.username}
                onChange={(e) => setFormData(prev => ({ ...prev, username: e.target.value }))}
                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
              />
            </div>
          )}

          {formData.type === 'SSH' ? (
            <div>
              <label className="block text-sm font-medium text-gray-700">私钥</label>
              <textarea
                value={formData.privateKey}
                onChange={(e) => setFormData(prev => ({ ...prev, privateKey: e.target.value }))}
                rows={6}
                className="mt-1 block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500"
                placeholder="-----BEGIN RSA PRIVATE KEY-----..."
              />
            </div>
          ) : (
            <div>
              <label className="block text-sm font-medium text-gray-700">
                {formData.type === 'Token' ? '访问令牌' : '密码'}
              </label>
              <div className="mt-1 relative">
                <input
                  type={showSecret ? 'text' : 'password'}
                  value={formData.secret}
                  onChange={(e) => setFormData(prev => ({ ...prev, secret: e.target.value }))}
                  className="block w-full rounded-md border-gray-300 shadow-sm focus:border-blue-500 focus:ring-blue-500 pr-10"
                />
                <button
                  type="button"
                  onClick={() => setShowSecret(!showSecret)}
                  className="absolute inset-y-0 right-0 pr-3 flex items-center"
                >
                  {showSecret ? (
                    <EyeSlashIcon className="h-4 w-4 text-gray-400" />
                  ) : (
                    <EyeIcon className="h-4 w-4 text-gray-400" />
                  )}
                </button>
              </div>
            </div>
          )}

          <div className="flex items-center">
            <input
              type="checkbox"
              checked={formData.isDefault}
              onChange={(e) => setFormData(prev => ({ ...prev, isDefault: e.target.checked }))}
              className="h-4 w-4 text-blue-600 focus:ring-blue-500 border-gray-300 rounded"
            />
            <label className="ml-2 block text-sm text-gray-900">设为默认凭证</label>
          </div>

          <div className="flex justify-end space-x-3">
            <button
              onClick={() => setShowCreateModal(false)}
              className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50"
            >
              取消
            </button>
            <button
              onClick={handleCreate}
              className="px-4 py-2 bg-blue-600 border border-transparent rounded-md text-sm font-medium text-white hover:bg-blue-700"
            >
              创建
            </button>
          </div>
        </div>
      </Modal>

      {/* SSH密钥生成结果模态框 */}
      <Modal isOpen={showKeyModal} onClose={() => setShowKeyModal(false)} title="生成的 SSH 密钥对">
        {generatedKeyPair && (
          <div className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">公钥（添加到Git服务器）</label>
              <textarea
                value={generatedKeyPair.publicKey}
                readOnly
                rows={3}
                className="block w-full rounded-md border-gray-300 bg-gray-50 text-sm"
              />
            </div>
            
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">私钥（保密）</label>
              <textarea
                value={generatedKeyPair.privateKey}
                readOnly
                rows={8}
                className="block w-full rounded-md border-gray-300 bg-gray-50 text-sm font-mono"
              />
            </div>

            <div className="bg-yellow-50 border border-yellow-200 rounded-md p-3">
              <p className="text-sm text-yellow-800">
                请将公钥添加到您的Git服务器（GitHub、GitLab等），并妥善保管私钥。
              </p>
            </div>

            <div className="flex justify-end space-x-3">
              <button
                onClick={() => setShowKeyModal(false)}
                className="px-4 py-2 border border-gray-300 rounded-md text-sm font-medium text-gray-700 hover:bg-gray-50"
              >
                关闭
              </button>
              <button
                onClick={handleUseGeneratedKey}
                className="px-4 py-2 bg-blue-600 border border-transparent rounded-md text-sm font-medium text-white hover:bg-blue-700"
              >
                使用此密钥创建凭证
              </button>
            </div>
          </div>
        )}
      </Modal>
    </div>
  );
};

export default CredentialManagement;