import { useState } from 'react';
import { useNavigate } from 'react-router-dom';
import {
  ArrowLeftIcon,
  CogIcon,
  BellIcon,
  ShieldCheckIcon,
  CpuChipIcon,
  CheckCircleIcon,
  ExclamationTriangleIcon
} from '@heroicons/react/24/outline';

interface ReviewSettings {
  id: number;
  projectId: number;
  projectName: string;
  autoAssignReviewers: boolean;
  requireMinReviewers: number;
  allowSelfApproval: boolean;
  enforceStyleGuide: boolean;
  autoMergeOnApproval: boolean;
  aiReviewEnabled: boolean;
  aiReviewRules: {
    checkSecurity: boolean;
    checkPerformance: boolean;
    checkCodeQuality: boolean;
    checkDocumentation: boolean;
    strictnessLevel: 'low' | 'medium' | 'high';
  };
  notificationSettings: {
    emailNotifications: boolean;
    slackNotifications: boolean;
    webhookUrl?: string;
  };
  branchProtectionRules: {
    requireReviewBeforeMerge: boolean;
    dismissStaleReviews: boolean;
    requireStatusChecks: boolean;
    restrictPushes: boolean;
  };
}

export const ReviewSettingsPage = () => {
  const navigate = useNavigate();
  const [activeTab, setActiveTab] = useState<'general' | 'ai' | 'notifications' | 'protection'>('general');
  const [settings, setSettings] = useState<ReviewSettings>({
    id: 1,
    projectId: 1,
    projectName: 'AI代码评审平台',
    autoAssignReviewers: true,
    requireMinReviewers: 2,
    allowSelfApproval: false,
    enforceStyleGuide: true,
    autoMergeOnApproval: false,
    aiReviewEnabled: true,
    aiReviewRules: {
      checkSecurity: true,
      checkPerformance: true,
      checkCodeQuality: true,
      checkDocumentation: false,
      strictnessLevel: 'medium'
    },
    notificationSettings: {
      emailNotifications: true,
      slackNotifications: false,
      webhookUrl: ''
    },
    branchProtectionRules: {
      requireReviewBeforeMerge: true,
      dismissStaleReviews: true,
      requireStatusChecks: true,
      restrictPushes: true
    }
  });

  const [isSaving, setIsSaving] = useState(false);
  const [saveMessage, setSaveMessage] = useState<{ type: 'success' | 'error'; text: string } | null>(null);

  const handleSave = async () => {
    setIsSaving(true);
    try {
      // 模拟API调用
      await new Promise(resolve => setTimeout(resolve, 1000));
      setSaveMessage({ type: 'success', text: '设置已保存' });
      setTimeout(() => setSaveMessage(null), 3000);
    } catch {
      setSaveMessage({ type: 'error', text: '保存失败，请重试' });
      setTimeout(() => setSaveMessage(null), 3000);
    } finally {
      setIsSaving(false);
    }
  };

  const tabs = [
    { id: 'general', name: '常规设置', icon: CogIcon },
    { id: 'ai', name: 'AI评审', icon: CpuChipIcon },
    { id: 'notifications', name: '通知设置', icon: BellIcon },
    { id: 'protection', name: '分支保护', icon: ShieldCheckIcon },
  ] as const;

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <button
            onClick={() => navigate('/projects')}
            className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100"
          >
            <ArrowLeftIcon className="h-5 w-5" />
          </button>
          <div>
            <h1 className="text-2xl font-bold text-gray-900">评审设置</h1>
            <p className="text-gray-500 mt-1">配置 {settings.projectName} 的评审规则和流程</p>
          </div>
        </div>

        <div className="flex items-center space-x-3">
          {saveMessage && (
            <div className={`flex items-center space-x-2 px-4 py-2 rounded-lg ${
              saveMessage.type === 'success' 
                ? 'bg-green-100 text-green-800' 
                : 'bg-red-100 text-red-800'
            }`}>
              {saveMessage.type === 'success' ? (
                <CheckCircleIcon className="h-5 w-5" />
              ) : (
                <ExclamationTriangleIcon className="h-5 w-5" />
              )}
              <span className="text-sm font-medium">{saveMessage.text}</span>
            </div>
          )}
          <button
            onClick={handleSave}
            disabled={isSaving}
            className="btn btn-primary"
          >
            {isSaving ? '保存中...' : '保存设置'}
          </button>
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200">
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
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
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
        {activeTab === 'general' && (
          <GeneralSettings 
            settings={settings} 
            onSettingsChange={setSettings} 
          />
        )}
        {activeTab === 'ai' && (
          <AISettings 
            settings={settings} 
            onSettingsChange={setSettings} 
          />
        )}
        {activeTab === 'notifications' && (
          <NotificationSettings 
            settings={settings} 
            onSettingsChange={setSettings} 
          />
        )}
        {activeTab === 'protection' && (
          <ProtectionSettings 
            settings={settings} 
            onSettingsChange={setSettings} 
          />
        )}
      </div>
    </div>
  );
};

