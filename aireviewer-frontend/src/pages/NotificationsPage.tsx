import React, { useState } from 'react';
import { 
  BellIcon, 
  TrashIcon,
  EyeIcon,
  EyeSlashIcon,
  FunnelIcon,
  MagnifyingGlassIcon
} from '@heroicons/react/24/outline';
import { useNotifications } from '../hooks/useNotifications';
import { useTranslation } from 'react-i18next';

type NotificationFilter = 'all' | 'unread' | 'read' | 'review' | 'project' | 'security';

export const NotificationsPage: React.FC = () => {
  const { t } = useTranslation();
  const { notifications, removeNotification, clearAllNotifications } = useNotifications();
  const [unreadIds, setUnreadIds] = useState<Set<string>>(new Set());
  const [selectedFilter, setSelectedFilter] = useState<NotificationFilter>('all');
  const [searchQuery, setSearchQuery] = useState('');
  const [selectedNotifications, setSelectedNotifications] = useState<Set<string>>(new Set());

  // 计算未读通知数量
  const unreadCount = notifications.filter((_, index) => unreadIds.has(index.toString())).length;

  // 标记通知为已读/未读
  const toggleReadStatus = (index: number) => {
    setUnreadIds(prev => {
      const newSet = new Set(prev);
      const id = index.toString();
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  };

  // 标记所有通知为已读
  const markAllAsRead = () => {
    setUnreadIds(new Set());
  };

  // 标记所有通知为未读
//   const markAllAsUnread = () => {
//     const allIds = new Set(notifications.map((_, index) => index.toString()));
//     setUnreadIds(allIds);
//   };

  // 选择/取消选择通知
  const toggleSelection = (index: number) => {
    setSelectedNotifications(prev => {
      const newSet = new Set(prev);
      const id = index.toString();
      if (newSet.has(id)) {
        newSet.delete(id);
      } else {
        newSet.add(id);
      }
      return newSet;
    });
  };

  // 全选/取消全选
  const toggleSelectAll = () => {
    if (selectedNotifications.size === filteredNotifications.length) {
      setSelectedNotifications(new Set());
    } else {
      const allIds = new Set(filteredNotifications.map((_, index) => index.toString()));
      setSelectedNotifications(allIds);
    }
  };

  // 批量删除
  const deleteSelected = () => {
    const indices = Array.from(selectedNotifications).map(id => parseInt(id)).sort((a, b) => b - a);
    indices.forEach(index => {
      removeNotification(index);
    });
    setSelectedNotifications(new Set());
  };

  // 获取通知图标和颜色
  const getNotificationStyle = (type: string) => {
    switch (type) {
      case 'review_comment':
        return { icon: '💬', bgColor: 'bg-blue-100 dark:bg-blue-900/20', textColor: 'text-blue-800 dark:text-blue-300' };
      case 'review_request':
        return { icon: '📝', bgColor: 'bg-green-100 dark:bg-green-900/20', textColor: 'text-green-800 dark:text-green-300' };
      case 'review_approved':
        return { icon: '✅', bgColor: 'bg-emerald-100 dark:bg-emerald-900/20', textColor: 'text-emerald-800 dark:text-emerald-300' };
      case 'review_rejected':
        return { icon: '❌', bgColor: 'bg-red-100 dark:bg-red-900/20', textColor: 'text-red-800 dark:text-red-300' };
      case 'project_update':
        return { icon: '📦', bgColor: 'bg-purple-100 dark:bg-purple-900/20', textColor: 'text-purple-800 dark:text-purple-300' };
      case 'security_update':
        return { icon: '🔒', bgColor: 'bg-orange-100 dark:bg-orange-900/20', textColor: 'text-orange-800 dark:text-orange-300' };
      case 'profile_update':
        return { icon: '👤', bgColor: 'bg-gray-100 dark:bg-gray-900/20', textColor: 'text-gray-800 dark:text-gray-300' };
      default:
        return { icon: '🔔', bgColor: 'bg-gray-100 dark:bg-gray-900/20', textColor: 'text-gray-800 dark:text-gray-300' };
    }
  };

  // 格式化时间
  const formatTime = (timestamp: string) => {
    try {
      const date = new Date(timestamp);
      return date.toLocaleString('zh-CN', {
        year: 'numeric',
        month: 'short',
        day: 'numeric',
        hour: '2-digit',
        minute: '2-digit'
      });
    } catch {
      return t('notificationsPage.time.unknown');
    }
  };

  // 过滤通知
  const filteredNotifications = notifications.filter((notification, index) => {
    // 搜索过滤
    if (searchQuery) {
      const searchLower = searchQuery.toLowerCase();
      const matchesSearch = 
        notification.message.toLowerCase().includes(searchLower) ||
        notification.content?.toLowerCase().includes(searchLower) ||
        notification.authorName?.toLowerCase().includes(searchLower);
      if (!matchesSearch) return false;
    }

    // 状态过滤
    const isUnread = unreadIds.has(index.toString());
    switch (selectedFilter) {
      case 'unread':
        return isUnread;
      case 'read':
        return !isUnread;
      case 'review':
        return notification.type.includes('review');
      case 'project':
        return notification.type.includes('project');
      case 'security':
        return notification.type.includes('security') || notification.type.includes('profile');
      case 'all':
      default:
        return true;
    }
  });

  const filterOptions = [
    { key: 'all', label: t('notificationsPage.filters.all'), count: notifications.length },
    { key: 'unread', label: t('notificationsPage.filters.unread'), count: unreadCount },
    { key: 'read', label: t('notificationsPage.filters.read'), count: notifications.length - unreadCount },
    { key: 'review', label: t('notificationsPage.filters.review'), count: notifications.filter(n => n.type.includes('review')).length },
    { key: 'project', label: t('notificationsPage.filters.project'), count: notifications.filter(n => n.type.includes('project')).length },
    { key: 'security', label: t('notificationsPage.filters.security'), count: notifications.filter(n => n.type.includes('security') || n.type.includes('profile')).length }
  ];

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 transition-colors">
      <div className="max-w-6xl mx-auto py-8 px-4 sm:px-6 lg:px-8">
        {/* Header */}
        <div className="mb-8 fade-in">
          <div className="flex items-center justify-between">
            <div>
              <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 flex items-center">
                <BellIcon className="h-8 w-8 mr-3 text-primary-600" />
                {t('notificationsPage.title')}
              </h1>
              <p className="mt-2 text-gray-600 dark:text-gray-400">
                {t('notificationsPage.subtitle')}
              </p>
            </div>
            
            {notifications.length > 0 && (
              <div className="flex items-center space-x-3">
                <button
                  onClick={markAllAsRead}
                  className="btn btn-secondary transition-all hover:scale-105 inline-flex items-center space-x-1"
                >
                  <EyeIcon className="h-4 w-4 mr-2" />
                  {t('notificationsPage.markAllRead')}
                </button>
                <button
                  onClick={clearAllNotifications}
                  className="btn btn-danger transition-all hover:scale-105 inline-flex items-center space-x-1"
                >
                  <TrashIcon className="h-4 w-4 mr-2" />
                  {t('notificationsPage.clearAll')}
                </button>
              </div>
            )}
          </div>
        </div>

        <div className="grid grid-cols-1 lg:grid-cols-4 gap-8">
          {/* Sidebar Filters */}
          <div className="lg:col-span-1">
            <div className="card dark:bg-gray-900 dark:border-gray-800 fade-in">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4 flex items-center">
                <FunnelIcon className="h-5 w-5 mr-2" />
                {t('notificationsPage.filters.title')}
              </h3>
              
              <div className="space-y-2">
                {filterOptions.map(({ key, label, count }) => (
                  <button
                    key={key}
                    onClick={() => setSelectedFilter(key as NotificationFilter)}
                    className={`w-full text-left px-3 py-2 rounded-lg transition-colors ${
                      selectedFilter === key
                        ? 'bg-primary-100 dark:bg-primary-900/20 text-primary-700 dark:text-primary-300'
                        : 'hover:bg-gray-100 dark:hover:bg-gray-800 text-gray-600 dark:text-gray-400'
                    }`}
                  >
                    <div className="flex items-center justify-between">
                      <span>{label}</span>
                      <span className="text-sm font-medium">{count}</span>
                    </div>
                  </button>
                ))}
              </div>
            </div>
          </div>

          {/* Main Content */}
          <div className="lg:col-span-3">
            {/* Search and Actions */}
            <div className="card dark:bg-gray-900 dark:border-gray-800 mb-6 fade-in">
              <div className="flex flex-col sm:flex-row gap-4">
                {/* Search */}
                <div className="flex-1 relative">
                  <MagnifyingGlassIcon className="absolute left-3 top-1/2 transform -translate-y-1/2 h-5 w-5 text-gray-400" />
                  <input
                    type="text"
                    placeholder={t('notificationsPage.search.placeholder')}
                    value={searchQuery}
                    onChange={(e) => setSearchQuery(e.target.value)}
                    className="input pl-10 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                  />
                </div>

                {/* Batch Actions */}
                {selectedNotifications.size > 0 && (
                  <div className="flex items-center space-x-2">
                    <span className="text-sm text-gray-600 dark:text-gray-400">
                      {t('notificationsPage.actions.selected', { count: selectedNotifications.size })}
                    </span>
                    <button
                      onClick={deleteSelected}
                      className="btn btn-danger btn-sm transition-all hover:scale-105"
                    >
                      <TrashIcon className="h-4 w-4 mr-1" />
                      {t('notificationsPage.actions.deleteSelected')}
                    </button>
                  </div>
                )}
              </div>
            </div>

            {/* Notifications List */}
            <div className="space-y-4">
              {filteredNotifications.length === 0 ? (
                <div className="card dark:bg-gray-900 dark:border-gray-800 text-center py-12 fade-in">
                  <BellIcon className="h-16 w-16 text-gray-300 dark:text-gray-600 mx-auto mb-4" />
                  <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">
                    {searchQuery ? '未找到匹配的通知' : t('notificationsPage.empty.title')}
                  </h3>
                  <p className="text-gray-500 dark:text-gray-400">
                    {searchQuery ? '尝试使用不同的关键词搜索' : t('notificationsPage.empty.description')}
                  </p>
                </div>
              ) : (
                <>
                  {/* Select All */}
                  <div className="card dark:bg-gray-900 dark:border-gray-800 py-3 fade-in">
                    <div className="flex items-center justify-between">
                      <label className="flex items-center space-x-3 cursor-pointer">
                        <input
                          type="checkbox"
                          checked={selectedNotifications.size === filteredNotifications.length && filteredNotifications.length > 0}
                          onChange={toggleSelectAll}
                          className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                        />
                        <span className="text-sm text-gray-600 dark:text-gray-400">
                          选择全部 ({filteredNotifications.length} 项)
                        </span>
                      </label>
                      
                      <div className="flex items-center space-x-2 text-sm text-gray-500 dark:text-gray-400">
                        <span>{t('notifications.showing', { filtered: filteredNotifications.length, total: notifications.length })}</span>
                      </div>
                    </div>
                  </div>

                  {/* Notification Items */}
                  {filteredNotifications.map((notification, originalIndex) => {
                    const isUnread = unreadIds.has(originalIndex.toString());
                    const isSelected = selectedNotifications.has(originalIndex.toString());
                    const style = getNotificationStyle(notification.type);
                    
                    return (
                      <div 
                        key={`${notification.timestamp}-${originalIndex}`}
                        className={`card dark:bg-gray-900 dark:border-gray-800 transition-all hover:shadow-lg ${
                          isUnread ? 'ring-2 ring-blue-200 dark:ring-blue-800' : ''
                        } ${isSelected ? 'ring-2 ring-primary-200 dark:ring-primary-800' : ''} fade-in`}
                        style={{ animationDelay: `${originalIndex * 50}ms` }}
                      >
                        <div className="flex items-start space-x-4">
                          {/* Checkbox */}
                          <div className="flex items-center mt-1">
                            <input
                              type="checkbox"
                              checked={isSelected}
                              onChange={() => toggleSelection(originalIndex)}
                              className="rounded border-gray-300 text-primary-600 focus:ring-primary-500"
                            />
                          </div>

                          {/* Icon and Status */}
                          <div className="flex-shrink-0">
                            <div className={`w-12 h-12 rounded-full ${style.bgColor} flex items-center justify-center relative`}>
                              <span className="text-xl">{style.icon}</span>
                              {isUnread && (
                                <div className="absolute -top-1 -right-1 h-3 w-3 bg-blue-500 rounded-full"></div>
                              )}
                            </div>
                          </div>

                          {/* Content */}
                          <div className="flex-1 min-w-0">
                            <div className="flex items-start justify-between">
                              <div className="flex-1">
                                <p className={`text-sm font-medium ${
                                  isUnread 
                                    ? 'text-gray-900 dark:text-gray-100' 
                                    : 'text-gray-700 dark:text-gray-300'
                                }`}>
                                  {notification.message}
                                </p>
                                {notification.content && (
                                  <p className="text-sm text-gray-600 dark:text-gray-400 mt-1">
                                    {notification.content}
                                  </p>
                                )}
                                <div className="flex items-center mt-3 text-xs text-gray-500 dark:text-gray-400">
                                  <span className={`inline-flex px-2 py-1 rounded-full text-xs font-medium ${style.bgColor} ${style.textColor} mr-3`}>
                                    {notification.type.replace('_', ' ')}
                                  </span>
                                  <span>{formatTime(notification.timestamp)}</span>
                                  {notification.authorName && (
                                    <>
                                      <span className="mx-2">•</span>
                                      <span>{notification.authorName}</span>
                                    </>
                                  )}
                                </div>
                              </div>

                              {/* Actions */}
                              <div className="flex items-center space-x-2 ml-4">
                                <button
                                  onClick={() => toggleReadStatus(originalIndex)}
                                  className={`p-2 rounded-lg transition-colors ${
                                    isUnread
                                      ? 'text-blue-600 hover:bg-blue-100 dark:text-blue-400 dark:hover:bg-blue-900/20'
                                      : 'text-gray-400 hover:bg-gray-100 dark:hover:bg-gray-800'
                                  }`}
                                  title={isUnread ? t('notificationsPage.actions.markAsRead') : t('notificationsPage.actions.markAsUnread')}
                                >
                                  {isUnread ? <EyeIcon className="h-4 w-4" /> : <EyeSlashIcon className="h-4 w-4" />}
                                </button>
                                <button
                                  onClick={() => removeNotification(originalIndex)}
                                  className="p-2 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
                                  title={t('notificationsPage.actions.delete')}
                                >
                                  <TrashIcon className="h-4 w-4" />
                                </button>
                              </div>
                            </div>
                          </div>
                        </div>
                      </div>
                    );
                  })}
                </>
              )}
            </div>
          </div>
        </div>
      </div>
    </div>
  );
};