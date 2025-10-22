import { useState, useEffect, useMemo, useCallback } from 'react';
import { Link } from 'react-router-dom';
import { useQuery, keepPreviousData } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { 
  PlusIcon, 
  MagnifyingGlassIcon,
  ClockIcon,
  CheckCircleIcon,
  XCircleIcon,
  CpuChipIcon,
  EyeIcon,
  FunnelIcon
} from '@heroicons/react/24/outline';
import { reviewService } from '../services/review.service';
import { ReviewState } from '../types/review';
import type { Review, ReviewQueryParameters, PagedResult } from '../types/review';

export const ReviewsPage = () => {
  const { t } = useTranslation();
  const [filters, setFilters] = useState<ReviewQueryParameters>({
    page: 1,
    pageSize: 20
  });
  const [searchTerm, setSearchTerm] = useState('');

  // 防抖搜索
  useEffect(() => {
    const timer = setTimeout(() => {
      setFilters(prev => ({
        ...prev,
        search: searchTerm || undefined,
        page: 1 // 重置到第一页
      }));
    }, 300);

    return () => clearTimeout(timer);
  }, [searchTerm]);

  const params = useMemo(() => ({
    ...filters,
  }), [filters]);

  const {
    data: reviewsData,
    isLoading,
    isFetching,
    error,
    refetch
  } = useQuery<PagedResult<Review>, Error, PagedResult<Review>>({
    queryKey: ['reviews', params] as const,
    queryFn: (): Promise<PagedResult<Review>> => reviewService.getReviews(params),
    placeholderData: keepPreviousData,
    staleTime: 1000, // 1秒内数据不会重新获取
  });

  const reviews = reviewsData?.items || [];

  const statusOptions = useMemo(() => [
    { value: '', label: t('reviews.filter_all') },
    { value: ReviewState.Pending, label: t('reviews.filter_pending') },
    { value: ReviewState.AIReviewing, label: t('reviews.filter_ai_reviewing') },
    { value: ReviewState.HumanReview, label: t('reviews.filter_human_review') },
    { value: ReviewState.Approved, label: t('reviews.filter_approved') },
    { value: ReviewState.Rejected, label: t('reviews.filter_rejected') },
  ], [t]);

  const getStatusIcon = useCallback((status: string) => {
    switch (status) {
      case ReviewState.Approved:
        return <CheckCircleIcon className="h-5 w-5 text-green-500" />;
      case ReviewState.Rejected:
        return <XCircleIcon className="h-5 w-5 text-red-500" />;
      case ReviewState.AIReviewing:
        return <CpuChipIcon className="h-5 w-5 text-blue-500" />;
      case ReviewState.HumanReview:
        return <EyeIcon className="h-5 w-5 text-orange-500" />;
      default:
        return <ClockIcon className="h-5 w-5 text-gray-500" />;
    }
  }, []);

  const getStatusText = useCallback((status: string) => {
    switch (status) {
      case ReviewState.Pending:
        return t('status.Pending');
      case ReviewState.AIReviewing:
        return t('status.AIReviewing');
      case ReviewState.HumanReview:
        return t('status.HumanReview');
      case ReviewState.Approved:
        return t('status.Approved');
      case ReviewState.Rejected:
        return t('status.Rejected');
      default:
        return status;
    }
  }, [t]);

  const getStatusColor = useCallback((status: string) => {
    switch (status) {
      case ReviewState.Approved:
        return 'bg-green-100 text-green-800';
      case ReviewState.Rejected:
        return 'bg-red-100 text-red-800';
      case ReviewState.AIReviewing:
        return 'bg-blue-100 text-blue-800';
      case ReviewState.HumanReview:
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-gray-100 text-gray-800';
    }
  }, []);

  // 仅在首次加载且没有任何数据时展示整页加载
  if (isLoading && !reviewsData) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (error) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 mb-4">
          <p>{t('reviews.loading_error')}</p>
        </div>
        <button 
          onClick={() => refetch()}
          className="btn btn-primary"
        >
          {t('projects.reload')}
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900 text-left">{t('reviews.title')}</h1>
          <p className="mt-1 text-gray-500 mt-2">
            {t('reviews.subtitle')}
          </p>
        </div>
        <div className="mt-4 sm:mt-0">
          <Link to="/reviews/new" className="btn btn-primary inline-flex items-center space-x-1">
            <PlusIcon className="h-5 w-5 mr-2" />
            {t('reviews.create')}
          </Link>
        </div>
      </div>

      {/* Filters */}
  <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 p-4">
        <div className="flex flex-col lg:flex-row lg:items-center gap-4">
          <div className="flex-1">
            <div className="relative">
              <MagnifyingGlassIcon className="h-5 w-5 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                placeholder={t('reviews.search_placeholder')}
                className="input pl-10"
                value={searchTerm}
                onChange={(e) => setSearchTerm(e.target.value)}
              />
            </div>
          </div>
          
          <div className="flex items-center space-x-4">
            <div className="flex items-center space-x-2">
              <FunnelIcon className="h-5 w-5 text-gray-400" />
              <select
                className="input min-w-[150px]"
                value={filters.status || ''}
                onChange={(e) => setFilters(prev => ({
                  ...prev,
                  status: e.target.value || undefined,
                  page: 1
                }))}
              >
                {statusOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
            {isFetching && reviewsData && (
              <div className="flex items-center text-gray-500 text-sm ml-auto">
                <svg className="animate-spin -ml-1 mr-2 h-4 w-4 text-primary-600" xmlns="http://www.w3.org/2000/svg" fill="none" viewBox="0 0 24 24">
                  <circle className="opacity-25" cx="12" cy="12" r="10" stroke="currentColor" strokeWidth="4"></circle>
                  <path className="opacity-75" fill="currentColor" d="M4 12a8 8 0 018-8v4a4 4 0 00-4 4H4z"></path>
                </svg>
                {t('reviews.updating')}
              </div>
            )}
          </div>
        </div>
      </div>

      {/* Reviews List */}
      {reviews.length === 0 ? (
        <div className="text-center py-12">
          <CpuChipIcon className="h-16 w-16 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            {searchTerm || filters.status ? t('reviews.no_match') : t('reviews.no_reviews')}
          </h3>
          <p className="text-gray-500 mb-6">
            {searchTerm || filters.status 
              ? t('reviews.try_adjust_filter')
              : t('reviews.no_reviews_desc')
            }
          </p>
          {!searchTerm && !filters.status && (
            <Link to="/reviews/new" className="btn btn-primary inline-flex items-center space-x-1">
              <PlusIcon className="h-5 w-5 mr-2" />
              {t('reviews.create')}
            </Link>
          )}
        </div>
      ) : (
        <div className="bg-white dark:bg-gray-900 rounded-lg border border-gray-200 dark:border-gray-800 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200 dark:divide-gray-800">
              <thead className="bg-gray-50 dark:bg-gray-800/50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    {t('reviews.table_info')}
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    {t('reviews.table_project')}
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    {t('reviews.table_status')}
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 dark:text-gray-400 uppercase tracking-wider">
                    {t('reviews.table_created')}
                  </th>
                  <th className="relative px-6 py-3">
                    <span className="sr-only">{t('reviews.table_actions')}</span>
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white dark:bg-gray-900 divide-y divide-gray-200 dark:divide-gray-800">
                {reviews.map((review: Review) => (
                  <tr key={review.id} className="row-hover-transition">
                    <td className="px-6 py-4">
                      <div>
                        <div className="text-sm font-medium text-gray-900 dark:text-gray-100 text-left">
                          <Link 
                            to={`/reviews/${review.id}`}
                            className="hover:text-primary-600"
                          >
                            {review.title}
                          </Link>
                        </div>
                        {review.description && (
                          <div className="text-sm text-gray-500 dark:text-gray-400 line-clamp-2 mt-1">
                            {review.description}
                          </div>
                        )}
                        <div className="flex items-center space-x-4 mt-2 text-xs text-gray-500 dark:text-gray-400">
                          <span>{t('reviews.branch')}: {review.branch}</span>
                          {review.pullRequestNumber && (
                            <span>PR #{review.pullRequestNumber}</span>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900 dark:text-gray-100">{review.projectName}</div>
                      <div className="text-sm text-gray-500 dark:text-gray-400">{t('reviews.by')} {review.authorName}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center">
                        {getStatusIcon(review.status)}
                        <span className={`ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(review.status)}`}>
                          {getStatusText(review.status)}
                        </span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500 dark:text-gray-400">
                      {new Date(review.createdAt).toLocaleDateString()}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <Link 
                        to={`/reviews/${review.id}`}
                        className="text-primary-600 hover:text-primary-700"
                      >
                        {t('reviews.view_details')}
                      </Link>
                    </td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      )}

      {/* Pagination */}
      {reviewsData && reviewsData.totalPages > 1 && (
        <div className="flex items-center justify-between">
          <div className="text-sm text-gray-500">
            {t('reviews.showing', { count: reviews.length, total: reviewsData.totalCount })}
          </div>
          <div className="flex items-center space-x-2">
            <button
              onClick={() => setFilters(prev => ({ ...prev, page: Math.max((prev.page || 1) - 1, 1) }))}
              disabled={(filters.page || 1) <= 1}
              className="btn btn-secondary disabled:opacity-50"
            >
              {t('reviews.prev_page')}
            </button>
            <span className="text-sm text-gray-700">
              {t('reviews.page', { current: filters.page || 1, total: reviewsData.totalPages })}
            </span>
            <button
              onClick={() => setFilters(prev => ({ ...prev, page: (prev.page || 1) + 1 }))}
              disabled={(filters.page || 1) >= reviewsData.totalPages}
              className="btn btn-secondary disabled:opacity-50"
            >
              {t('reviews.next_page')}
            </button>
          </div>
        </div>
      )}
    </div>
  );
};