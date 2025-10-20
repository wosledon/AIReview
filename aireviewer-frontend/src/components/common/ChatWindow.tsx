import React, { useState, useEffect, useRef, useCallback } from 'react';
import { XMarkIcon, PaperAirplaneIcon, ChatBubbleLeftRightIcon, ClipboardDocumentIcon, CheckIcon, ChevronUpIcon, UserIcon } from '@heroicons/react/24/outline';
import ReactMarkdown from 'react-markdown';
import { chatService } from '../../services/chat.service';
import type { ChatMessage, ChatModel } from '../../services/chat.service';

interface ChatWindowProps {
  isOpen: boolean;
  onClose: () => void;
}

const ChatWindow: React.FC<ChatWindowProps> = ({ isOpen, onClose }) => {
  const [messages, setMessages] = useState<ChatMessage[]>([]);
  const [inputMessage, setInputMessage] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [models, setModels] = useState<ChatModel[]>([]);
  const [selectedModelId, setSelectedModelId] = useState<number | null>(null);
  const [isMinimized, setIsMinimized] = useState(false);
  const messagesEndRef = useRef<HTMLDivElement>(null);
  const inputRef = useRef<HTMLInputElement>(null);

  // 代码块组件
  const CodeBlock: React.FC<{ children: React.ReactNode; className?: string }> = ({ children, className }) => {
    const [copied, setCopied] = useState(false);
    const codeRef = useRef<HTMLPreElement>(null);

    const copyToClipboard = async () => {
      if (codeRef.current) {
        const text = codeRef.current.textContent || '';
        try {
          await navigator.clipboard.writeText(text);
          setCopied(true);
          setTimeout(() => setCopied(false), 2000);
        } catch (err) {
          console.error('Failed to copy text: ', err);
        }
      }
    };

    return (
      <div className="relative group">
        <pre
          ref={codeRef}
          className={`${className} bg-slate-900 text-slate-100 p-4 rounded-xl text-sm font-mono overflow-x-auto mb-3 border border-slate-700 shadow-lg`}
        >
          {children}
        </pre>
        <button
          onClick={copyToClipboard}
          className="absolute top-3 right-3 p-2 bg-slate-800 hover:bg-slate-700 text-slate-300 hover:text-white rounded-lg opacity-0 group-hover:opacity-100 transition-all duration-200 shadow-md hover:shadow-lg"
          title="复制代码"
        >
          {copied ? (
            <CheckIcon className="w-4 h-4 text-green-400" />
          ) : (
            <ClipboardDocumentIcon className="w-4 h-4" />
          )}
        </button>
      </div>
    );
  };

  const initializeChat = useCallback(async () => {
    try {
      // 启动SignalR连接
      await chatService.start();
      await chatService.joinChat();

      // 获取可用模型
      const availableModels = await chatService.getAvailableModels();
      setModels(availableModels);

      // 设置默认模型
      const defaultModel = availableModels.find(m => m.isDefault) || availableModels[0];
      if (defaultModel) {
        setSelectedModelId(defaultModel.id);
      }

      // 设置消息处理器
      const unsubscribeMessage = chatService.onMessage(handleNewMessage);
      const unsubscribeError = chatService.onError(handleError);

      return () => {
        unsubscribeMessage();
        unsubscribeError();
      };
    } catch (error) {
      console.error('Failed to initialize chat:', error);
    }
  }, []);

  // 初始化聊天服务
  useEffect(() => {
    if (isOpen) {
      initializeChat();
    } else {
      chatService.stop();
      setIsLoading(false); // 关闭窗口时重置loading状态
    }

    return () => {
      chatService.stop();
      setIsLoading(false); // 组件卸载时重置loading状态
    };
  }, [isOpen, initializeChat]);

  // 滚动到底部
  useEffect(() => {
    scrollToBottom();
  }, [messages]);

  const handleNewMessage = (message: ChatMessage) => {
    console.log('Received message:', message); // 调试信息
    setMessages(prev => [...prev, message]);
    // 如果收到机器人回复，重置loading状态
    // if (message.type === 'Bot') {
    //   console.log('Resetting loading state for Bot message'); // 调试信息
    //   setIsLoading(false);
    // } else if (message.type === 'System') {
    //   // 系统消息也可能表示回复完成
    //   console.log('Resetting loading state for System message'); // 调试信息
    //   setIsLoading(false);
    // } else {
    //   setIsLoading(false);
    // }
    // // 额外保险：延迟重置loading状态，避免卡在loading状态
    // setTimeout(() => {
    //   console.log('Timeout resetting loading state'); // 调试信息
    //   setIsLoading(false);
    // }, 1000);
    setIsLoading(false);
  };

  const handleError = (error: string) => {
    console.error('Chat error:', error);
    setIsLoading(false); // 发生错误时也要重置loading状态
    // 可以显示错误提示
  };

  const scrollToBottom = () => {
    messagesEndRef.current?.scrollIntoView({ behavior: 'smooth' });
  };

  const handleSendMessage = async () => {
    if (!inputMessage.trim() || !selectedModelId || isLoading) return;

    const message = inputMessage.trim();
    setInputMessage('');
    setIsLoading(true);

    try {
      await chatService.sendMessage(message, selectedModelId);
    } catch (error) {
      console.error('Failed to send message:', error);
      setIsLoading(false);
    }
  };

  const handleKeyPress = (e: React.KeyboardEvent) => {
    if (e.key === 'Enter' && !e.shiftKey) {
      e.preventDefault();
      handleSendMessage();
    }
  };

  const formatTime = (timestamp: string) => {
    return new Date(timestamp).toLocaleTimeString('zh-CN', {
      hour: '2-digit',
      minute: '2-digit'
    });
  };

  if (!isOpen) return null;

  return (
    <div className="fixed bottom-6 right-6 z-50">
      <div className={`bg-white rounded-2xl shadow-2xl border border-gray-200 transition-all duration-300 overflow-hidden ${
        isMinimized ? 'w-80 h-16' : 'w-[400px] h-[600px]'
      }`}>
        {/* 头部 */}
        <div className="flex items-center justify-between p-3 border-b border-gray-100 bg-gradient-to-r from-blue-500 to-blue-600 rounded-t-2xl">
          <div className="flex items-center space-x-3">
            <div className="w-8 h-8 bg-white bg-opacity-20 rounded-full flex items-center justify-center">
              <ChatBubbleLeftRightIcon className="w-4 h-4 text-white" />
            </div>
            <div>
              <span className="font-semibold text-white text-sm">评审专家</span>
              <div className="text-xs text-blue-100">AI 代码评审助手</div>
            </div>
          </div>
          <div className="flex items-center space-x-1">
            <button
              onClick={() => setIsMinimized(!isMinimized)}
              className="p-1.5 hover:bg-white hover:bg-opacity-20 rounded-lg transition-colors"
            >
              {isMinimized ? (
                <ChevronUpIcon className="w-4 h-4 text-white" />
              ) : (
                <div className="w-3 h-0.5 bg-white"></div>
              )}
            </button>
            <button
              onClick={onClose}
              className="p-1.5 hover:bg-white hover:bg-opacity-20 rounded-lg transition-colors"
            >
              <XMarkIcon className="w-4 h-4 text-white" />
            </button>
          </div>
        </div>

        {!isMinimized && (
          <div className="flex flex-col h-[calc(100%-5rem)]">
            {/* 模型选择 */}
            <div className="p-4 border-b border-gray-100 bg-gray-50 flex-shrink-0">
              <div className="flex items-center space-x-2">
                <label className="text-xs font-medium text-gray-600">AI 模型:</label>
                <select
                  value={selectedModelId || ''}
                  onChange={(e) => setSelectedModelId(Number(e.target.value))}
                  className="flex-1 px-3 py-2 bg-white border border-gray-200 rounded-lg text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent"
                >
                  {models.map(model => (
                    <option key={model.id} value={model.id}>
                      {model.name} ({model.provider})
                    </option>
                  ))}
                </select>
              </div>
            </div>

            {/* 消息区域 */}
            <div className="flex-1 overflow-y-auto p-4 space-y-4 bg-gray-50 min-h-0">
              {messages.length === 0 ? (
                <div className="text-center text-gray-500 py-12">
                  <ChatBubbleLeftRightIcon className="w-12 h-12 mx-auto mb-4 text-gray-300" />
                  <p className="text-base font-medium mb-2">开始与评审专家对话吧！</p>
                  <p className="text-sm text-gray-400">我可以帮您分析代码质量、提供改进建议</p>
                </div>
              ) : (
                messages.map((message) => {                  
                  return (
                  <div key={message.id}>
                    {message.type === 'User' || message.type === 0 ? (
                      // 用户消息：右对齐
                      <div className="flex items-start justify-end gap-3">
                        <div className="max-w-[70%] px-4 py-3 rounded-2xl rounded-br-md shadow-sm bg-blue-600 text-white">
                          <div className="text-sm leading-relaxed whitespace-pre-wrap">
                            {message.content}
                          </div>
                          <div className="text-xs mt-2 text-blue-100">
                            {formatTime(message.timestamp)}
                          </div>
                        </div>
                        <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0">
                          <UserIcon className="w-5 h-5 text-blue-600" />
                        </div>
                      </div>
                    ) : (
                      // 机器人消息：左对齐
                      <div className="flex items-start justify-start gap-3">
                        <div className="w-8 h-8 bg-blue-100 rounded-full flex items-center justify-center flex-shrink-0">
                          <ChatBubbleLeftRightIcon className="w-4 h-4 text-blue-600" />
                        </div>
                        <div className="max-w-[70%] px-4 py-3 rounded-2xl rounded-bl-md shadow-sm bg-white text-gray-900 border border-gray-200">
                          <div className="text-sm leading-relaxed prose prose-sm max-w-none">
                            <ReactMarkdown
                              components={{
                                p: ({children}) => <p className="mb-2 last:mb-0 text-gray-900">{children}</p>,
                                code: ({children, className}) => {
                                  const isInline = !className;
                                  return isInline ? (
                                    <code className="bg-blue-50 text-blue-800 px-2 py-1 rounded-md text-sm font-mono border border-blue-200 shadow-sm">
                                      {children}
                                    </code>
                                  ) : (
                                    <code className={className}>{children}</code>
                                  );
                                },
                                pre: ({children}) => (
                                  <CodeBlock className="bg-gray-900 text-gray-100">
                                    {children}
                                  </CodeBlock>
                                ),
                                ul: ({children}) => <ul className="list-disc list-inside mb-2 space-y-1 text-gray-900">{children}</ul>,
                                ol: ({children}) => <ol className="list-decimal list-inside mb-2 space-y-1 text-gray-900">{children}</ol>,
                                li: ({children}) => <li className="text-gray-900">{children}</li>,
                                strong: ({children}) => <strong className="font-semibold text-gray-900">{children}</strong>,
                                h1: ({children}) => <h1 className="text-lg font-bold text-gray-900 mb-2">{children}</h1>,
                                h2: ({children}) => <h2 className="text-base font-bold text-gray-900 mb-2">{children}</h2>,
                                h3: ({children}) => <h3 className="text-sm font-bold text-gray-900 mb-1">{children}</h3>,
                              }}
                            >
                              {message.content}
                            </ReactMarkdown>
                          </div>
                          <div className="text-xs mt-2 text-gray-500">
                            {formatTime(message.timestamp)}
                          </div>
                        </div>
                      </div>
                    )}
                  </div>
                  );
                })
              )}
              <div ref={messagesEndRef} />
            </div>

            {/* 输入区域 */}
            <div className="px-4 pt-4 pb-3 border-t border-gray-100 bg-white flex-shrink-0">
              <div className="flex space-x-3">
                <input
                  ref={inputRef}
                  type="text"
                  value={inputMessage}
                  onChange={(e) => setInputMessage(e.target.value)}
                  onKeyPress={handleKeyPress}
                  placeholder="询问代码评审问题..."
                  className="flex-1 px-4 py-3 bg-gray-50 border border-gray-200 rounded-xl text-sm focus:outline-none focus:ring-2 focus:ring-blue-500 focus:border-transparent focus:bg-white transition-colors"
                  disabled={isLoading}
                />
                <button
                  onClick={handleSendMessage}
                  disabled={!inputMessage.trim() || !selectedModelId || isLoading}
                  className="px-4 py-3 bg-blue-500 text-white rounded-xl hover:bg-blue-600 disabled:bg-gray-300 disabled:cursor-not-allowed transition-colors shadow-sm hover:shadow-md"
                >
                  {isLoading ? (
                    <div className="w-4 h-4 border-2 border-white border-t-transparent rounded-full animate-spin"></div>
                  ) : (
                    <PaperAirplaneIcon className="w-4 h-4" />
                  )}
                </button>
              </div>
              {isLoading && (
                <div className="flex items-center space-x-2 mt-2 text-xs text-gray-500">
                  <div className="w-3 h-3 border border-gray-300 border-t-gray-500 rounded-full animate-spin"></div>
                  <span>专家正在思考...</span>
                </div>
              )}
            </div>
          </div>
        )}
      </div>
    </div>
  );
};

export default ChatWindow;