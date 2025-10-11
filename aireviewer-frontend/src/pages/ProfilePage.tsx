import React, { useState } from 'react';
import { useMutation } from '@tanstack/react-query';
import { 
  UserIcon, 
  CameraIcon, 
  KeyIcon, 
  BellIcon, 
  ShieldCheckIcon,
  PencilIcon,
  EyeIcon,
  EyeSlashIcon
} from '@heroicons/react/24/outline';
import { useAuth } from '../contexts/AuthContext';
import { useNotifications } from '../hooks/useNotifications';

interface UserProfile {
  id: string;
  email: string;
  displayName: string;
  avatar?: string;
  bio?: string;
  location?: string;
  website?: string;
  githubUsername?: string;
  preferences: {
    emailNotifications: boolean;
    pushNotifications: boolean;
    reviewReminders: boolean;
    weeklyDigest: boolean;
  };
}

interface PasswordChangeForm {
  currentPassword: string;
  newPassword: string;
  confirmPassword: string;
}

export const ProfilePage: React.FC = () => {
  const { user } = useAuth();
  const { addNotification } = useNotifications();
  
  // State management
  const [isEditingProfile, setIsEditingProfile] = useState(false);
  const [isChangingPassword, setIsChangingPassword] = useState(false);
  const [showCurrentPassword, setShowCurrentPassword] = useState(false);
  const [showNewPassword, setShowNewPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  
  // Form data
  const [profileForm, setProfileForm] = useState<Partial<UserProfile>>({
    displayName: user?.displayName || '',
    bio: '',
    location: '',
    website: '',
    githubUsername: '',
    preferences: {
      emailNotifications: true,
      pushNotifications: true,
      reviewReminders: true,
      weeklyDigest: false,
    }
  });
  
  const [passwordForm, setPasswordForm] = useState<PasswordChangeForm>({
    currentPassword: '',
    newPassword: '',
    confirmPassword: ''
  });

  // Mock API calls - replace with actual API service
  const updateProfileMutation = useMutation({
    mutationFn: async (data: Partial<UserProfile>) => {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      return data;
    },
    onSuccess: () => {
      addNotification({
        type: 'profile_update',
        message: '个人资料已更新',
        timestamp: new Date().toISOString(),
        content: '您的个人资料信息已成功保存'
      });
      setIsEditingProfile(false);
    },
    onError: () => {
      addNotification({
        type: 'profile_update',
        message: '更新失败',
        timestamp: new Date().toISOString(),
        content: '更新个人资料时发生错误，请重试'
      });
    }
  });

  const changePasswordMutation = useMutation({
    mutationFn: async (data: PasswordChangeForm) => {
      // Simulate API call
      await new Promise(resolve => setTimeout(resolve, 1000));
      if (data.newPassword !== data.confirmPassword) {
        throw new Error('密码确认不匹配');
      }
      return data;
    },
    onSuccess: () => {
      addNotification({
        type: 'security_update',
        message: '密码已更新',
        timestamp: new Date().toISOString(),
        content: '您的密码已成功更改'
      });
      setIsChangingPassword(false);
      setPasswordForm({
        currentPassword: '',
        newPassword: '',
        confirmPassword: ''
      });
    },
    onError: (error) => {
      addNotification({
        type: 'security_update',
        message: '密码更新失败',
        timestamp: new Date().toISOString(),
        content: error instanceof Error ? error.message : '更改密码时发生错误'
      });
    }
  });

  const uploadAvatarMutation = useMutation({
    mutationFn: async (file: File) => {
      // Simulate file upload
      await new Promise(resolve => setTimeout(resolve, 1500));
      return URL.createObjectURL(file);
    },
    onSuccess: (avatarUrl) => {
      setProfileForm(prev => ({ ...prev, avatar: avatarUrl }));
      addNotification({
        type: 'profile_update',
        message: '头像已更新',
        timestamp: new Date().toISOString(),
        content: '您的头像已成功上传'
      });
    },
    onError: () => {
      addNotification({
        type: 'profile_update',
        message: '头像上传失败',
        timestamp: new Date().toISOString(),
        content: '上传头像时发生错误，请重试'
      });
    }
  });

  const handleAvatarUpload = (event: React.ChangeEvent<HTMLInputElement>) => {
    const file = event.target.files?.[0];
    if (file) {
      if (file.size > 5 * 1024 * 1024) { // 5MB limit
        addNotification({
          type: 'profile_update',
          message: '文件过大',
          timestamp: new Date().toISOString(),
          content: '头像文件不能超过5MB'
        });
        return;
      }
      uploadAvatarMutation.mutate(file);
    }
  };

  const handleProfileSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    updateProfileMutation.mutate(profileForm);
  };

  const handlePasswordSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (passwordForm.newPassword !== passwordForm.confirmPassword) {
      addNotification({
        type: 'security_update',
        message: '密码确认不匹配',
        timestamp: new Date().toISOString(),
        content: '请确保新密码和确认密码一致'
      });
      return;
    }
    changePasswordMutation.mutate(passwordForm);
  };

  const displayName = user?.displayName || user?.email || '';
  const initials = (() => {
    const src = displayName || '';
    if (!src) return '?';
    const parts = src.trim().split(/\s+/);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return src[0]?.toUpperCase() || '?';
  })();

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 transition-colors">
      <div className="max-w-4xl mx-auto py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8 fade-in">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100">个人资料</h1>
          <p className="mt-2 text-gray-600 dark:text-gray-400">管理您的账户信息和偏好设置</p>
        </div>

        <div className="space-y-8">
          {/* Profile Information */}
          <div className="card dark:bg-gray-900 dark:border-gray-800 fade-in">
            <div className="flex items-center justify-between mb-6">
              <div className="flex items-center space-x-2">
                <UserIcon className="h-6 w-6 text-gray-500 dark:text-gray-400" />
                <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">基本信息</h2>
              </div>
              <button
                onClick={() => setIsEditingProfile(!isEditingProfile)}
                className="btn btn-secondary transition-all hover:scale-105 inline-flex items-center space-x-1"
                disabled={updateProfileMutation.isPending}
              >
                <PencilIcon className="h-4 w-4 mr-2" />
                {isEditingProfile ? '取消编辑' : '编辑资料'}
              </button>
            </div>

            <form onSubmit={handleProfileSubmit} className="space-y-6">
              {/* Avatar Section */}
              <div className="flex items-center space-x-6">
                <div className="relative">
                  {profileForm.avatar || user?.avatar ? (
                    <img
                      src={profileForm.avatar || user?.avatar}
                      alt={displayName}
                      className="h-24 w-24 rounded-full object-cover ring-4 ring-gray-200 dark:ring-gray-700 transition-all"
                    />
                  ) : (
                    <div className="h-24 w-24 rounded-full bg-primary-100 dark:bg-primary-900/40 flex items-center justify-center ring-4 ring-gray-200 dark:ring-gray-700 transition-all">
                      <span className="text-2xl font-semibold text-primary-700 dark:text-primary-400">
                        {initials}
                      </span>
                    </div>
                  )}
                  {isEditingProfile && (
                    <label className="absolute bottom-0 right-0 bg-white dark:bg-gray-800 rounded-full p-2 shadow-lg border border-gray-200 dark:border-gray-700 cursor-pointer hover:bg-gray-50 dark:hover:bg-gray-700 transition-all hover:scale-105">
                      <CameraIcon className="h-4 w-4 text-gray-600 dark:text-gray-400" />
                      <input
                        type="file"
                        className="hidden"
                        accept="image/*"
                        onChange={handleAvatarUpload}
                        disabled={uploadAvatarMutation.isPending}
                      />
                    </label>
                  )}
                  {uploadAvatarMutation.isPending && (
                    <div className="absolute inset-0 bg-black bg-opacity-50 rounded-full flex items-center justify-center">
                      <div className="animate-spin rounded-full h-6 w-6 border-b-2 border-white"></div>
                    </div>
                  )}
                </div>
                <div>
                  <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100">{displayName}</h3>
                  <p className="text-gray-600 dark:text-gray-400">{user?.email}</p>
                  {isEditingProfile && (
                    <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                      点击相机图标更换头像（最大5MB）
                    </p>
                  )}
                </div>
              </div>

              {/* Form Fields */}
              <div className="grid grid-cols-1 md:grid-cols-2 gap-6">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    显示名称
                  </label>
                  <input
                    type="text"
                    className="input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                    value={profileForm.displayName}
                    onChange={(e) => setProfileForm(prev => ({ ...prev, displayName: e.target.value }))}
                    disabled={!isEditingProfile || updateProfileMutation.isPending}
                    placeholder="请输入显示名称"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    GitHub用户名
                  </label>
                  <input
                    type="text"
                    className="input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                    value={profileForm.githubUsername}
                    onChange={(e) => setProfileForm(prev => ({ ...prev, githubUsername: e.target.value }))}
                    disabled={!isEditingProfile || updateProfileMutation.isPending}
                    placeholder="请输入GitHub用户名"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    所在地
                  </label>
                  <input
                    type="text"
                    className="input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                    value={profileForm.location}
                    onChange={(e) => setProfileForm(prev => ({ ...prev, location: e.target.value }))}
                    disabled={!isEditingProfile || updateProfileMutation.isPending}
                    placeholder="请输入所在地"
                  />
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    个人网站
                  </label>
                  <input
                    type="url"
                    className="input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                    value={profileForm.website}
                    onChange={(e) => setProfileForm(prev => ({ ...prev, website: e.target.value }))}
                    disabled={!isEditingProfile || updateProfileMutation.isPending}
                    placeholder="https://example.com"
                  />
                </div>
              </div>

              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  个人简介
                </label>
                <textarea
                  rows={4}
                  className="input resize-none dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                  value={profileForm.bio}
                  onChange={(e) => setProfileForm(prev => ({ ...prev, bio: e.target.value }))}
                  disabled={!isEditingProfile || updateProfileMutation.isPending}
                  placeholder="请输入个人简介"
                />
              </div>

              {isEditingProfile && (
                <div className="flex items-center justify-end space-x-3">
                  <button
                    type="button"
                    onClick={() => setIsEditingProfile(false)}
                    className="btn btn-secondary transition-all hover:scale-105"
                    disabled={updateProfileMutation.isPending}
                  >
                    取消
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary transition-all hover:scale-105"
                    disabled={updateProfileMutation.isPending}
                  >
                    {updateProfileMutation.isPending ? '保存中...' : '保存更改'}
                  </button>
                </div>
              )}
            </form>
          </div>

          {/* Security Settings */}
          <div className="card dark:bg-gray-900 dark:border-gray-800 fade-in">
            <div className="flex items-center justify-between mb-6">
              <div className="flex items-center space-x-2">
                <ShieldCheckIcon className="h-6 w-6 text-gray-500 dark:text-gray-400" />
                <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">安全设置</h2>
              </div>
              <button
                onClick={() => setIsChangingPassword(!isChangingPassword)}
                className="btn btn-secondary transition-all hover:scale-105 inline-flex items-center space-x-1"
                disabled={changePasswordMutation.isPending}
              >
                <KeyIcon className="h-4 w-4 mr-2" />
                {isChangingPassword ? '取消' : '更改密码'}
              </button>
            </div>

            {!isChangingPassword && (
              <div className="text-center py-6 border-t border-gray-200 dark:border-gray-700">
                <KeyIcon className="h-8 w-8 text-gray-400 mx-auto mb-2" />
                <p className="text-gray-600 dark:text-gray-400 text-sm">
                  点击"更改密码"按钮来更新您的账户密码
                </p>
                <p className="text-gray-500 dark:text-gray-500 text-xs mt-1">
                  建议定期更换密码以保障账户安全
                </p>
              </div>
            )}

            {isChangingPassword && (
              <form onSubmit={handlePasswordSubmit} className="space-y-4 animate-fade-in">
                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    当前密码
                  </label>
                  <div className="relative">
                    <input
                      type={showCurrentPassword ? 'text' : 'password'}
                      className="input pr-10 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                      value={passwordForm.currentPassword}
                      onChange={(e) => setPasswordForm(prev => ({ ...prev, currentPassword: e.target.value }))}
                      disabled={changePasswordMutation.isPending}
                      required
                    />
                    <button
                      type="button"
                      className="absolute inset-y-0 right-0 pr-3 flex items-center"
                      onClick={() => setShowCurrentPassword(!showCurrentPassword)}
                    >
                      {showCurrentPassword ? (
                        <EyeSlashIcon className="h-5 w-5 text-gray-400" />
                      ) : (
                        <EyeIcon className="h-5 w-5 text-gray-400" />
                      )}
                    </button>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    新密码
                  </label>
                  <div className="relative">
                    <input
                      type={showNewPassword ? 'text' : 'password'}
                      className="input pr-10 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                      value={passwordForm.newPassword}
                      onChange={(e) => setPasswordForm(prev => ({ ...prev, newPassword: e.target.value }))}
                      disabled={changePasswordMutation.isPending}
                      required
                    />
                    <button
                      type="button"
                      className="absolute inset-y-0 right-0 pr-3 flex items-center"
                      onClick={() => setShowNewPassword(!showNewPassword)}
                    >
                      {showNewPassword ? (
                        <EyeSlashIcon className="h-5 w-5 text-gray-400" />
                      ) : (
                        <EyeIcon className="h-5 w-5 text-gray-400" />
                      )}
                    </button>
                  </div>
                </div>

                <div>
                  <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                    确认新密码
                  </label>
                  <div className="relative">
                    <input
                      type={showConfirmPassword ? 'text' : 'password'}
                      className="input pr-10 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                      value={passwordForm.confirmPassword}
                      onChange={(e) => setPasswordForm(prev => ({ ...prev, confirmPassword: e.target.value }))}
                      disabled={changePasswordMutation.isPending}
                      required
                    />
                    <button
                      type="button"
                      className="absolute inset-y-0 right-0 pr-3 flex items-center"
                      onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                    >
                      {showConfirmPassword ? (
                        <EyeSlashIcon className="h-5 w-5 text-gray-400" />
                      ) : (
                        <EyeIcon className="h-5 w-5 text-gray-400" />
                      )}
                    </button>
                  </div>
                </div>

                <div className="flex items-center justify-end space-x-3">
                  <button
                    type="button"
                    onClick={() => setIsChangingPassword(false)}
                    className="btn btn-secondary transition-all hover:scale-105"
                    disabled={changePasswordMutation.isPending}
                  >
                    取消
                  </button>
                  <button
                    type="submit"
                    className="btn btn-primary transition-all hover:scale-105"
                    disabled={changePasswordMutation.isPending}
                  >
                    {changePasswordMutation.isPending ? '更新中...' : '更新密码'}
                  </button>
                </div>
              </form>
            )}
          </div>

          {/* Notification Preferences */}
          <div className="card dark:bg-gray-900 dark:border-gray-800 fade-in">
            <div className="flex items-center space-x-2 mb-6">
              <BellIcon className="h-6 w-6 text-gray-500 dark:text-gray-400" />
              <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">通知偏好</h2>
            </div>

            <div className="space-y-4">
              {[
                { key: 'emailNotifications', label: '邮件通知', description: '接收重要更新的邮件通知' },
                { key: 'pushNotifications', label: '推送通知', description: '接收浏览器推送通知' },
                { key: 'reviewReminders', label: '评审提醒', description: '当有待处理的代码评审时提醒我' },
                { key: 'weeklyDigest', label: '周报摘要', description: '每周发送项目活动摘要' }
              ].map(({ key, label, description }) => (
                <div key={key} className="flex items-center justify-between p-4 bg-gray-50 dark:bg-gray-800 rounded-lg transition-colors">
                  <div>
                    <div className="text-sm font-medium text-gray-900 dark:text-gray-100">{label}</div>
                    <div className="text-xs text-gray-500 dark:text-gray-400">{description}</div>
                  </div>
                  <label className="relative inline-flex items-center cursor-pointer">
                    <input
                      type="checkbox"
                      className="sr-only peer"
                      checked={profileForm.preferences?.[key as keyof typeof profileForm.preferences] || false}
                      onChange={(e) => setProfileForm(prev => ({
                        ...prev,
                        preferences: {
                          emailNotifications: true,
                          pushNotifications: true,
                          reviewReminders: true,
                          weeklyDigest: false,
                          ...prev.preferences,
                          [key]: e.target.checked
                        } as UserProfile['preferences']
                      }))}
                    />
                    <div className="w-11 h-6 bg-gray-200 peer-focus:outline-none peer-focus:ring-4 peer-focus:ring-primary-300 dark:peer-focus:ring-primary-800 rounded-full peer dark:bg-gray-700 peer-checked:after:translate-x-full peer-checked:after:border-white after:content-[''] after:absolute after:top-[2px] after:left-[2px] after:bg-white after:border-gray-300 after:border after:rounded-full after:h-5 after:w-5 after:transition-all dark:border-gray-600 peer-checked:bg-primary-600"></div>
                  </label>
                </div>
              ))}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};