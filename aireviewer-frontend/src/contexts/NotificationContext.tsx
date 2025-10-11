import { createContext, useContext, useEffect, useState } from 'react';
import type { ReactNode } from 'react';
import { signalRService, type NotificationData } from '../services/signalr.service';
import { useAuth } from './AuthContext';

interface NotificationContextType {
  notifications: NotificationData[];
  isConnected: boolean;
  connectionState: string;
  addNotification: (notification: NotificationData) => void;
  removeNotification: (index: number) => void;
  clearAllNotifications: () => void;
  joinGroup: (groupName: string) => Promise<void>;
  leaveGroup: (groupName: string) => Promise<void>;
}

const NotificationContext = createContext<NotificationContextType | undefined>(undefined);

export { NotificationContext };

interface NotificationProviderProps {
  children: ReactNode;
}

export function NotificationProvider({ children }: NotificationProviderProps) {
  const { user, isAuthenticated } = useAuth();
  const [notifications, setNotifications] = useState<NotificationData[]>([]);
  const [isConnected, setIsConnected] = useState(false);
  const [connectionState, setConnectionState] = useState('Disconnected');

  useEffect(() => {
    if (isAuthenticated && user) {
      // Start SignalR connection when user is authenticated
      signalRService.start().then(() => {
        setIsConnected(signalRService.isConnected);
        setConnectionState(signalRService.connectionState);
      });

      // Subscribe to all notifications
      const unsubscribe = signalRService.subscribe('*', (notification) => {
        addNotification(notification);
      });

      // Monitor connection state
      const checkConnection = setInterval(() => {
        setIsConnected(signalRService.isConnected);
        setConnectionState(signalRService.connectionState);
      }, 1000);

      return () => {
        unsubscribe();
        clearInterval(checkConnection);
      };
    } else {
      // Stop SignalR connection when user is not authenticated
      signalRService.stop();
      setNotifications([]);
      setIsConnected(false);
      setConnectionState('Disconnected');
    }
  }, [isAuthenticated, user]);

  const addNotification = (notification: NotificationData) => {
    setNotifications(prev => [notification, ...prev].slice(0, 50)); // Keep last 50 notifications
  };

  const removeNotification = (index: number) => {
    setNotifications(prev => prev.filter((_, i) => i !== index));
  };

  const clearAllNotifications = () => {
    setNotifications([]);
  };

  const joinGroup = async (groupName: string) => {
    await signalRService.joinGroup(groupName);
  };

  const leaveGroup = async (groupName: string) => {
    await signalRService.leaveGroup(groupName);
  };

  const value: NotificationContextType = {
    notifications,
    isConnected,
    connectionState,
    addNotification,
    removeNotification,
    clearAllNotifications,
    joinGroup,
    leaveGroup,
  };

  return (
    <NotificationContext.Provider value={value}>
      {children}
    </NotificationContext.Provider>
  );
}

export function useNotifications() {
  const context = useContext(NotificationContext);
  if (!context) {
    throw new Error('useNotifications must be used within a NotificationProvider');
  }
  return context;
}