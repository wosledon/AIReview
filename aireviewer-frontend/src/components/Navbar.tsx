import React, { Fragment } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Menu, Transition } from '@headlessui/react';
import { 
  Bars3Icon, 
  BellIcon, 
  CodeBracketIcon,
  PlusIcon,
  ChevronDownIcon,
  UserIcon,
  Cog6ToothIcon,
  CpuChipIcon,
  ArrowRightOnRectangleIcon
} from '@heroicons/react/24/outline';
import { useAuth } from '../contexts/AuthContext';
import { useUISettings } from '../hooks/useUISettings';

export const Navbar: React.FC = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const { theme, setTheme, density, setDensity } = useUISettings();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  const displayName = user?.displayName || user?.email || '';
  const initials = (() => {
    const src = displayName || '';
    if (!src) return '?';
    const parts = src.trim().split(/\s+/);
    if (parts.length >= 2) return (parts[0][0] + parts[1][0]).toUpperCase();
    return src[0]?.toUpperCase() || '?';
  })();

  return (
    <nav className="sticky top-0 z-40 bg-white/80 dark:bg-gray-900/80 backdrop-blur supports-[backdrop-filter]:bg-white/60 dark:supports-[backdrop-filter]:bg-gray-900/60 border-b border-gray-200 dark:border-gray-800">
      <div className="mx-auto max-w-screen-2xl px-4 sm:px-6 lg:px-8">
        <div className="flex justify-between items-center h-16">
          {/* Logo and brand */}
          <div className="flex items-center">
            <Link to="/" className="flex items-center space-x-2">
              <CodeBracketIcon className="h-8 w-8 text-primary-600" />
              <span className="text-xl font-bold text-gray-900 dark:text-gray-100">AI Reviewer</span>
            </Link>
          </div>

          {/* Navigation links */}
          {isAuthenticated && (
            <div className="hidden md:flex items-center space-x-8">
              <Link 
                to="/projects" 
                className="text-gray-700 dark:text-gray-200 hover:text-primary-600 px-3 py-2 rounded-md text-sm font-medium"
              >
                项目
              </Link>
              <Link 
                to="/reviews" 
                className="text-gray-700 dark:text-gray-200 hover:text-primary-600 px-3 py-2 rounded-md text-sm font-medium"
              >
                代码评审
              </Link>
              <Link 
                to="/projects/new" 
                className="btn btn-primary inline-flex items-center space-x-1"
              >
                <PlusIcon className="h-4 w-4" />
                <span>新建项目</span>
              </Link>
            </div>
          )}

          {/* Right side */}
          <div className="flex items-center space-x-4">
            {isAuthenticated ? (
              <>
                {/* Notifications */}
                <button className="text-gray-400 hover:text-gray-500 dark:text-gray-300 dark:hover:text-gray-100 p-2">
                  <BellIcon className="h-6 w-6" />
                </button>

                {/* User menu */}
                <Menu as="div" className="relative">
                  <Menu.Button className="flex items-center gap-2 text-gray-700 dark:text-gray-200 hover:text-gray-900 dark:hover:text-white px-2 py-1 rounded-full border border-gray-200 dark:border-gray-700 hover:shadow-sm transition focus:outline-none focus-visible:ring-2 focus-visible:ring-primary-500 focus-visible:ring-offset-2 focus-visible:ring-offset-white dark:focus-visible:ring-offset-gray-900">
                    <span className="relative inline-flex">
                      {user?.avatar ? (
                        <img
                          src={user.avatar}
                          alt={displayName}
                          className="h-8 w-8 rounded-full object-cover ring-1 ring-gray-200"
                        />
                      ) : (
                        <span className="h-8 w-8 rounded-full bg-primary-100 text-primary-700 flex items-center justify-center ring-1 ring-gray-200 text-xs font-semibold">
                          {initials}
                        </span>
                      )}
                      <span className="absolute bottom-0 right-0 block h-2.5 w-2.5 rounded-full ring-2 ring-white bg-emerald-500" />
                    </span>
                    <span className="hidden md:flex md:flex-col md:items-start">
                      <span className="text-sm font-medium leading-4">{displayName}</span>
                      <span className="text-[11px] text-gray-500 leading-3">账号</span>
                    </span>
                    <ChevronDownIcon className="h-4 w-4 text-gray-400 hidden md:block" />
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
                    <Menu.Items className="absolute right-0 mt-2 w-64 bg-white dark:bg-gray-900 rounded-md shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none z-50 border border-gray-100 dark:border-gray-800">
                      <div className="px-4 py-3 border-b border-gray-100">
                        <div className="flex items-center gap-3">
                          {user?.avatar ? (
                            <img src={user.avatar} alt={displayName} className="h-10 w-10 rounded-full object-cover ring-1 ring-gray-200" />
                          ) : (
                            <span className="h-10 w-10 rounded-full bg-primary-100 text-primary-700 flex items-center justify-center ring-1 ring-gray-200 text-sm font-semibold">
                              {initials}
                            </span>
                          )}
                          <div>
                            <div className="text-sm font-medium text-gray-900 dark:text-gray-100">{displayName}</div>
                            {user?.email && (
                              <div className="text-xs text-gray-500 dark:text-gray-400">{user.email}</div>
                            )}
                          </div>
                        </div>
                      </div>
                      <div className="py-1">
                        <Menu.Item>
                          {({ active }) => (
                            <Link
                              to="/profile"
                              className={`${active ? 'bg-gray-100 text-gray-900' : 'text-gray-700'} flex items-center gap-2 px-4 py-2 text-sm`}
                            >
                              <UserIcon className="h-4 w-4 text-gray-400" />
                              <span>个人资料</span>
                            </Link>
                          )}
                        </Menu.Item>
                        <Menu.Item>
                          {({ active }) => (
                            <Link
                              to="/settings/reviews"
                              className={`${active ? 'bg-gray-100 text-gray-900' : 'text-gray-700'} flex items-center gap-2 px-4 py-2 text-sm`}
                            >
                              <Cog6ToothIcon className="h-4 w-4 text-gray-400" />
                              <span>评审设置</span>
                            </Link>
                          )}
                        </Menu.Item>
                        <Menu.Item>
                          {({ active }) => (
                            <Link
                              to="/admin/llm-config"
                              className={`${active ? 'bg-gray-100 text-gray-900' : 'text-gray-700'} flex items-center gap-2 px-4 py-2 text-sm`}
                            >
                              <CpuChipIcon className="h-4 w-4 text-gray-400" />
                              <span>LLM配置</span>
                            </Link>
                          )}
                        </Menu.Item>
                        <div className="px-4 py-2">
                          <div className="text-xs uppercase tracking-wide text-gray-400 mb-1">主题</div>
                          <div className="flex items-center gap-2">
                            {(['system','light','dark'] as const).map(m => (
                              <button key={m} onClick={() => setTheme(m)} className={`px-2 py-1 rounded text-xs border ${theme===m ? 'bg-primary-50 text-primary-700 border-primary-200' : 'text-gray-600 border-gray-200 hover:bg-gray-50'}`}>{m==='system'?'跟随系统':m==='light'?'浅色':'深色'}</button>
                            ))}
                          </div>
                        </div>
                        <div className="px-4 py-2">
                          <div className="text-xs uppercase tracking-wide text-gray-400 mb-1">布局密度</div>
                          <div className="flex items-center gap-2">
                            {(['compact','comfortable','full'] as const).map(d => (
                              <button key={d} onClick={() => setDensity(d)} className={`px-2 py-1 rounded text-xs border ${density===d ? 'bg-primary-50 text-primary-700 border-primary-200' : 'text-gray-600 border-gray-200 hover:bg-gray-50'}`}>{d==='compact'?'紧凑':d==='comfortable'?'舒展':'全宽'}</button>
                            ))}
                          </div>
                        </div>
                        <div className="my-1 border-t border-gray-100" />
                        <Menu.Item>
                          {({ active }) => (
                            <button
                              onClick={handleLogout}
                              className={`${active ? 'bg-gray-100 text-gray-900' : 'text-gray-700'} w-full text-left px-4 py-2 text-sm flex items-center gap-2`}
                            >
                              <ArrowRightOnRectangleIcon className="h-4 w-4 text-gray-400" />
                              <span>退出登录</span>
                            </button>
                          )}
                        </Menu.Item>
                      </div>
                    </Menu.Items>
                  </Transition>
                </Menu>
              </>
            ) : (
              <div className="flex items-center space-x-4">
                <Link
                  to="/login"
                  className="text-gray-700 hover:text-primary-600 px-3 py-2 rounded-md text-sm font-medium"
                >
                  登录
                </Link>
                <Link
                  to="/register"
                  className="btn btn-primary"
                >
                  注册
                </Link>
              </div>
            )}

            {/* Mobile menu button */}
            <button className="md:hidden text-gray-400 hover:text-gray-500 p-2">
              <Bars3Icon className="h-6 w-6" />
            </button>
          </div>
        </div>
      </div>
    </nav>
  );
};