interface SettingsTabProps {
  settings: ReviewSettings;
  onSettingsChange: (settings: ReviewSettings) => void;
}

const GeneralSettings = ({ settings, onSettingsChange }: SettingsTabProps) => {
  const updateSettings = (updates: Partial<ReviewSettings>) => {
    onSettingsChange({ ...settings, ...updates });
  };

  return (
    <div className="space-y-6">
      <div className="card">
        <h3 className="text-lg font-semibold text-gray-900 mb-6">评审流程设置</h3>
        
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">自动分配评审者</h4>
              <p className="text-sm text-gray-500">当创建新的评审请求时自动分配评审者</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.autoAssignReviewers}
                onChange={(e) => updateSettings({ autoAssignReviewers: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-900 mb-2">
              最少评审者数量
            </label>
            <select
              className="input w-32"
              value={settings.requireMinReviewers}
              onChange={(e) => updateSettings({ requireMinReviewers: parseInt(e.target.value) })}
            >
              <option value={1}>1 人</option>
              <option value={2}>2 人</option>
              <option value={3}>3 人</option>
              <option value={4}>4 人</option>
              <option value={5}>5 人</option>
            </select>
            <p className="text-sm text-gray-500 mt-1">评审通过前需要的最少审批人数</p>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">允许自我审批</h4>
              <p className="text-sm text-gray-500">允许提交者审批自己的代码</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.allowSelfApproval}
                onChange={(e) => updateSettings({ allowSelfApproval: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">强制代码风格检查</h4>
              <p className="text-sm text-gray-500">要求通过代码风格检查才能提交</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.enforceStyleGuide}
                onChange={(e) => updateSettings({ enforceStyleGuide: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">自动合并</h4>
              <p className="text-sm text-gray-500">评审通过后自动合并到目标分支</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.autoMergeOnApproval}
                onChange={(e) => updateSettings({ autoMergeOnApproval: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>
        </div>
      </div>
    </div>
  );
};

const AISettings = ({ settings, onSettingsChange }: SettingsTabProps) => {
  const updateAIRules = (updates: Partial<ReviewSettings['aiReviewRules']>) => {
    onSettingsChange({
      ...settings,
      aiReviewRules: { ...settings.aiReviewRules, ...updates }
    });
  };

  return (
    <div className="space-y-6">
      <div className="card">
        <h3 className="text-lg font-semibold text-gray-900 mb-6">AI评审配置</h3>
        
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">启用AI评审</h4>
              <p className="text-sm text-gray-500">使用AI自动分析代码质量和安全性</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.aiReviewEnabled}
                onChange={(e) => onSettingsChange({ ...settings, aiReviewEnabled: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>

          {settings.aiReviewEnabled && (
            <>
              <div>
                <h4 className="text-sm font-medium text-gray-900 mb-4">检查项目</h4>
                <div className="space-y-3">
                  <label className="flex items-center">
                    <input
                      type="checkbox"
                      className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                      checked={settings.aiReviewRules.checkSecurity}
                      onChange={(e) => updateAIRules({ checkSecurity: e.target.checked })}
                    />
                    <span className="ml-3 text-sm text-gray-700">安全性检查</span>
                  </label>
                  <label className="flex items-center">
                    <input
                      type="checkbox"
                      className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                      checked={settings.aiReviewRules.checkPerformance}
                      onChange={(e) => updateAIRules({ checkPerformance: e.target.checked })}
                    />
                    <span className="ml-3 text-sm text-gray-700">性能优化建议</span>
                  </label>
                  <label className="flex items-center">
                    <input
                      type="checkbox"
                      className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                      checked={settings.aiReviewRules.checkCodeQuality}
                      onChange={(e) => updateAIRules({ checkCodeQuality: e.target.checked })}
                    />
                    <span className="ml-3 text-sm text-gray-700">代码质量分析</span>
                  </label>
                  <label className="flex items-center">
                    <input
                      type="checkbox"
                      className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                      checked={settings.aiReviewRules.checkDocumentation}
                      onChange={(e) => updateAIRules({ checkDocumentation: e.target.checked })}
                    />
                    <span className="ml-3 text-sm text-gray-700">文档完整性检查</span>
                  </label>
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-900 mb-2">
                  严格程度
                </label>
                <select
                  className="input w-40"
                  value={settings.aiReviewRules.strictnessLevel}
                  onChange={(e) => updateAIRules({ strictnessLevel: e.target.value as 'low' | 'medium' | 'high' })}
                >
                  <option value="low">宽松</option>
                  <option value="medium">中等</option>
                  <option value="high">严格</option>
                </select>
                <p className="text-sm text-gray-500 mt-1">
                  {settings.aiReviewRules.strictnessLevel === 'low' && '只检查严重问题'}
                  {settings.aiReviewRules.strictnessLevel === 'medium' && '检查常见问题和最佳实践'}
                  {settings.aiReviewRules.strictnessLevel === 'high' && '详细检查所有潜在问题'}
                </p>
              </div>
            </>
          )}
        </div>
      </div>
    </div>
  );
};

const NotificationSettings = ({ settings, onSettingsChange }: SettingsTabProps) => {
  const updateNotifications = (updates: Partial<ReviewSettings['notificationSettings']>) => {
    onSettingsChange({
      ...settings,
      notificationSettings: { ...settings.notificationSettings, ...updates }
    });
  };

  return (
    <div className="space-y-6">
      <div className="card">
        <h3 className="text-lg font-semibold text-gray-900 mb-6">通知设置</h3>
        
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">邮件通知</h4>
              <p className="text-sm text-gray-500">通过邮件接收评审状态更新</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.notificationSettings.emailNotifications}
                onChange={(e) => updateNotifications({ emailNotifications: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">Slack通知</h4>
              <p className="text-sm text-gray-500">通过Slack接收评审状态更新</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.notificationSettings.slackNotifications}
                onChange={(e) => updateNotifications({ slackNotifications: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>

          <div>
            <label className="block text-sm font-medium text-gray-900 mb-2">
              Webhook URL
            </label>
            <input
              type="url"
              className="input"
              placeholder="https://hooks.slack.com/services/..."
              value={settings.notificationSettings.webhookUrl || ''}
              onChange={(e) => updateNotifications({ webhookUrl: e.target.value })}
            />
            <p className="text-sm text-gray-500 mt-1">用于接收通知的Webhook地址</p>
          </div>
        </div>
      </div>
    </div>
  );
};

const ProtectionSettings = ({ settings, onSettingsChange }: SettingsTabProps) => {
  const updateProtection = (updates: Partial<ReviewSettings['branchProtectionRules']>) => {
    onSettingsChange({
      ...settings,
      branchProtectionRules: { ...settings.branchProtectionRules, ...updates }
    });
  };

  return (
    <div className="space-y-6">
      <div className="card">
        <h3 className="text-lg font-semibold text-gray-900 mb-6">分支保护规则</h3>
        
        <div className="space-y-6">
          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">合并前需要评审</h4>
              <p className="text-sm text-gray-500">所有合并请求必须经过评审才能合并</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.branchProtectionRules.requireReviewBeforeMerge}
                onChange={(e) => updateProtection({ requireReviewBeforeMerge: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">撤销过时评审</h4>
              <p className="text-sm text-gray-500">当有新提交时自动撤销之前的评审</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.branchProtectionRules.dismissStaleReviews}
                onChange={(e) => updateProtection({ dismissStaleReviews: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">要求状态检查</h4>
              <p className="text-sm text-gray-500">合并前必须通过所有CI/CD检查</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.branchProtectionRules.requireStatusChecks}
                onChange={(e) => updateProtection({ requireStatusChecks: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>

          <div className="flex items-center justify-between">
            <div>
              <h4 className="text-sm font-medium text-gray-900">限制直接推送</h4>
              <p className="text-sm text-gray-500">防止直接推送到受保护分支</p>
            </div>
            <label className="relative inline-flex items-center cursor-pointer">
              <input
                type="checkbox"
                className="sr-only peer"
                checked={settings.branchProtectionRules.restrictPushes}
                onChange={(e) => updateProtection({ restrictPushes: e.target.checked })}
              />
              <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 rounded-full peer peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all peer-checked:bg-primary-600"></div>
            </label>
          </div>
        </div>
      </div>
    </div>
  );
};