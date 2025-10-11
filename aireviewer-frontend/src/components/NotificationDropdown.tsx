import React, { Fragment, useState, useEffect } from 'react';
import { Menu, Transition } from '@headlessui/react';
import { Link } from 'react-router-dom';
import { 
  BellIcon, 
  TrashIcon,
  EyeIcon,
  ClockIcon
} from '@heroicons/react/24/outline';
import { useNotifications } from '../contexts/NotificationContext';
import type { NotificationData } from '../services/signalr.service';

interface NotificationDropdownProps {
  className?: string;
}

export const NotificationDropdown: React.FC<NotificationDropdownProps> = ({ className = '' }) => {
  const { notifications, addNotification, removeNotification, clearAllNotifications } = useNotifications();
  const [unreadIds, setUnreadIds] = useState<Set<string>>(new Set());
  const [lastNotificationCount, setLastNotificationCount] = useState(0);

  // 计算未读通知数量
  const unreadCount = notifications.filter((_, index: number) => unreadIds.has(index.toString())).length;

  // 标记通知为已读
  const markAsRead = (index: number) => {
    setUnreadIds(prev => {
      const newSet = new Set(prev);
      newSet.delete(index.toString());
      return newSet;
    });
  };

  // 标记所有通知为已读
  const markAllAsRead = () => {
    setUnreadIds(new Set());
  };

  // 删除单个通知
  const handleRemoveNotification = (index: number) => {
    removeNotification(index);
    setUnreadIds(prev => {
      const newSet = new Set(prev);
      newSet.delete(index.toString());
      return newSet;
    });
  };

  // 清空所有通知
  const handleClearAll = () => {
    clearAllNotifications();
    setUnreadIds(new Set());
  };

  // 获取通知图标
  const getNotificationIcon = (type: string) => {
    switch (type) {
      case 'review_comment':
        return '💬';
      case 'review_request':
        return '📝';
      case 'review_approved':
        return '✅';
      case 'review_rejected':
        return '❌';
      case 'project_update':
        return '📦';
      case 'security_update':
        return '🔒';
      case 'profile_update':
        return '👤';
      default:
        return '🔔';
    }
  };

  // 格式化时间
  const formatTime = (timestamp: string) => {
    try {
      const date = new Date(timestamp);
      const now = new Date();
      const diffInMinutes = Math.floor((now.getTime() - date.getTime()) / (1000 * 60));
      
      if (diffInMinutes < 1) return '刚刚';
      if (diffInMinutes < 60) return `${diffInMinutes} 分钟前`;
      
      const diffInHours = Math.floor(diffInMinutes / 60);
      if (diffInHours < 24) return `${diffInHours} 小时前`;
      
      const diffInDays = Math.floor(diffInHours / 24);
      if (diffInDays < 30) return `${diffInDays} 天前`;
      
      return date.toLocaleDateString('zh-CN');
    } catch {
      return '刚刚';
    }
  };

  // 自动标记新通知为未读
  useEffect(() => {
    // 当通知数量增加时，标记新增的通知为未读
    if (notifications.length > lastNotificationCount) {
      setUnreadIds(prev => {
        const newSet = new Set(prev);
        // 标记新增的通知为未读（新通知在数组开头）
        for (let i = 0; i < notifications.length - lastNotificationCount; i++) {
          newSet.add(i.toString());
        }
        return newSet;
      });
    }
    setLastNotificationCount(notifications.length);
  }, [notifications.length, lastNotificationCount]);

  return (
    <Menu as="div" className={`relative ${className}`}>
      <Menu.Button className="relative p-2 text-gray-400 hover:text-gray-500 dark:text-gray-300 dark:hover:text-gray-100 rounded-full hover:bg-gray-100 dark:hover:bg-gray-800 transition-all">
        <BellIcon className="h-6 w-6" />
        {unreadCount > 0 && (
          <span className="absolute -top-1 -right-1 h-5 w-5 rounded-full bg-red-500 flex items-center justify-center text-xs font-bold text-white animate-pulse">
            {unreadCount > 99 ? '99+' : unreadCount}
          </span>
        )}
      </Menu.Button>

      <Transition
        as={Fragment}
        enter="transition ease-out duration-100"
        enterFrom="transform opacity-0 scale-95"
        enterTo="transform opacity-100 scale-100"
        leave="transition ease-in duration-75"
        leaveFrom="transform opacity-100 scale-100"
        leaveTo="transform opacity-0 scale-95"
      >
        <Menu.Items className="absolute right-0 mt-2 w-96 bg-white dark:bg-gray-900 rounded-md shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none z-50 border border-gray-100 dark:border-gray-800 max-h-96 overflow-hidden">
          {/* Header */}
          <div className="px-4 py-3 border-b border-gray-100 dark:border-gray-700">
            <div className="flex items-center justify-between">
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">通知</h3>
              <div className="flex items-center space-x-2">
                {unreadCount > 0 && (
                  <button
                    onClick={markAllAsRead}
                    className="text-xs text-primary-600 dark:text-primary-400 hover:text-primary-700 dark:hover:text-primary-300 transition-colors"
                  >
                    全部已读
                  </button>
                )}
                {notifications.length > 0 && (
                  <button
                    onClick={handleClearAll}
                    className="text-xs text-red-600 dark:text-red-400 hover:text-red-700 dark:hover:text-red-300 transition-colors"
                  >
                    清空
                  </button>
                )}
                <button
                  onClick={() => {
                    // 添加测试通知
                    const testNotification = {
                      type: 'review_comment',
                      message: '这是一个测试通知',
                      timestamp: new Date().toISOString(),
                      content: '测试通知内容，用于验证通知系统是否正常工作',
                      authorName: '系统测试'
                    };
                    addNotification(testNotification);
                  }}
                  className="text-xs text-green-600 dark:text-green-400 hover:text-green-700 dark:hover:text-green-300 transition-colors"
                >
                  测试
                </button>
              </div>
            </div>
            {unreadCount > 0 && (
              <p className="text-sm text-gray-500 dark:text-gray-400 mt-1">
                {unreadCount} 条未读通知
              </p>
            )}
          </div>

          {/* Notifications List */}
          <div className="max-h-80 overflow-y-auto">
            {notifications.length === 0 ? (
              <div className="px-4 py-8 text-center">
                <BellIcon className="h-12 w-12 text-gray-300 dark:text-gray-600 mx-auto mb-3" />
                <p className="text-gray-500 dark:text-gray-400 text-sm">暂无通知</p>
              </div>
            ) : (
              notifications.map((notification: NotificationData, index: number) => {
                const isUnread = unreadIds.has(index.toString());
                return (
                  <div
                    key={`${notification.timestamp}-${index}`}
                    className={`px-4 py-3 border-b border-gray-50 dark:border-gray-800 hover:bg-gray-50 dark:hover:bg-gray-800 transition-colors ${
                      isUnread ? 'bg-blue-50 dark:bg-blue-900/10' : ''
                    }`}
                  >
                    <div className="flex items-start space-x-3">
                      {/* Icon */}
                      <div className="flex-shrink-0 mt-1">
                        <span className="text-lg">{getNotificationIcon(notification.type)}</span>
                        {isUnread && (
                          <div className="absolute -mt-1 -ml-1 h-2 w-2 bg-blue-500 rounded-full"></div>
                        )}
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
                              <p className="text-xs text-gray-500 dark:text-gray-400 mt-1 line-clamp-2">
                                {notification.content}
                              </p>
                            )}
                            <div className="flex items-center mt-2 text-xs text-gray-400 dark:text-gray-500">
                              <ClockIcon className="h-3 w-3 mr-1" />
                              {formatTime(notification.timestamp)}
                              {notification.authorName && (
                                <>
                                  <span className="mx-1">•</span>
                                  <span>{notification.authorName}</span>
                                </>
                              )}
                            </div>
                          </div>

                          {/* Actions */}
                          <div className="flex items-center space-x-1 ml-2">
                            {isUnread && (
                              <button
                                onClick={() => markAsRead(index)}
                                className="p-1 text-gray-400 hover:text-blue-600 dark:hover:text-blue-400 rounded transition-colors"
                                title="标记为已读"
                              >
                                <EyeIcon className="h-4 w-4" />
                              </button>
                            )}
                            <button
                              onClick={() => handleRemoveNotification(index)}
                              className="p-1 text-gray-400 hover:text-red-600 dark:hover:text-red-400 rounded transition-colors"
                              title="删除通知"
                            >
                              <TrashIcon className="h-4 w-4" />
                            </button>
                          </div>
                        </div>
                      </div>
                    </div>
                  </div>
                );
              })
            )}
          </div>

          {/* Footer */}
          {notifications.length > 0 && (
            <div className="px-4 py-3 border-t border-gray-100 dark:border-gray-700 bg-gray-50 dark:bg-gray-800">
              <Link 
                to="/notifications"
                className="block w-full text-sm text-primary-600 dark:text-primary-400 hover:text-primary-700 dark:hover:text-primary-300 transition-colors text-center"
              >
                查看所有通知
              </Link>
            </div>
          )}
        </Menu.Items>
      </Transition>
    </Menu>
  );
};