import { ExclamationTriangleIcon } from '@heroicons/react/24/outline';
import { useNotifications } from '../hooks/useNotifications';

export function NotificationContainer() {
  // 不再显示弹出的通知提示框，通知直接在铃铛下拉菜单中显示
  return null;
}

export function ConnectionStatus() {
  const { isConnected, connectionState } = useNotifications();

  if (isConnected) {
    return null; // Don't show anything when connected
  }

  return (
    <div className="fixed bottom-4 right-4 z-40">
      <div className="rounded-md bg-yellow-50 dark:bg-yellow-900/20 p-4 shadow-lg border border-yellow-200 dark:border-yellow-800">
        <div className="flex">
          <div className="flex-shrink-0">
            <ExclamationTriangleIcon className="h-5 w-5 text-yellow-400" />
          </div>
          <div className="ml-3">
            <h3 className="text-sm font-medium text-yellow-800 dark:text-yellow-200">
              连接状态: {connectionState}
            </h3>
            <div className="mt-2 text-sm text-yellow-700 dark:text-yellow-300">
              <p>实时通知功能暂时不可用</p>
            </div>
          </div>
        </div>
      </div>
    </div>
  );
}