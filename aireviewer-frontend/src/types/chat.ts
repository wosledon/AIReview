// 聊天相关类型定义

export interface ChatMessage {
  id: string;
  userId: string;
  content: string;
  type: 'User' | 'Bot' | 'System';
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

export interface ChatSession {
  sessionId: string;
  userId: string;
  title: string;
  createdAt: string;
  lastActivityAt: string;
  messages: ChatMessage[];
}

export type ChatMessageType = 'User' | 'Bot' | 'System';