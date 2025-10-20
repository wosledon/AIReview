import React from 'react';
import { Link } from 'react-router-dom';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { 
  CodeBracketIcon, 
  CpuChipIcon, 
  ShieldCheckIcon, 
  ClockIcon,
  UsersIcon,
  ChartBarIcon,
  ArrowPathIcon
} from '@heroicons/react/24/outline';
import { useAuth } from '../contexts/AuthContext';
import { projectService } from '../services/project.service';
import { reviewService } from '../services/review.service';
import { ReviewState } from '../types/review';
import type { PagedResult, Review } from '../types/review';
import type { Project } from '../types/project';
import { CountUp } from '../components/CountUp';
import { useTranslation } from 'react-i18next';

export const HomePage: React.FC = () => {
  const { isAuthenticated, user } = useAuth();
  const { t, i18n } = useTranslation();

  // 辅助：状态样式与文本
  const getStatusText = (status: string) => {
    switch (status) {
      case 'Approved':
        return t('status.Approved');
      case 'Rejected':
        return t('status.Rejected');
      case 'AIReviewing':
        return t('status.AIReviewing');
      case 'HumanReview':
        return t('status.HumanReview');
      case 'Pending':
      default:
        return t('status.Pending');
    }
  };

  const getStatusColor = (status: string) => {
    switch (status) {
      case 'Approved':
        return 'bg-green-100 text-green-800';
      case 'Rejected':
        return 'bg-red-100 text-red-800';
      case 'AIReviewing':
        return 'bg-blue-100 text-blue-800';
      case 'HumanReview':
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  };

  // 辅助：相对时间格式化
  const formatRelativeTime = (iso: string) => {
    try {
      const date = new Date(iso);
      const now = new Date();
      const diffMs = date.getTime() - now.getTime();
      const absMs = Math.abs(diffMs);
  const rtf = new Intl.RelativeTimeFormat(i18n.resolvedLanguage || i18n.language || 'en', { numeric: 'auto' });
      const minutes = Math.round(absMs / (60 * 1000));
      if (minutes < 60) return rtf.format(Math.sign(diffMs) * -minutes, 'minute');
      const hours = Math.round(minutes / 60);
      if (hours < 24) return rtf.format(Math.sign(diffMs) * -hours, 'hour');
      const days = Math.round(hours / 24);
      return rtf.format(Math.sign(diffMs) * -days, 'day');
    } catch {
  return new Date(iso).toLocaleString(i18n.resolvedLanguage || i18n.language || 'en');
    }
  };

  // 顶层声明 hooks，使用 enabled 避免未登录时请求
  const { data: projectsData, isFetching: fetchingProjects } = useQuery<PagedResult<Project>>({
    queryKey: ['stats', 'projects', 'count'] as const,
    queryFn: () => projectService.getProjects(),
    placeholderData: keepPreviousData,
    enabled: isAuthenticated,
    staleTime: 5_000,
    refetchOnWindowFocus: true,
    refetchInterval: 60_000
  });

  const { data: approvedData, isFetching: fetchingApproved } = useQuery<PagedResult<Review>>({
    queryKey: ['stats', 'reviews', 'count', 'approved'] as const,
    queryFn: () => reviewService.getReviews({ status: ReviewState.Approved, page: 1, pageSize: 1 }),
    placeholderData: keepPreviousData,
    enabled: isAuthenticated,
    staleTime: 5_000,
    refetchOnWindowFocus: true,
    refetchInterval: 60_000
  });

  const { data: pendingData, isFetching: fetchingPending } = useQuery<{ totalCount: number}>({
    queryKey: ['stats', 'reviews', 'count', 'pending'] as const,
    queryFn: async () => {
      const [pending, ai] = await Promise.all([
        reviewService.getReviews({ status: ReviewState.Pending, page: 1, pageSize: 1 }),
        reviewService.getReviews({ status: ReviewState.AIReviewing, page: 1, pageSize: 1 })
      ]);
      return { totalCount: (pending?.totalCount || 0) + (ai?.totalCount || 0) };
    },
    placeholderData: keepPreviousData,
    enabled: isAuthenticated,
    staleTime: 5_000,
    refetchOnWindowFocus: true,
    refetchInterval: 60_000
  });

  const { data: recentReviews, isFetching: fetchingRecent, refetch: refetchRecent } = useQuery<PagedResult<Review>>({
    queryKey: ['home', 'recent-reviews', { page: 1, pageSize: 5 }] as const,
    queryFn: () => reviewService.getReviews({ page: 1, pageSize: 5 }),
    placeholderData: keepPreviousData,
    enabled: isAuthenticated,
    staleTime: 5_000,
    refetchOnWindowFocus: true,
    refetchInterval: 30_000
  });

  const features = [
    {
      icon: CpuChipIcon,
      title: t('features.ai.title'),
      description: t('features.ai.desc')
    },
    {
      icon: ShieldCheckIcon,
      title: t('features.security.title'),
      description: t('features.security.desc')
    },
    {
      icon: ClockIcon,
      title: t('features.fast.title'),
      description: t('features.fast.desc')
    },
    {
      icon: UsersIcon,
      title: t('features.collab.title'),
      description: t('features.collab.desc')
    },
    {
      icon: ChartBarIcon,
      title: t('features.report.title'),
      description: t('features.report.desc')
    },
    {
      icon: CodeBracketIcon,
      title: t('features.multilang.title'),
      description: t('features.multilang.desc')
    }
  ];

  if (isAuthenticated) {
    const projectsCount = projectsData?.totalCount ?? 0;
    const approvedCount = approvedData?.totalCount ?? 0;
    const pendingCount = pendingData?.totalCount ?? 0;

    return (
      <div className="space-y-8">
        {/* Welcome Section */}
  <div className="bg-white dark:bg-gray-900 rounded-xl p-6 border border-gray-200/70 dark:border-gray-800 shadow-sm transition hover:shadow-md">
          <h1 className="text-2xl font-bold text-gray-900 mb-2">
            {t('home.welcome', { name: user?.displayName || user?.userName })}
          </h1>
          <p className="text-gray-600 mb-6">
            {t('home.start')}
          </p>
          
          <div className="flex flex-wrap gap-4">
            <Link to="/projects/new" className="btn btn-primary">
              {t('home.createProject')}
            </Link>
            <Link to="/projects" className="btn btn-secondary">
              {t('home.viewProjects')}
            </Link>
            <Link to="/reviews" className="btn btn-secondary">
              {t('home.goReviews')}
            </Link>
          </div>
        </div>

        {/* Quick Stats */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="card transition hover:shadow-md bg-gradient-to-br from-primary-50 to-white dark:from-primary-950/20 dark:to-gray-900">
            <div className="flex items-center">
              <div className="p-3 bg-primary-100 rounded-lg">
                <CodeBracketIcon className="h-6 w-6 text-primary-600" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-500">{t('home.myProjects')}</p>
                <p className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
                  {fetchingProjects ? '…' : <CountUp to={projectsCount} />}
                </p>
              </div>
            </div>
          </div>

          <div className="card transition hover:shadow-md bg-gradient-to-br from-green-50 to-white dark:from-green-950/20 dark:to-gray-900">
            <div className="flex items-center">
              <div className="p-3 bg-green-100 rounded-lg">
                <ShieldCheckIcon className="h-6 w-6 text-green-600" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-500">{t('home.completedReviews')}</p>
                <p className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
                  {fetchingApproved ? '…' : <CountUp to={approvedCount} />}
                </p>
              </div>
            </div>
          </div>

          <div className="card transition hover:shadow-md bg-gradient-to-br from-orange-50 to-white dark:from-orange-950/20 dark:to-gray-900">
            <div className="flex items-center">
              <div className="p-3 bg-orange-100 rounded-lg">
                <ClockIcon className="h-6 w-6 text-orange-600" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-500">{t('home.pendingReviews')}</p>
                <p className="text-2xl font-semibold text-gray-900 dark:text-gray-100">
                  {fetchingPending ? '…' : <CountUp to={pendingCount} />}
                </p>
              </div>
            </div>
          </div>
        </div>

        {/* Recent Activity */}
  <div className="card dark:bg-gray-900 dark:border-gray-800 transition hover:shadow-md">
          <div className="flex items-center justify-between mb-4">
            <h2 className="text-lg font-semibold text-gray-900">{t('home.recentActivity')}</h2>
            <div className="flex items-center gap-2">
              {fetchingRecent && (
                <svg className="animate-spin h-4 w-4 text-primary-600" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                </svg>
              )}
              <button
                onClick={() => refetchRecent()}
                className="inline-flex items-center px-2 py-1 text-sm text-gray-600 hover:text-primary-700 hover:bg-primary-50 rounded"
                title={t('home.refresh')}
              >
                <ArrowPathIcon className="h-4 w-4 mr-1" /> {t('home.refresh')}
              </button>
              <Link to="/reviews" className="text-sm text-primary-600 hover:text-primary-700">
                {t('home.viewAll')}
              </Link>
            </div>
          </div>
          {fetchingRecent && !recentReviews ? (
            <ul className="animate-pulse divide-y divide-gray-200">
              {Array.from({ length: 3 }).map((_, i) => (
                <li key={i} className="py-4 flex items-center justify-between">
                  <div className="flex-1">
                    <div className="h-4 bg-gray-200 rounded w-48 mb-2"></div>
                    <div className="h-3 bg-gray-100 rounded w-64"></div>
                  </div>
                  <span className="h-5 w-16 bg-gray-200 rounded-full"></span>
                </li>
              ))}
            </ul>
          ) : recentReviews && recentReviews.items && recentReviews.items.length > 0 ? (
            <ul className="divide-y divide-gray-200">
              {recentReviews.items.map((r) => (
                <li key={r.id} className="py-4 flex items-center justify-between text-left">
                  <div>
                    <Link to={`/reviews/${r.id}`} className="text-sm font-medium text-gray-900 hover:text-primary-600">
                      {r.title}
                    </Link>
                    <div className="text-xs text-gray-500 mt-1">
                      <span className="mr-2">{t('labels.project')}：{r.projectName}</span>
                      <span title={new Date(r.createdAt).toLocaleString(i18n.resolvedLanguage || i18n.language || 'en')}>{t('labels.time')}：{formatRelativeTime(r.createdAt)}</span>
                    </div>
                  </div>
                  <span className={`text-xs px-2.5 py-0.5 rounded-full ${getStatusColor(r.status)}`}>
                    {getStatusText(r.status)}
                  </span>
                </li>
              ))}
            </ul>
          ) : (
            <div className="text-center py-8">
              <CodeBracketIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
              <p className="text-gray-500">{t('home.noActivity')}</p>
              <p className="text-sm text-gray-400 mt-1">{t('home.createFirstProject')}</p>
            </div>
          )}
        </div>
      </div>
    );
  }

  return (
  <div className="space-y-16">
      {/* Hero Section */}
  <div className="text-center">
        <div className="flex justify-center mb-8">
          <div className="p-4 bg-primary-100 rounded-2xl">
            <CpuChipIcon className="h-16 w-16 text-primary-600" />
          </div>
        </div>
        
  <h1 className="text-4xl md:text-6xl font-bold text-gray-900 dark:text-gray-100 mb-6">
          {t('hero.title')}
        </h1>
        
  <p className="text-xl text-gray-600 dark:text-gray-300 mb-8 max-w-3xl mx-auto">
          {t('hero.subtitle')}
        </p>
        
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Link to="/register" className="btn btn-primary text-lg px-8 py-3">
            {t('hero.cta_free')}
          </Link>
          <Link to="/login" className="btn btn-secondary text-lg px-8 py-3">
            {t('hero.cta_login')}
          </Link>
        </div>
      </div>

      {/* Features Section */}
      <div>
  <h2 className="text-3xl font-bold text-center text-gray-900 dark:text-gray-100 mb-12">
          {t('features.title')}
        </h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          {features.map((feature, index) => (
            <div key={index} className="card hover:shadow-lg transition-shadow dark:bg-gray-900 dark:border-gray-800">
              <div className="flex items-center mb-4">
                <div className="p-3 bg-primary-100 rounded-lg">
                  <feature.icon className="h-6 w-6 text-primary-600" />
                </div>
                <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 ml-3">
                  {feature.title}
                </h3>
              </div>
              <p className="text-gray-600 dark:text-gray-300">
                {feature.description}
              </p>
            </div>
          ))}
        </div>
      </div>

      {/* CTA Section */}
      <div className="bg-primary-50 dark:bg-primary-900/20 rounded-2xl p-8 text-center">
        <h2 className="text-3xl font-bold text-gray-900 mb-4">
          {t('features.ready.title')}
        </h2>
        <p className="text-lg text-gray-600 dark:text-gray-300 mb-8 max-w-2xl mx-auto">
          {t('features.ready.desc')}
        </p>
        <Link to="/register" className="btn btn-primary text-lg px-8 py-3">
          {t('features.ready.cta')}
        </Link>
      </div>
    </div>
  );
};