import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { apiClient } from './api-client';

export interface NotificationData {
  type: string;
  message: string;
  timestamp: string;
  reviewId?: string;
  projectId?: string;
  commentId?: string;
  authorName?: string;
  content?: string;
  status?: string;
}

export type NotificationHandler = (notification: NotificationData) => void;

class SignalRService {
  private connection: HubConnection | null = null;
  private handlers: Map<string, NotificationHandler[]> = new Map();
  private reconnectTimer: number | null = null;

  async start(): Promise<void> {
    if (this.connection) {
      return;
    }

    const token = apiClient.getToken();
    if (!token) {
      console.warn('No auth token available for SignalR connection');
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl('http://10.60.33.81:5000/hubs/notifications', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.setupEventHandlers();

    try {
      await this.connection.start();
      console.log('SignalR connected successfully');
      this.clearReconnectTimer();
    } catch (error) {
      console.error('SignalR connection failed:', error);
      this.scheduleReconnect();
    }
  }

  async stop(): Promise<void> {
    if (this.connection) {
      await this.connection.stop();
      this.connection = null;
    }
    this.clearReconnectTimer();
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    this.connection.onreconnecting((error) => {
      console.warn('SignalR reconnecting...', error);
    });

    this.connection.onreconnected((connectionId) => {
      console.log('SignalR reconnected with ID:', connectionId);
    });

    this.connection.onclose((error) => {
      console.error('SignalR connection closed:', error);
      this.scheduleReconnect();
    });

    // 监听各种类型的通知
    this.connection.on('ReviewStatusUpdate', (notification: NotificationData) => {
      this.notifyHandlers('reviewStatusUpdate', notification);
    });

    this.connection.on('ProjectNotification', (notification: NotificationData) => {
      this.notifyHandlers('projectNotification', notification);
    });

    this.connection.on('ReviewComment', (notification: NotificationData) => {
      this.notifyHandlers('reviewComment', notification);
    });

    this.connection.on('Broadcast', (notification: NotificationData) => {
      this.notifyHandlers('broadcast', notification);
    });
  }

  private notifyHandlers(type: string, notification: NotificationData): void {
    const typeHandlers = this.handlers.get(type) || [];
    const allHandlers = this.handlers.get('*') || [];
    
    [...typeHandlers, ...allHandlers].forEach(handler => {
      try {
        handler(notification);
      } catch (error) {
        console.error('Error in notification handler:', error);
      }
    });
  }

  private scheduleReconnect(): void {
    this.clearReconnectTimer();
    this.reconnectTimer = setTimeout(() => {
      if (!this.connection || this.connection.state === 'Disconnected') {
        console.log('Attempting to reconnect SignalR...');
        this.start();
      }
    }, 5000);
  }

  private clearReconnectTimer(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
  }

  // 订阅通知
  subscribe(type: string, handler: NotificationHandler): () => void {
    if (!this.handlers.has(type)) {
      this.handlers.set(type, []);
    }
    this.handlers.get(type)!.push(handler);

    // 返回取消订阅函数
    return () => {
      const handlers = this.handlers.get(type);
      if (handlers) {
        const index = handlers.indexOf(handler);
        if (index > -1) {
          handlers.splice(index, 1);
        }
      }
    };
  }

  // 加入特定组（如项目组或评审组）
  async joinGroup(groupName: string): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      try {
        await this.connection.invoke('JoinGroup', groupName);
        console.log(`Joined group: ${groupName}`);
      } catch (error) {
        console.error(`Failed to join group ${groupName}:`, error);
      }
    }
  }

  // 离开特定组
  async leaveGroup(groupName: string): Promise<void> {
    if (this.connection && this.connection.state === 'Connected') {
      try {
        await this.connection.invoke('LeaveGroup', groupName);
        console.log(`Left group: ${groupName}`);
      } catch (error) {
        console.error(`Failed to leave group ${groupName}:`, error);
      }
    }
  }

  get isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }

  get connectionState(): string {
    return this.connection?.state || 'Disconnected';
  }
}

export const signalRService = new SignalRService();