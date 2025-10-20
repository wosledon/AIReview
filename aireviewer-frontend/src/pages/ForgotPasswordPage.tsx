import React, { useState } from 'react';
import { Link } from 'react-router-dom';
import { useTranslation } from 'react-i18next';
import { CodeBracketIcon, ExclamationTriangleIcon, CheckCircleIcon, ArrowLeftIcon } from '@heroicons/react/24/outline';

export const ForgotPasswordPage: React.FC = () => {
  const { t } = useTranslation();
  const [email, setEmail] = useState('');
  const [isLoading, setIsLoading] = useState(false);
  const [emailSent, setEmailSent] = useState(false);
  const [error, setError] = useState('');

  const validateEmail = (email: string): boolean => {
    return /^[^\s@]+@[^\s@]+\.[^\s@]+$/.test(email);
  };

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    
    if (!email) {
      setError(t('auth.forgotPassword.errors.emailRequired'));
      return;
    }
    
    if (!validateEmail(email)) {
      setError(t('auth.forgotPassword.errors.invalidEmail'));
      return;
    }

    setIsLoading(true);
    setError('');

    try {
      // 模拟API调用
      await new Promise(resolve => setTimeout(resolve, 2000));
      setEmailSent(true);
    } catch {
      setError(t('auth.forgotPassword.errors.sendFailed'));
    } finally {
      setIsLoading(false);
    }
  };

  const handleInputChange = (e: React.ChangeEvent<HTMLInputElement>) => {
    setEmail(e.target.value);
    if (error) setError('');
  };

  const handleResendEmail = async () => {
    setIsLoading(true);
    try {
      // 模拟重新发送邮件
      await new Promise(resolve => setTimeout(resolve, 1000));
    } catch {
      setError(t('auth.forgotPassword.errors.resendFailed'));
    } finally {
      setIsLoading(false);
    }
  };

  if (emailSent) {
    return (
      <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
        <div className="max-w-md w-full space-y-8">
          <div className="text-center">
            <div className="flex justify-center">
              <CheckCircleIcon className="h-16 w-16 text-green-500" />
            </div>
            <h2 className="mt-6 text-3xl font-extrabold text-gray-900">
              {t('auth.forgotPassword.success.title')}
            </h2>
            <p className="mt-4 text-gray-600">
              {t('auth.forgotPassword.success.message', { email })}
            </p>
            <p className="mt-2 text-sm text-gray-500">
              {t('auth.forgotPassword.success.instructions')}
            </p>
          </div>

          <div className="space-y-4">
            <button
              onClick={handleResendEmail}
              disabled={isLoading}
              className="btn btn-secondary w-full"
            >
              {isLoading ? t('auth.forgotPassword.sending') : t('auth.forgotPassword.success.resendButton')}
            </button>
            
            <div className="text-center">
              <Link
                to="/login"
                className="btn btn-primary w-full"
              >
                {t('auth.forgotPassword.backToLogin')}
              </Link>
            </div>
            
            <div className="text-center">
              <p className="text-sm text-gray-500">
                {t('auth.forgotPassword.success.noEmailHelp')}{' '}
                <button
                  onClick={handleResendEmail}
                  className="font-medium text-primary-600 hover:text-primary-500"
                  disabled={isLoading}
                >
                  {t('auth.forgotPassword.success.resendLink')}
                </button>
              </p>
            </div>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="min-h-screen flex items-center justify-center bg-gray-50 py-12 px-4 sm:px-6 lg:px-8">
      <div className="max-w-md w-full space-y-8">
        <div>
          <div className="flex justify-center">
            <CodeBracketIcon className="h-12 w-12 text-primary-600" />
          </div>
          <h2 className="mt-6 text-center text-3xl font-extrabold text-gray-900">
            {t('auth.forgotPassword.title')}
          </h2>
          <p className="mt-2 text-center text-sm text-gray-600">
            {t('auth.forgotPassword.subtitle')}
          </p>
        </div>

        <form className="mt-8 space-y-6" onSubmit={handleSubmit}>
          {error && (
            <div className="bg-red-50 border border-red-200 text-red-600 px-4 py-3 rounded-md text-sm flex items-center">
              <ExclamationTriangleIcon className="h-5 w-5 mr-2 flex-shrink-0" />
              {error}
            </div>
          )}

          <div>
            <label htmlFor="email" className="block text-sm font-medium text-gray-700 mb-2">
              {t('auth.forgotPassword.emailLabel')}
            </label>
            <input
              id="email"
              name="email"
              type="email"
              autoComplete="email"
              className={`input ${error ? 'border-red-300 focus:border-red-500 focus:ring-red-500' : ''}`}
              placeholder={t('auth.forgotPassword.emailPlaceholder')}
              value={email}
              onChange={handleInputChange}
              disabled={isLoading}
              required
            />
          </div>

          <div>
            <button
              type="submit"
              disabled={isLoading || !email}
              className="btn btn-primary w-full flex justify-center items-center"
            >
              {isLoading ? (
                <>
                  <svg className="animate-spin -ml-1 mr-3 h-5 w-5" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                    <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                    <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8V0C5.373 0 0 5.373 0 12h4zm2 5.291A7.962 7.962 0 014 12H0c0 3.042 1.135 5.824 3 7.938l3-2.647z"></path>
                  </svg>
                  {t('auth.forgotPassword.sending')}
                </>
              ) : (
                t('auth.forgotPassword.sendButton')
              )}
            </button>
          </div>

          <div className="flex items-center justify-center">
            <Link
              to="/login"
              className="flex items-center text-sm text-primary-600 hover:text-primary-500"
            >
              <ArrowLeftIcon className="h-4 w-4 mr-1" />
              {t('auth.forgotPassword.backToLogin')}
            </Link>
          </div>
        </form>

        <div className="text-center">
          <p className="text-xs text-gray-500">
            {t('auth.forgotPassword.noAccount')}{' '}
            <Link
              to="/register"
              className="font-medium text-primary-600 hover:text-primary-500"
            >
              {t('auth.forgotPassword.registerNow')}
            </Link>
          </p>
        </div>
      </div>
    </div>
  );
};