import { Fragment } from 'react';
import { Transition } from '@headlessui/react';
import { XMarkIcon, CheckCircleIcon, ExclamationTriangleIcon, InformationCircleIcon } from '@heroicons/react/24/outline';
import { useNotifications } from '../hooks/useNotifications';
import type { NotificationData } from '../services/signalr.service';

function getNotificationIcon(type: string) {
  switch (type) {
    case 'review_status_update':
      return <CheckCircleIcon className="h-6 w-6 text-green-400" />;
    case 'project_notification':
      return <InformationCircleIcon className="h-6 w-6 text-blue-400" />;
    case 'review_comment':
      return <InformationCircleIcon className="h-6 w-6 text-blue-400" />;
    case 'broadcast':
      return <ExclamationTriangleIcon className="h-6 w-6 text-yellow-400" />;
    default:
      return <InformationCircleIcon className="h-6 w-6 text-gray-400" />;
  }
}

function getNotificationTitle(notification: NotificationData): string {
  switch (notification.type) {
    case 'review_status_update':
      return '评审状态更新';
    case 'project_notification':
      return '项目通知';
    case 'review_comment':
      return '新评论';
    case 'broadcast':
      return '系统通知';
    default:
      return '通知';
  }
}

function formatTimestamp(timestamp: string): string {
  const date = new Date(timestamp);
  const now = new Date();
  const diffMs = now.getTime() - date.getTime();
  const diffMinutes = Math.floor(diffMs / (1000 * 60));
  
  if (diffMinutes < 1) return '刚刚';
  if (diffMinutes < 60) return `${diffMinutes}分钟前`;
  
  const diffHours = Math.floor(diffMinutes / 60);
  if (diffHours < 24) return `${diffHours}小时前`;
  
  const diffDays = Math.floor(diffHours / 24);
  if (diffDays < 7) return `${diffDays}天前`;
  
  return date.toLocaleDateString('zh-CN');
}

interface NotificationItemProps {
  notification: NotificationData;
  index: number;
  onRemove: (index: number) => void;
}

function NotificationItem({ notification, index, onRemove }: NotificationItemProps) {
  return (
    <Transition
      show={true}
      as={Fragment}
      enter="transform ease-out duration-300 transition"
      enterFrom="translate-y-2 opacity-0 sm:translate-y-0 sm:translate-x-2"
      enterTo="translate-y-0 opacity-100 sm:translate-x-0"
      leave="transition ease-in duration-100"
      leaveFrom="opacity-100"
      leaveTo="opacity-0"
    >
      <div className="pointer-events-auto w-full max-w-sm overflow-hidden rounded-lg bg-white shadow-lg ring-1 ring-black ring-opacity-5">
        <div className="p-4">
          <div className="flex items-start">
            <div className="flex-shrink-0">
              {getNotificationIcon(notification.type)}
            </div>
            <div className="ml-3 w-0 flex-1 pt-0.5">
              <p className="text-sm font-medium text-gray-900">
                {getNotificationTitle(notification)}
              </p>
              <p className="mt-1 text-sm text-gray-500">
                {notification.message}
              </p>
              <p className="mt-1 text-xs text-gray-400">
                {formatTimestamp(notification.timestamp)}
              </p>
            </div>
            <div className="ml-4 flex flex-shrink-0">
              <button
                type="button"
                className="inline-flex rounded-md bg-white text-gray-400 hover:text-gray-500 focus:outline-none focus:ring-2 focus:ring-indigo-500 focus:ring-offset-2"
                onClick={() => onRemove(index)}
              >
                <span className="sr-only">关闭</span>
                <XMarkIcon className="h-5 w-5" />
              </button>
            </div>
          </div>
        </div>
      </div>
    </Transition>
  );
}

export function NotificationContainer() {
  const { notifications, removeNotification } = useNotifications();

  return (
    <div
      aria-live="assertive"
      className="pointer-events-none fixed inset-0 z-50 flex items-end px-4 py-6 sm:items-start sm:p-6"
    >
      <div className="flex w-full flex-col items-center space-y-4 sm:items-end">
        {notifications.slice(0, 5).map((notification, index) => (
          <NotificationItem
            key={`${notification.timestamp}-${index}`}
            notification={notification}
            index={index}
            onRemove={removeNotification}
          />
        ))}
      </div>
    </div>
  );
}

export function ConnectionStatus() {
  const { isConnected, connectionState } = useNotifications();

  if (isConnected) {
    return null; // Don't show anything when connected
  }

  return (
    <div className="fixed bottom-4 right-4 z-40">
      <div className="rounded-md bg-yellow-50 p-4 shadow-lg">
        <div className="flex">
          <div className="flex-shrink-0">
            <ExclamationTriangleIcon className="h-5 w-5 text-yellow-400" />
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-yellow-800">
              连接状态: {connectionState}
            </h3>
            <div className="mt-2 text-sm text-yellow-700">
              <p>实时通知功能暂时不可用</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}