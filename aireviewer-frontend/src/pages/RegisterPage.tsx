import React, { useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import { EyeIcon, EyeSlashIcon, CodeBracketIcon, ExclamationTriangleIcon } from '@heroicons/react/24/outline';
import { useAuth } from '../contexts/AuthContext';
import type { RegisterRequest } from '../types/auth';
import { useTranslation } from 'react-i18next';

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
  const { t } = useTranslation();
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
      newErrors.email = t('errors.email_required');
    } else if (!/^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(formData.email)) {
      newErrors.email = t('errors.email_invalid');
    }

    // Username validation
    if (!formData.userName) {
      newErrors.userName = t('errors.username_required');
    } else if (formData.userName.length < 3) {
      newErrors.userName = t('errors.username_length');
    } else if (!/^[a-zA-Z0-9_-]+$/.test(formData.userName)) {
      newErrors.userName = t('errors.username_charset');
    }

    // Display name validation
    if (!formData.displayName) {
      newErrors.displayName = t('errors.displayName_required');
    } else if (formData.displayName.length < 2) {
      newErrors.displayName = t('errors.displayName_length');
    }

    // Password validation
    if (!formData.password) {
      newErrors.password = t('errors.password_required');
    } else if (formData.password.length < 6) {
      newErrors.password = t('errors.password_length');
    } else if (!/(?=.*[a-zA-Z])(?=.*\d)/.test(formData.password)) {
      newErrors.password = t('errors.password_charset');
    }

    // Confirm password validation
    if (!formData.confirmPassword) {
      newErrors.confirmPassword = t('errors.register_confirmpw_required');
    } else if (formData.password !== formData.confirmPassword) {
      newErrors.confirmPassword = t('errors.register_confirmpw_mismatch');
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
        return t('errors.register_conflict');
      }
      if (status === 400) {
        return t('errors.bad_request');
      }
      if (status && status >= 500) {
        return t('errors.server_unavailable');
      }
    }
    
    return t('errors.register_failed_generic');
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
    <div className="min-h-screen flex items-center justify-center bg-gradient-to-b from-gray-50 to-white dark:from-gray-900 dark:to-gray-950 py-12 px-4 sm:px-6 lg:px-8">
      <div className="w-full max-w-md space-y-8">
        <div>
          <div className="flex justify-center">
            <CodeBracketIcon className="h-12 w-12 text-primary-600" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900 dark:text-gray-100">
            {t('auth.register.title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600 dark:text-gray-400">
            {t('auth.register.have_account')}{' '}
            <Link
              to="/login"
              className="font-medium text-primary-600 hover:text-primary-500"
            >
              {t('auth.register.login_now')}
            </Link>
          </p>
        </div>

        <form className="mt-8 space-y-6 card dark:bg-gray-900 dark:border-gray-800" onSubmit={handleSubmit}>
          {errors.general && (
            <div className="bg-red-50 dark:bg-red-900/30 border border-red-200 dark:border-red-800 text-red-600 dark:text-red-400 px-4 py-3 rounded-md text-sm flex items-center">
              <ExclamationTriangleIcon className="h-5 w-5 mr-2 flex-shrink-0" />
              {errors.general}
            </div>
          )}

          <div className="space-y-4">
            <div>
              <label htmlFor="userName" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                {t('auth.register.username')} *
              </label>
              <input
                id="userName"
                name="userName"
                type="text"
                autoComplete="username"
                className={`input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 ${errors.userName ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                placeholder={t('auth.register.placeholder_username')}
                value={formData.userName}
                onChange={handleInputChange}
                disabled={isLoading}
              />
              {errors.userName && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400 flex items-center">
                  <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                  {errors.userName}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="displayName" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                {t('auth.register.displayName')} *
              </label>
              <input
                id="displayName"
                name="displayName"
                type="text"
                autoComplete="name"
                className={`input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 ${errors.displayName ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                placeholder={t('auth.register.placeholder_displayName')}
                value={formData.displayName}
                onChange={handleInputChange}
                disabled={isLoading}
              />
              {errors.displayName && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400 flex items-center">
                  <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                  {errors.displayName}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="email" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                {t('auth.register.email')} *
              </label>
              <input
                id="email"
                name="email"
                type="email"
                autoComplete="email"
                className={`input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 ${errors.email ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                placeholder={t('auth.register.placeholder_email')}
                value={formData.email}
                onChange={handleInputChange}
                disabled={isLoading}
              />
              {errors.email && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400 flex items-center">
                  <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                  {errors.email}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="password" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                {t('auth.register.password')} *
              </label>
              <div className="relative">
                <input
                  id="password"
                  name="password"
                  type={showPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  className={`input pr-10 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 ${errors.password ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                  placeholder={t('auth.register.placeholder_password')}
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
                    <EyeSlashIcon className="h-5 w-5 text-gray-400 hover:text-gray-200" />
                  ) : (
                    <EyeIcon className="h-5 w-5 text-gray-400 hover:text-gray-200" />
                  )}
                </button>
              </div>
              {errors.password && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400 flex items-center">
                  <ExclamationTriangleIcon className="h-4 w-4 mr-1" />
                  {errors.password}
                </p>
              )}
            </div>

            <div>
              <label htmlFor="confirmPassword" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-1">
                {t('auth.register.confirmPassword')} *
              </label>
              <div className="relative">
                <input
                  id="confirmPassword"
                  name="confirmPassword"
                  type={showConfirmPassword ? 'text' : 'password'}
                  autoComplete="new-password"
                  className={`input pr-10 dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100 ${errors.confirmPassword ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
                  placeholder={t('auth.register.placeholder_confirmPassword')}
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
                    <EyeSlashIcon className="h-5 w-5 text-gray-400 hover:text-gray-200" />
                  ) : (
                    <EyeIcon className="h-5 w-5 text-gray-400 hover:text-gray-200" />
                  )}
                </button>
              </div>
              {errors.confirmPassword && (
                <p className="mt-1 text-sm text-red-600 dark:text-red-400 flex items-center">
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
            <label htmlFor="terms" className="ml-2 block text-sm text-gray-700 dark:text-gray-300">
              {t('auth.register.agree_prefix')}
              <Link to="/terms" className="text-primary-600 hover:text-primary-500">{t('auth.register.terms')}</Link>
              {t('auth.register.and')}
              <Link to="/privacy" className="text-primary-600 hover:text-primary-500">{t('auth.register.privacy')}</Link>
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
                  {t('auth.register.submitting')}
                </>
              ) : (
                t('auth.register.submit')
              )}
            </button>
          </div>

          <div className="text-center">
            <p className="text-xs text-gray-500 dark:text-gray-400">
              {t('auth.register.disclaimer')}
            </p>
          </div>
        </form>
      </div>
    </div>
  );
};