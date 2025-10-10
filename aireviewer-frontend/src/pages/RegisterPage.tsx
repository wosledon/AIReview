import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { EyeIcon, EyeSlashIcon, CodeBracketIcon, ExclamationTriangleIcon } from '@heroicons/react/24/outline';
import { useAuth } from '../contexts/AuthContext';
import type { RegisterRequest } from '../types/auth';

interface FormErrors {
  email?: string;
  password?: string;
  confirmPassword?: string;
  userName?: string;
  displayName?: string;
  general?: string;
}

export const RegisterPage: React.FC = () => {
  const [formData, setFormData] = useState<RegisterRequest>({
    email: '',
    password: '',
    confirmPassword: '',
    userName: '',
    displayName: '',
  });
  const [showPassword, setShowPassword] = useState(false);
  const [showConfirmPassword, setShowConfirmPassword] = useState(false);
  const [errors, setErrors] = useState<FormErrors>({});
  const [isLoading, setIsLoading] = useState(false);

  const { register } = useAuth();
  const navigate = useNavigate();

  const validateForm = (): boolean => {
    const newErrors: FormErrors = {};

    // Email validation
    if (!formData.email) {
      newErrors.email = '请输入邮箱地址';
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = '请输入有效的邮箱地址';
    }

    // Username validation
    if (!formData.userName) {
      newErrors.userName = '请输入用户名';
    } else if (formData.userName.length < 3) {
      newErrors.userName = '用户名至少需要3个字符';
    } else if (!/^[a-zA-Z0-9_-]+$/.test(formData.userName)) {
      newErrors.userName = '用户名只能包含字母、数字、下划线和短横线';
    }

    // Display name validation
    if (!formData.displayName) {
      newErrors.displayName = '请输入显示名称';
    } else if (formData.displayName.length < 2) {
      newErrors.displayName = '显示名称至少需要2个字符';
    }

    // Password validation
    if (!formData.password) {
      newErrors.password = '请输入密码';
    } else if (formData.password.length < 6) {
      newErrors.password = '密码至少需要6个字符';
    } else if (!/(?=.*[a-zA-Z])(?=.*\d)/.test(formData.password)) {
      newErrors.password = '密码必须包含字母和数字';
    }

    // Confirm password validation
    if (!formData.confirmPassword) {
      newErrors.confirmPassword = '请确认密码';
    } else if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = '两次输入的密码不一致';
    }

    setErrors(newErrors);
    return Object.keys(newErrors).length === 0;
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    const { name, value } = e.target;
    setFormData(prev => ({
      ...prev,
      [name]: value,
    }));
    
    // Clear field-specific error when user starts typing
    if (errors[name as keyof FormErrors]) {
      setErrors(prev => ({
        ...prev,
        [name]: undefined,
      }));
    }
    
    // Clear general error
    if (errors.general) {
      setErrors(prev => ({
        ...prev,
        general: undefined,
      }));
    }
  };

  const getErrorMessage = (error: unknown): string => {
    if (typeof error === 'string') return error;
    
    if (error && typeof error === 'object') {
      const err = error as { 
        response?: { 
          data?: { message?: string; error?: string }; 
          status?: number; 
        }; 
        message?: string; 
      };
      
      if (err?.response?.data?.message) return err.response.data.message;
      if (err?.response?.data?.error) return err.response.data.error;
      if (err?.message) return err.message;
      
      // Check for specific HTTP status codes
      const status = err?.response?.status;
      if (status === 409) {
        return '邮箱或用户名已被使用，请尝试其他的';
      }
      if (status === 400) {
        return '请检查输入信息是否正确';
      }
      if (status && status >= 500) {
        return '服务器暂时不可用，请稍后再试';
      }
    }
    
    return '注册失败，请检查网络连接或稍后重试';
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!validateForm()) return;
    
    setIsLoading(true);
    setErrors({});

    try {
      await register(formData);
      navigate('/', { replace: true });
    } catch (error) {
      console.error('Registration failed:', error);
      setErrors({
        general: getErrorMessage(error)
      });
    } finally {
      setIsLoading(false);
    }
  };

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <div className="flex justify-center">
            <CodeBracketIcon className="h-12 w-12 text-primary-600" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            创建新账户
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            已有账户？{' '}
            <Link
              to="/login"
              className="font-medium text-primary-600 hover:text-primary-500"
            >
              立即登录
            </Link>
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          {errors.general && (
            <div className="bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-md text-sm flex items-center">
              <ExclamationTriangleIcon className="h-5 w-5 mr-2 flex-shrink-0" />
              {errors.general}
            </div>
          )}

          <div className="space-y-4">
            <div>
              <label htmlFor="userName" className="block text-sm font-medium text-gray-700 mb-1">
                用户名 *
              </label>
              <input
                id="userName"
                name="userName"
                type="text"
                autoComplete="username"
                className={`input ${errors.userName ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                placeholder="请输入用户名（字母、数字、下划线）"
                value={formData.userName}
                onChange={handleInputChange}
                disabled={isLoading}
              />
              {errors.userName && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                  {errors.userName}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="displayName" className="block text-sm font-medium text-gray-700 mb-1">
                显示名称 *
              </label>
              <input
                id="displayName"
                name="displayName"
                type="text"
                autoComplete="name"
                className={`input ${errors.displayName ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                placeholder="请输入显示名称"
                value={formData.displayName}
                onChange={handleInputChange}
                disabled={isLoading}
              />
              {errors.displayName && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                  {errors.displayName}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-1">
                邮箱地址 *
              </label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                className={`input ${errors.email ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                placeholder="请输入邮箱地址"
                value={formData.email}
                onChange={handleInputChange}
                disabled={isLoading}
              />
              {errors.email && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                  {errors.email}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700 mb-1">
                密码 *
              </label>
              <div className="relative">
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  className={`input pr-10 ${errors.password ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                  placeholder="请输入密码（至少6位，包含字母和数字）"
                  value={formData.password}
                  onChange={handleInputChange}
                  disabled={isLoading}
                />
                <button
                  type="button"
                  className="absolute inset-y-0 right-0 pr-3 flex items-center"
                  onClick={() => setShowPassword(!showPassword)}
                  disabled={isLoading}
                >
                  {showPassword ? (
                    <EyeSlashIcon className="h-5 w-5 text-gray-400 hover:text-gray-600" />
                  ) : (
                    <EyeIcon className="h-5 w-5 text-gray-400 hover:text-gray-600" />
                  )}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                  {errors.password}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 mb-1">
                确认密码 *
              </label>
              <div className="relative">
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  className={`input pr-10 ${errors.confirmPassword ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                  placeholder="请再次输入密码"
                  value={formData.confirmPassword}
                  onChange={handleInputChange}
                  disabled={isLoading}
                />
                <button
                  type="button"
                  className="absolute inset-y-0 right-0 pr-3 flex items-center"
                  onClick={() => setShowConfirmPassword(!showConfirmPassword)}
                  disabled={isLoading}
                >
                  {showConfirmPassword ? (
                    <EyeSlashIcon className="h-5 w-5 text-gray-400 hover:text-gray-600" />
                  ) : (
                    <EyeIcon className="h-5 w-5 text-gray-400 hover:text-gray-600" />
                  )}
                </button>
              </div>
              {errors.confirmPassword && (
                <p className="mt-1 text-sm text-red-600 flex items-center">
                  <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                  {errors.confirmPassword}
                </p>
              )}
            </div>
          </div>

          <div className="flex items-center">
            <input
              id="terms"
              name="terms"
              type="checkbox"
              className="h-4 w-4 text-primary-600 focus:ring-primary-500 border-gray-300 rounded"
              required
              disabled={isLoading}
            />
            <label htmlFor="terms" className="ml-2 block text-sm text-gray-700">
              我同意{' '}
              <Link to="/terms" className="text-primary-600 hover:text-primary-500">
                服务条款
              </Link>{' '}
              和{' '}
              <Link to="/privacy" className="text-primary-600 hover:text-primary-500">
                隐私政策
              </Link>
            </label>
          </div>

          <div>
            <button
              type="submit"
              disabled={isLoading}
              className="btn btn-primary w-full flex justify-center items-center"
            >
              {isLoading ? (
                <>
                  <svg className="animate-spin -ml-1 mr-3 h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  注册中...
                </>
              ) : (
                '创建账户'
              )}
            </button>
          </div>

          <div className="text-center">
            <p className="text-xs text-gray-500">
              点击"创建账户"即表示您同意我们的服务条款和隐私政策
            </p>
          </div>
        </form>
      </div>
    </div>
  );
};