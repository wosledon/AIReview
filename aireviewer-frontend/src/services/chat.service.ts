import { HubConnection, HubConnectionBuilder, LogLevel } from '@microsoft/signalr';
import { apiClient } from './api-client';

export interface ChatMessage {
  id: string;
  userId: string;
  content: string;
  type: 'User' | 'Bot' | 'System' | 0 | 1 | 2; // 支持字符串和数字类型
  timestamp: string;
  modelId?: number;
}

export interface SendChatMessageRequest {
  content: string;
  modelId: number;
}

export interface ChatModel {
  id: number;
  name: string;
  provider: string;
  model: string;
  isDefault: boolean;
}

export type ChatMessageHandler = (message: ChatMessage) => void;
export type ChatErrorHandler = (error: string) => void;

class ChatService {
  private connection: HubConnection | null = null;
  private messageHandlers: ChatMessageHandler[] = [];
  private errorHandlers: ChatErrorHandler[] = [];
  private reconnectTimer: number | null = null;

  async start(): Promise<void> {
    if (this.connection) {
      return;
    }

    const token = apiClient.getToken();
    if (!token) {
      console.warn('No auth token available for chat SignalR connection');
      return;
    }

    this.connection = new HubConnectionBuilder()
      .withUrl('http://10.60.33.81:5000/hubs/chat', {
        accessTokenFactory: () => token,
      })
      .withAutomaticReconnect()
      .configureLogging(LogLevel.Information)
      .build();

    this.setupEventHandlers();

    try {
      await this.connection.start();
      console.log('Chat SignalR connected successfully');
      this.clearReconnectTimer();
    } catch (error) {
      console.error('Chat SignalR connection failed:', error);
      this.scheduleReconnect();
    }
  }

  async stop(): Promise<void> {
    if (this.connection) {
      try {
        await this.connection.stop();
      } catch (error) {
        console.error('Error stopping chat SignalR connection:', error);
      }
      this.connection = null;
    }
    this.clearReconnectTimer();
  }

  private setupEventHandlers(): void {
    if (!this.connection) return;

    this.connection.on('ReceiveMessage', (message: ChatMessage) => {
      this.messageHandlers.forEach(handler => handler(message));
    });

    this.connection.on('Error', (error: string) => {
      this.errorHandlers.forEach(handler => handler(error));
    });

    this.connection.on('JoinedChat', (userId: string) => {
      console.log('Joined chat for user:', userId);
    });

    this.connection.on('LeftChat', (userId: string) => {
      console.log('Left chat for user:', userId);
    });

    this.connection.onclose(() => {
      console.log('Chat SignalR connection closed');
      this.scheduleReconnect();
    });

    this.connection.onreconnected(() => {
      console.log('Chat SignalR reconnected');
      this.clearReconnectTimer();
    });
  }

  private scheduleReconnect(): void {
    if (this.reconnectTimer) return;

    this.reconnectTimer = window.setTimeout(async () => {
      console.log('Attempting to reconnect chat SignalR...');
      await this.start();
    }, 5000);
  }

  private clearReconnectTimer(): void {
    if (this.reconnectTimer) {
      clearTimeout(this.reconnectTimer);
      this.reconnectTimer = null;
    }
  }

  // 加入聊天
  async joinChat(): Promise<void> {
    if (this.connection?.state === 'Connected') {
      await this.connection.invoke('JoinChat');
    }
  }

  // 离开聊天
  async leaveChat(): Promise<void> {
    if (this.connection?.state === 'Connected') {
      await this.connection.invoke('LeaveChat');
    }
  }

  // 发送消息
  async sendMessage(content: string, modelId: number): Promise<void> {
    if (this.connection?.state === 'Connected') {
      await this.connection.invoke('SendMessage', content, modelId);
    } else {
      throw new Error('Chat connection not available');
    }
  }

  // API调用：获取聊天历史
  async getChatHistory(modelId?: number, page = 1, pageSize = 50): Promise<ChatMessage[]> {
    const params = new URLSearchParams();
    if (modelId) params.append('modelId', modelId.toString());
    params.append('page', page.toString());
    params.append('pageSize', pageSize.toString());

    return await apiClient.get<ChatMessage[]>(`/chat/history?${params}`);
  }

  // API调用：获取可用模型
  async getAvailableModels(): Promise<ChatModel[]> {
    return await apiClient.get<ChatModel[]>('/chat/models');
  }

  // API调用：清除聊天历史
  async clearChatHistory(modelId?: number): Promise<void> {
    const params = new URLSearchParams();
    if (modelId) params.append('modelId', modelId.toString());

    await apiClient.delete(`/chat/history?${params}`);
  }

  // 事件处理器管理
  onMessage(handler: ChatMessageHandler): () => void {
    this.messageHandlers.push(handler);
    return () => {
      const index = this.messageHandlers.indexOf(handler);
      if (index > -1) {
        this.messageHandlers.splice(index, 1);
      }
    };
  }

  onError(handler: ChatErrorHandler): () => void {
    this.errorHandlers.push(handler);
    return () => {
      const index = this.errorHandlers.indexOf(handler);
      if (index > -1) {
        this.errorHandlers.splice(index, 1);
      }
    };
  }

  // 获取连接状态
  get isConnected(): boolean {
    return this.connection?.state === 'Connected';
  }
}

export const chatService = new ChatService();