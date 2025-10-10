import { useState } from 'react';
import { Link } from 'react-router-dom';
import { useQuery } from '@tanstack/react-query';
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
import type { Review, ReviewQueryParameters } from '../types/review';

export const ReviewsPage = () => {
  const [filters, setFilters] = useState<ReviewQueryParameters>({
    page: 1,
    pageSize: 20
  });
  const [searchTerm, setSearchTerm] = useState('');
  const [statusFilter, setStatusFilter] = useState<string>('');

  const {
    data: reviewsData,
    isLoading,
    error,
    refetch
  } = useQuery({
    queryKey: ['reviews', { ...filters, search: searchTerm, status: statusFilter }],
    queryFn: () => reviewService.getReviews({
      ...filters,
      // Add search and status filters when implemented
    }),
  });

  const reviews = reviewsData?.items || [];

  const statusOptions = [
    { value: '', label: '全部状态' },
    { value: ReviewState.Pending, label: '待处理' },
    { value: ReviewState.AIReviewing, label: 'AI评审中' },
    { value: ReviewState.HumanReview, label: '人工评审' },
    { value: ReviewState.Approved, label: '已通过' },
    { value: ReviewState.Rejected, label: '需修改' },
  ];

  const getStatusIcon = (status: string) => {
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
  };

  const getStatusText = (status: string) => {
    switch (status) {
      case ReviewState.Pending:
        return '待处理';
      case ReviewState.AIReviewing:
        return 'AI评审中';
      case ReviewState.HumanReview:
        return '人工评审';
      case ReviewState.Approved:
        return '已通过';
      case ReviewState.Rejected:
        return '需修改';
      default:
        return status;
    }
  };

  const getStatusColor = (status: string) => {
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
  };

  if (isLoading) {
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
          <p>加载评审记录时出错</p>
        </div>
        <button 
          onClick={() => refetch()}
          className="btn btn-primary"
        >
          重新加载
        </button>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      {/* Header */}
      <div className="flex flex-col sm:flex-row sm:items-center sm:justify-between">
        <div>
          <h1 className="text-2xl font-bold text-gray-900">代码评审</h1>
          <p className="mt-1 text-gray-500">
            查看和管理所有代码评审记录
          </p>
        </div>
        <div className="mt-4 sm:mt-0">
          <Link to="/reviews/new" className="btn btn-primary inline-flex items-center space-x-1">
            <PlusIcon className="h-5 w-5 mr-2" />
            新建评审
          </Link>
        </div>
      </div>

      {/* Filters */}
      <div className="bg-white rounded-lg border border-gray-200 p-4">
        <div className="flex flex-col lg:flex-row lg:items-center gap-4">
          <div className="flex-1">
            <div className="relative">
              <MagnifyingGlassIcon className="h-5 w-5 absolute left-3 top-1/2 transform -translate-y-1/2 text-gray-400" />
              <input
                type="text"
                placeholder="搜索评审标题或项目名称..."
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
                value={statusFilter}
                onChange={(e) => setStatusFilter(e.target.value)}
              >
                {statusOptions.map((option) => (
                  <option key={option.value} value={option.value}>
                    {option.label}
                  </option>
                ))}
              </select>
            </div>
          </div>
        </div>
      </div>

      {/* Reviews List */}
      {reviews.length === 0 ? (
        <div className="text-center py-12">
          <CpuChipIcon className="h-16 w-16 text-gray-400 mx-auto mb-4" />
          <h3 className="text-lg font-medium text-gray-900 mb-2">
            {searchTerm || statusFilter ? '未找到匹配的评审' : '还没有评审记录'}
          </h3>
          <p className="text-gray-500 mb-6">
            {searchTerm || statusFilter 
              ? '尝试调整搜索条件或筛选器'
              : '创建第一个评审任务开始使用AI代码评审'
            }
          </p>
          {!searchTerm && !statusFilter && (
            <Link to="/reviews/new" className="btn btn-primary inline-flex items-center space-x-1">
              <PlusIcon className="h-5 w-5 mr-2" />
              创建评审
            </Link>
          )}
        </div>
      ) : (
        <div className="bg-white rounded-lg border border-gray-200 overflow-hidden">
          <div className="overflow-x-auto">
            <table className="min-w-full divide-y divide-gray-200">
              <thead className="bg-gray-50">
                <tr>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    评审信息
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    项目
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    状态
                  </th>
                  <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wider">
                    创建时间
                  </th>
                  <th className="relative px-6 py-3">
                    <span className="sr-only">操作</span>
                  </th>
                </tr>
              </thead>
              <tbody className="bg-white divide-y divide-gray-200">
                {reviews.map((review: Review) => (
                  <tr key={review.id} className="hover:bg-gray-50">
                    <td className="px-6 py-4">
                      <div>
                        <div className="text-sm font-medium text-gray-900">
                          <Link 
                            to={`/reviews/${review.id}`}
                            className="hover:text-primary-600"
                          >
                            {review.title}
                          </Link>
                        </div>
                        {review.description && (
                          <div className="text-sm text-gray-500 line-clamp-2 mt-1">
                            {review.description}
                          </div>
                        )}
                        <div className="flex items-center space-x-4 mt-2 text-xs text-gray-500">
                          <span>分支: {review.branch}</span>
                          {review.pullRequestNumber && (
                            <span>PR #{review.pullRequestNumber}</span>
                          )}
                        </div>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="text-sm text-gray-900">{review.projectName}</div>
                      <div className="text-sm text-gray-500">by {review.authorName}</div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap">
                      <div className="flex items-center">
                        {getStatusIcon(review.status)}
                        <span className={`ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${getStatusColor(review.status)}`}>
                          {getStatusText(review.status)}
                        </span>
                      </div>
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-sm text-gray-500">
                      {new Date(review.createdAt).toLocaleDateString('zh-CN')}
                    </td>
                    <td className="px-6 py-4 whitespace-nowrap text-right text-sm font-medium">
                      <Link 
                        to={`/reviews/${review.id}`}
                        className="text-primary-600 hover:text-primary-700"
                      >
                        查看详情
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
            显示 {reviews.length} 条记录，共 {reviewsData.totalCount} 条
          </div>
          <div className="flex items-center space-x-2">
            <button
              onClick={() => setFilters(prev => ({ ...prev, page: Math.max((prev.page || 1) - 1, 1) }))}
              disabled={(filters.page || 1) <= 1}
              className="btn btn-secondary disabled:opacity-50"
            >
              上一页
            </button>
            <span className="text-sm text-gray-700">
              第 {filters.page || 1} 页，共 {reviewsData.totalPages} 页
            </span>
            <button
              onClick={() => setFilters(prev => ({ ...prev, page: (prev.page || 1) + 1 }))}
              disabled={(filters.page || 1) >= reviewsData.totalPages}
              className="btn btn-secondary disabled:opacity-50"
            >
              下一页
            </button>
          </div>
        </div>
      )}
    </div>
  );
};