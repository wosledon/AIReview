import React, { Fragment } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { Menu, Transition } from '@headlessui/react';
import { 
  Bars3Icon, 
  BellIcon, 
  UserCircleIcon,
  CodeBracketIcon,
  PlusIcon
} from '@heroicons/react/24/outline';
import { useAuth } from '../contexts/AuthContext';

export const Navbar: React.FC = () => {
  const { user, isAuthenticated, logout } = useAuth();
  const navigate = useNavigate();

  const handleLogout = () => {
    logout();
    navigate('/login');
  };

  return (
    <nav className="bg-white shadow-sm border-b border-gray-200">
      <div className="container mx-auto px-4">
        <div className="flex justify-between items-center h-16">
          {/* Logo and brand */}
          <div className="flex items-center">
            <Link to="/" className="flex items-center space-x-2">
              <CodeBracketIcon className="h-8 w-8 text-primary-600" />
              <span className="text-xl font-bold text-gray-900">AI Reviewer</span>
            </Link>
          </div>

          {/* Navigation links */}
          {isAuthenticated && (
            <div className="hidden md:flex items-center space-x-8">
              <Link 
                to="/projects" 
                className="text-gray-700 hover:text-primary-600 px-3 py-2 rounded-md text-sm font-medium"
              >
                项目
              </Link>
              <Link 
                to="/reviews" 
                className="text-gray-700 hover:text-primary-600 px-3 py-2 rounded-md text-sm font-medium"
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
                <button className="text-gray-400 hover:text-gray-500 p-2">
                  <BellIcon className="h-6 w-6" />
                </button>

                {/* User menu */}
                <Menu as="div" className="relative">
                  <Menu.Button className="flex items-center space-x-2 text-gray-700 hover:text-gray-900">
                    <UserCircleIcon className="h-8 w-8" />
                    <span className="hidden md:block text-sm font-medium">{user?.displayName || user?.email}</span>
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
                    <Menu.Items className="absolute right-0 mt-2 w-48 bg-white rounded-md shadow-lg ring-1 ring-black ring-opacity-5 focus:outline-none z-50">
                      <div className="py-1">
                        <Menu.Item>
                          {({ active }) => (
                            <Link
                              to="/profile"
                              className={`${
                                active ? 'bg-gray-100' : ''
                              } block px-4 py-2 text-sm text-gray-700`}
                            >
                              个人资料
                            </Link>
                          )}
                        </Menu.Item>
                        <Menu.Item>
                          {({ active }) => (
                            <Link
                              to="/settings/reviews"
                              className={`${
                                active ? 'bg-gray-100' : ''
                              } block px-4 py-2 text-sm text-gray-700`}
                            >
                              评审设置
                            </Link>
                          )}
                        </Menu.Item>
                        <Menu.Item>
                          {({ active }) => (
                            <Link
                              to="/admin/llm-config"
                              className={`${
                                active ? 'bg-gray-100' : ''
                              } block px-4 py-2 text-sm text-gray-700`}
                            >
                              LLM配置
                            </Link>
                          )}
                        </Menu.Item>
                        <Menu.Item>
                          {({ active }) => (
                            <button
                              onClick={handleLogout}
                              className={`${
                                active ? 'bg-gray-100' : ''
                              } block w-full text-left px-4 py-2 text-sm text-gray-700`}
                            >
                              退出登录
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