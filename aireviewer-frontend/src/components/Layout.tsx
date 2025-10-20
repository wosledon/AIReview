import React, { useState } from 'react';
import { Outlet } from 'react-router-dom';
import { Navbar } from './Navbar';
import ChatWindow from './common/ChatWindow';
import { ChatBubbleLeftRightIcon } from '@heroicons/react/24/outline';

export const Layout: React.FC = () => {
  const [isChatOpen, setIsChatOpen] = useState(false);

  return (
    <div className="min-h-screen bg-gradient-to-b from-gray-50 to-white dark:from-gray-50 dark:to-white">
      <Navbar />
      <main className="mx-auto max-w-screen-2xl [max-width:var(--max-w)] px-4 sm:px-6 lg:px-8 [padding-left:var(--pad-x)] [padding-right:var(--pad-x)] py-8 fade-in max-container">
        <Outlet />
      </main>

      {/* 聊天按钮 */}
      {!isChatOpen && (
        <button
          onClick={() => setIsChatOpen(true)}
          className="fixed bottom-4 right-4 z-40 bg-blue-500 hover:bg-blue-600 text-white p-3 rounded-full shadow-lg transition-colors duration-200"
          title="与评审专家对话"
        >
          <ChatBubbleLeftRightIcon className="w-6 h-6" />
        </button>
      )}

      {/* 聊天窗口 */}
      <ChatWindow
        isOpen={isChatOpen}
        onClose={() => setIsChatOpen(false)}
      />
    </div>
  );
};