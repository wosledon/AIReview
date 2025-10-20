import React, { useState, useRef, useEffect } from 'react';
import { useTranslation } from 'react-i18next';
import { GlobeAltIcon, CheckIcon } from '@heroicons/react/24/outline';

export const LanguageSwitcher: React.FC = () => {
  const { i18n } = useTranslation();
  const [isOpen, setIsOpen] = useState(false);
  const dropdownRef = useRef<HTMLDivElement>(null);

  const changeLanguage = (lng: 'en' | 'zh') => {
    i18n.changeLanguage(lng);
    try {
      localStorage.setItem('i18nextLng', lng);
    } catch {
      // ignore storage errors (private mode or disabled storage)
    }
    setIsOpen(false);
  };

  // 点击外部关闭下拉菜单
  useEffect(() => {
    const handleClickOutside = (event: MouseEvent) => {
      if (dropdownRef.current && !dropdownRef.current.contains(event.target as Node)) {
        setIsOpen(false);
      }
    };

    document.addEventListener('mousedown', handleClickOutside);
    return () => document.removeEventListener('mousedown', handleClickOutside);
  }, []);

  const current = i18n.resolvedLanguage || i18n.language || 'en';
  const currentLabel = current.startsWith('zh') ? '中文' : 'EN';

  return (
    <div className="relative" ref={dropdownRef}>
      {/* 触发按钮 */}
      <button
        onClick={() => setIsOpen(!isOpen)}
        className="flex items-center gap-2 px-3 py-2 rounded-lg text-sm font-medium text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
        aria-label="选择语言"
      >
        <GlobeAltIcon className="h-5 w-5" />
        <span className="hidden sm:inline">{currentLabel}</span>
      </button>

      {/* 下拉菜单 */}
      {isOpen && (
        <div className="absolute right-0 mt-2 w-40 bg-white dark:bg-gray-800 rounded-lg shadow-lg border border-gray-200 dark:border-gray-700 py-1 z-50 animate-in fade-in slide-in-from-top-2 duration-200">
          <button
            onClick={() => changeLanguage('en')}
            className={`w-full flex items-center justify-between px-4 py-2 text-sm transition-colors ${
              current.startsWith('en')
                ? 'text-primary-600 dark:text-primary-400 bg-primary-50 dark:bg-primary-900/20'
                : 'text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
            }`}
          >
            <span className="font-medium">English</span>
            {current.startsWith('en') && <CheckIcon className="h-4 w-4" />}
          </button>
          <button
            onClick={() => changeLanguage('zh')}
            className={`w-full flex items-center justify-between px-4 py-2 text-sm transition-colors ${
              current.startsWith('zh')
                ? 'text-primary-600 dark:text-primary-400 bg-primary-50 dark:bg-primary-900/20'
                : 'text-gray-700 dark:text-gray-300 hover:bg-gray-50 dark:hover:bg-gray-700'
            }`}
          >
            <span className="font-medium">简体中文</span>
            {current.startsWith('zh') && <CheckIcon className="h-4 w-4" />}
          </button>
        </div>
      )}
    </div>
  );
};
