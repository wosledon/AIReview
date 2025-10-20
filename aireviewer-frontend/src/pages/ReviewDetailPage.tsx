import { useState, useEffect, useCallback } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { useTranslation } from 'react-i18next';
import { 
  ArrowLeftIcon,
  ChatBubbleLeftRightIcon,
  ExclamationTriangleIcon,
  CheckCircleIcon,
  XCircleIcon,
  ClockIcon,
  CpuChipIcon,
  EyeIcon,
  PlusIcon,
  DocumentTextIcon,
  BugAntIcon,
  ShieldCheckIcon,
  BoltIcon
} from '@heroicons/react/24/outline';
import { reviewService } from '../services/review.service';
import { analysisService } from '../services/analysis.service';
import { useNotifications } from '../hooks/useNotifications';
import { LazyDiffViewer } from '../components/LazyDiffViewer';
import { ReviewState, ReviewCommentSeverity, ReviewCommentCategory } from '../types/review';
import type { Review, ReviewComment, AddCommentRequest, RejectReviewRequest } from '../types/review';
import type { AnalysisData, RiskAssessment, ImprovementSuggestion, PullRequestChangeSummary } from '../types/analysis';
import { getRiskLevel } from '../types/analysis';
import { LoadingSpinner, LoadingCard, EmptyState, ErrorState } from '../components/common/LoadingStates';
import { ProgressBar } from '../components/common/ProgressBar';
import { AnalysisCard, MetricGrid, Tag } from '../components/common/AnalysisCard';
import { RiskDonutChart } from '../components/common/Charts';
import { AnalysisDashboard } from '../components/common/AnalysisDashboard';

export const ReviewDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { joinGroup, leaveGroup, addNotification } = useNotifications();
  const { t } = useTranslation();
  const [activeTab, setActiveTab] = useState<'overview' | 'comments' | 'diff' | 'analysis'>('overview');
  const [showAddComment, setShowAddComment] = useState(false);
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  const [rejectReason, setRejectReason] = useState('');
  
  // 状态用于跳转到特定文件和行
  const [targetFileAndLine, setTargetFileAndLine] = useState<{ filePath: string; lineNumber: number } | null>(null);

  const reviewId = parseInt(id!, 10);

  // Join review group for real-time notifications
  useEffect(() => {
    if (reviewId) {
      joinGroup(`review_${reviewId}`);
      return () => {
        leaveGroup(`review_${reviewId}`);
      };
    }
  }, [reviewId, joinGroup, leaveGroup]);

  const {
    data: review,
    isLoading: isReviewLoading,
    error: reviewError
  } = useQuery({
    queryKey: ['review', reviewId],
    queryFn: () => reviewService.getReview(reviewId),
    enabled: !!reviewId,
  });

  const {
    data: comments,
    isLoading: isCommentsLoading
  } = useQuery({
    queryKey: ['review-comments', reviewId],
    queryFn: () => reviewService.getReviewComments(reviewId),
    enabled: !!reviewId,
  });

  const addCommentMutation = useMutation({
    mutationFn: (comment: AddCommentRequest) => reviewService.addComment(reviewId, comment),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['review-comments', reviewId] });
      setShowAddComment(false);
    }
  });

  const approveReviewMutation = useMutation({
    mutationFn: () => reviewService.approveReview(reviewId),
    onSuccess: (updatedReview) => {
      queryClient.setQueryData(['review', reviewId], updatedReview);
      queryClient.invalidateQueries({ queryKey: ['reviews'] });
      addNotification({
        type: 'review_status',
        message: t('reviewDetail.notifications.approved'),
        timestamp: new Date().toISOString(),
        reviewId: String(reviewId)
      });
    },
    onError: (error: unknown) => {
      const msg = error instanceof Error ? error.message : t('reviewDetail.notifications.approve_failed');
      addNotification({
        type: 'review_status',
        message: msg,
        timestamp: new Date().toISOString(),
        reviewId: String(reviewId)
      });
    }
  });

  const rejectReviewMutation = useMutation({
    mutationFn: (request: RejectReviewRequest) => reviewService.rejectReview(reviewId, request),
    onSuccess: (updatedReview) => {
      queryClient.setQueryData(['review', reviewId], updatedReview);
      queryClient.invalidateQueries({ queryKey: ['reviews'] });
      queryClient.invalidateQueries({ queryKey: ['review-comments', reviewId] });
      setShowRejectDialog(false);
      setRejectReason('');
      addNotification({
        type: 'review_status',
        message: '评审已拒绝',
        timestamp: new Date().toISOString(),
        reviewId: String(reviewId)
      });
    },
    onError: (error: unknown) => {
      const msg = error instanceof Error ? error.message : '拒绝评审失败';
      addNotification({
        type: 'review_status',
        message: msg,
        timestamp: new Date().toISOString(),
        reviewId: String(reviewId)
      });
    }
  });

  const handleApproveReview = () => {
    approveReviewMutation.mutate();
  };

  const handleRejectReview = () => {
    setShowRejectDialog(true);
  };

  const handleConfirmReject = () => {
    rejectReviewMutation.mutate({ reason: rejectReason.trim() || undefined });
  };

  const startAIReviewMutation = useMutation({
    mutationFn: () => reviewService.startAIReview(reviewId),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['review', reviewId] });
      queryClient.invalidateQueries({ queryKey: ['reviews'] });
      addNotification({
        type: 'review_status',
        message: t('reviewDetail.notifications.ai_started'),
        timestamp: new Date().toISOString(),
        reviewId: String(reviewId)
      });
    },
    onError: (error: unknown) => {
      const msg = error instanceof Error ? error.message : t('reviewDetail.notifications.ai_failed');
      addNotification({
        type: 'review_status',
        message: msg,
        timestamp: new Date().toISOString(),
        reviewId: String(reviewId)
      });
    }
  });

  const handleStartAIReview = () => {
    startAIReviewMutation.mutate();
  };

  // 跳转到代码变更并定位到特定行
  const handleJumpToCode = (filePath: string, lineNumber: number) => {
    setTargetFileAndLine({ filePath, lineNumber });
    setActiveTab('diff');
    // 给DiffViewer一点时间渲染，然后滚动到目标位置
    setTimeout(() => {
      const element = document.getElementById(`line-${lineNumber}`);
      if (element) {
        element.scrollIntoView({ behavior: 'smooth', block: 'center' });
        // 添加高亮效果
        element.classList.add('highlight-flash');
        setTimeout(() => element.classList.remove('highlight-flash'), 2000);
      }
    }, 100);
  };

  if (isReviewLoading) {
    return (
      <div className="flex items-center justify-center h-64">
        <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  if (reviewError || !review) {
    return (
      <div className="text-center py-12">
        <div className="text-red-600 dark:text-red-400 mb-4">
          <p>{t('reviewDetail.loading_error')}</p>
        </div>
        <button 
          onClick={() => navigate('/reviews')}
          className="btn btn-primary"
        >
          {t('reviewDetail.back_to_reviews')}
        </button>
      </div>
    );
  }

  const getStatusIcon = (status: string) => {
    switch (status) {
      case ReviewState.Approved:
        return <CheckCircleIcon className="h-6 w-6 text-green-500" />;
      case ReviewState.Rejected:
        return <XCircleIcon className="h-6 w-6 text-red-500" />;
      case ReviewState.AIReviewing:
        return <CpuChipIcon className="h-6 w-6 text-blue-500" />;
      case ReviewState.HumanReview:
        return <EyeIcon className="h-6 w-6 text-orange-500" />;
      default:
        return <ClockIcon className="h-6 w-6 text-gray-500" />;
    }
  };

  const getStatusText = (status: string) => {
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
  };

  const tabs = [
    { id: 'overview', name: t('reviewDetail.tabs.overview'), icon: EyeIcon },
    { id: 'comments', name: t('reviewDetail.tabs.comments'), icon: ChatBubbleLeftRightIcon, count: comments?.length || 0 },
    { id: 'diff', name: t('reviewDetail.tabs.diff'), icon: DocumentTextIcon },
    { id: 'analysis', name: t('reviewDetail.tabs.analysis'), icon: CpuChipIcon },
  ] as const;

  return (
    <div className="space-y-6 fade-in">
      {/* Reject Dialog */}
      {showRejectDialog && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 dark:bg-gray-900/70 overflow-y-auto h-full w-full z-50 flex items-center justify-center">
          <div className="relative p-6 border w-96 shadow-lg rounded-md bg-white dark:bg-gray-900 dark:border-gray-700">
            <div className="mt-3">
              <h3 className="text-lg font-bold text-gray-900 dark:text-gray-100 mb-4">{t('reviewDetail.reject.title')}</h3>
              <div className="mb-4">
                <label htmlFor="reject-reason" className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  {t('reviewDetail.reject.reason_label')}
                </label>
                <textarea
                  id="reject-reason"
                  rows={4}
                  className="w-full px-3 py-2 text-gray-700 dark:text-gray-200 border dark:border-gray-600 rounded-lg focus:outline-none focus:border-primary-500 bg-white dark:bg-gray-800"
                  placeholder={t('reviewDetail.reject.reason_placeholder')}
                  value={rejectReason}
                  onChange={(e) => setRejectReason(e.target.value)}
                />
              </div>
              <div className="flex items-center justify-end space-x-3">
                <button
                  type="button"
                  className="btn btn-secondary"
                  onClick={() => {
                    setShowRejectDialog(false);
                    setRejectReason('');
                  }}
                  disabled={rejectReviewMutation.isPending}
                >
                  {t('reviewDetail.reject.cancel')}
                </button>
                <button
                  type="button"
                  className="btn btn-danger"
                  onClick={handleConfirmReject}
                  disabled={rejectReviewMutation.isPending}
                >
                  {rejectReviewMutation.isPending ? t('reviewDetail.reject.processing') : t('reviewDetail.reject.confirm')}
                </button>
              </div>
            </div>
          </div>
        </div>
      )}

      {/* Header */}
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-4">
          <button
            onClick={() => navigate('/reviews')}
            className="p-2 text-gray-400 hover:text-gray-600 dark:text-gray-300 dark:hover:text-gray-100 rounded-lg hover:bg-gray-100 dark:hover:bg-gray-800 transition-colors"
          >
            <ArrowLeftIcon className="h-5 w-5" />
          </button>
          <div>
            <div className="flex items-center space-x-3">
              <h1 className="text-2xl font-bold text-gray-900 dark:text-gray-100">{review.title}</h1>
              <div className="flex items-center">
                {getStatusIcon(review.status)}
                <span className="ml-2 text-sm font-medium text-gray-700 dark:text-gray-300">
                  {getStatusText(review.status)}
                </span>
              </div>
            </div>
            {review.description && (
              <p className="text-gray-500 dark:text-gray-400 mt-1">{review.description}</p>
            )}
            <div className="flex items-center space-x-4 mt-2 text-sm text-gray-500 dark:text-gray-400">
              <span>{t('reviewDetail.overview.labels.project')}: {review.projectName}</span>
              <span>{t('reviewDetail.overview.labels.branch')}: {review.branch}</span>
              <span>{t('reviewDetail.overview.labels.author')}: {review.authorName}</span>
              {review.pullRequestNumber && (
                <span>PR #{review.pullRequestNumber}</span>
              )}
            </div>
          </div>
        </div>

        <div className="flex items-center space-x-3">
          {review.status === ReviewState.Pending && (
            <button 
              className="btn btn-primary inline-flex items-center space-x-1 transition-all hover:scale-105"
              onClick={handleStartAIReview}
              disabled={startAIReviewMutation.isPending}
            >
              <CpuChipIcon className="h-5 w-5 mr-2" />
              {startAIReviewMutation.isPending ? 'AI 评审启动中...' : '开始AI评审'}
            </button>
          )}
          {review.status === ReviewState.HumanReview && (
            <div className="flex space-x-2">
              <button 
                className="btn btn-danger"
                onClick={handleRejectReview}
                disabled={rejectReviewMutation.isPending}
              >
                {rejectReviewMutation.isPending ? '处理中...' : '拒绝'}
              </button>
              <button 
                className="btn btn-primary"
                onClick={handleApproveReview}
                disabled={approveReviewMutation.isPending}
              >
                {approveReviewMutation.isPending ? '处理中...' : '通过'}
              </button>
            </div>
          )}
        </div>
      </div>

      {/* Tabs */}
      <div className="border-b border-gray-200 dark:border-gray-800">
        <nav className="-mb-px flex space-x-8">
          {tabs.map((tab) => {
            const Icon = tab.icon;
            return (
              <button
                key={tab.id}
                onClick={() => setActiveTab(tab.id)}
                className={`${
                  activeTab === tab.id
                    ? 'border-primary-500 text-primary-600'
                    : 'border-transparent text-gray-500 dark:text-gray-400 hover:text-gray-700 dark:hover:text-gray-200 hover:border-gray-300 dark:hover:border-gray-700'
                } whitespace-nowrap py-2 px-1 border-b-2 font-medium text-sm flex items-center space-x-2 transition-colors`}
              >
                <Icon className="h-5 w-5" />
                <span>{tab.name}</span>
                {'count' in tab && tab.count > 0 && (
                  <span className="bg-gray-100 dark:bg-gray-700 text-gray-600 dark:text-gray-300 py-0.5 px-2 rounded-full text-xs">
                    {tab.count}
                  </span>
                )}
              </button>
            );
          })}
        </nav>
      </div>

      {/* Tab Content */}
      <div className="mt-6">
        {activeTab === 'overview' && (
          <OverviewTab 
            review={review} 
            comments={comments || []} 
            getStatusText={getStatusText}
            onApproveReview={handleApproveReview}
            onRejectReview={handleRejectReview}
            isApproving={approveReviewMutation.isPending}
            isRejecting={rejectReviewMutation.isPending}
          />
        )}
        {activeTab === 'comments' && (
          <CommentsTab 
            comments={comments || []} 
            isLoading={isCommentsLoading}
            showAddComment={showAddComment}
            onShowAddComment={setShowAddComment}
            onAddComment={(comment) => addCommentMutation.mutate(comment)}
            isAddingComment={addCommentMutation.isPending}
            onJumpToCode={handleJumpToCode}
          />
        )}
        {activeTab === 'diff' && <DiffTab review={review} targetFileAndLine={targetFileAndLine} />}
        {activeTab === 'analysis' && <AnalysisTab review={review} />}
      </div>
    </div>
  );
};

interface OverviewTabProps {
  review: Review;
  comments: ReviewComment[];
  getStatusText: (status: string) => string;
  onApproveReview: () => void;
  onRejectReview: () => void;
  isApproving: boolean;
  isRejecting: boolean;
}

const OverviewTab = ({ review, comments, getStatusText, onApproveReview, onRejectReview, isApproving, isRejecting }: OverviewTabProps) => {
  const aiComments = comments.filter(c => c.isAIGenerated);
  const humanComments = comments.filter(c => !c.isAIGenerated);

  const getSeverityIcon = (severity: string) => {
    switch (severity) {
      case ReviewCommentSeverity.Critical:
        return <ExclamationTriangleIcon className="h-5 w-5 text-red-500" />;
      case ReviewCommentSeverity.Error:
        return <XCircleIcon className="h-5 w-5 text-red-500" />;
      case ReviewCommentSeverity.Warning:
        return <ExclamationTriangleIcon className="h-5 w-5 text-orange-500" />;
      default:
        return <CheckCircleIcon className="h-5 w-5 text-blue-500" />;
    }
  };

  const getCategoryIcon = (category: string) => {
    switch (category) {
      case ReviewCommentCategory.Security:
        return <ShieldCheckIcon className="h-4 w-4" />;
      case ReviewCommentCategory.Performance:
        return <BoltIcon className="h-4 w-4" />;
      case ReviewCommentCategory.Quality:
        return <CheckCircleIcon className="h-4 w-4" />;
      default:
        return <BugAntIcon className="h-4 w-4" />;
    }
  };

  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case ReviewCommentSeverity.Critical:
        return 'bg-red-100 text-red-800';
      case ReviewCommentSeverity.Error:
        return 'bg-red-100 text-red-800';
      case ReviewCommentSeverity.Warning:
        return 'bg-orange-100 text-orange-800';
      default:
        return 'bg-blue-100 text-blue-800';
    }
  };

  return (
    <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
      {/* Main Content */}
      <div className="lg:col-span-2 space-y-6">
        {/* AI Review Summary */}
        {aiComments.length > 0 && (
          <div className="card dark:bg-gray-900 dark:border-gray-800 transform transition-all duration-300 hover:shadow-lg">
            <div className="flex items-center mb-4">
              <CpuChipIcon className="h-6 w-6 text-blue-500 mr-3" />
              <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">AI评审总结</h3>
            </div>
            
            <div className="space-y-4">
              <div className="bg-blue-50 dark:bg-blue-900/20 border border-blue-200 dark:border-blue-800 rounded-lg p-4">
                <p className="text-blue-800 dark:text-blue-300">
                  AI分析发现了 {aiComments.length} 个问题，建议重点关注安全性和代码质量问题。
                </p>
              </div>

              <div className="space-y-3">
                {aiComments.slice(0, 3).map((comment) => (
                  <div key={comment.id} className="flex items-start space-x-3 p-3 bg-gray-50 dark:bg-gray-800/50 rounded-lg row-hover-transition">
                    {getSeverityIcon(comment.severity)}
                    <div className="flex-1">
                      <div className="flex items-center space-x-2 mb-1">
                        <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${getSeverityColor(comment.severity)}`}>
                          {comment.severity}
                        </span>
                        <span className="text-xs text-gray-500 dark:text-gray-400 flex items-center">
                          {getCategoryIcon(comment.category)}
                          <span className="ml-1">{comment.category}</span>
                        </span>
                      </div>
                      {comment.filePath && (
                        <p className="text-xs text-gray-500 dark:text-gray-400 text-left mt-3">
                          {comment.filePath}:{comment.lineNumber}
                        </p>
                      )}
                      <p className="text-sm text-gray-900 dark:text-gray-100 text-left mt-1">{comment.content}</p>
                    </div>
                  </div>
                ))}
              </div>

              {aiComments.length > 3 && (
                <div className="text-center">
                  <button className="text-primary-600 hover:text-primary-700 text-sm font-medium transition-colors">
                    查看全部 {aiComments.length} 个AI评审意见
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Human Comments */}
        {humanComments.length > 0 && (
          <div className="card dark:bg-gray-900 dark:border-gray-800 transform transition-all duration-300 hover:shadow-lg">
            <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">人工评审</h3>
            <div className="space-y-3">
              {humanComments.map((comment) => (
                <div key={comment.id} className="flex items-start space-x-3 p-3 border border-gray-200 dark:border-gray-700 rounded-lg row-hover-transition">
                  <div className="h-8 w-8 rounded-full bg-primary-100 dark:bg-primary-900/30 flex items-center justify-center">
                    <span className="text-primary-600 dark:text-primary-400 font-medium text-sm">
                      {comment.authorName.charAt(0).toUpperCase()}
                    </span>
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center space-x-2 mb-1">
                      <span className="text-sm font-medium text-gray-900 dark:text-gray-100">{comment.authorName}</span>
                      <span className="text-xs text-gray-500 dark:text-gray-400">
                        {new Date(comment.createdAt).toLocaleString('zh-CN')}
                      </span>
                    </div>
                    {comment.filePath && (
                        <p className="text-xs text-gray-500 dark:text-gray-400 text-left mt-3">
                          {comment.filePath}:{comment.lineNumber}
                        </p>
                      )}
                    <p className="text-sm text-gray-900 dark:text-gray-100 text-left mt-1">{comment.content}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* No Comments */}
        {comments.length === 0 && (
          <div className="card dark:bg-gray-900 dark:border-gray-800 text-center py-8 transform transition-all duration-300 hover:shadow-lg">
            <ChatBubbleLeftRightIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <p className="text-gray-500 dark:text-gray-400">暂无评审意见</p>
            <p className="text-sm text-gray-400 dark:text-gray-500 mt-1">
              {review.status === ReviewState.Pending ? '等待AI评审完成' : '开始添加评审意见'}
            </p>
          </div>
        )}
      </div>

      {/* Sidebar */}
      <div className="space-y-6">
        {/* Review Info */}
        <div className="card dark:bg-gray-900 dark:border-gray-800 transform transition-all duration-300 hover:shadow-lg">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">评审信息</h3>
          <dl className="space-y-3">
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">状态</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{getStatusText(review.status)}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">创建时间</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
                {new Date(review.createdAt).toLocaleString('zh-CN')}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">更新时间</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">
                {new Date(review.updatedAt).toLocaleString('zh-CN')}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500 dark:text-gray-400">基础分支</dt>
              <dd className="mt-1 text-sm text-gray-900 dark:text-gray-100">{review.baseBranch}</dd>
            </div>
          </dl>
        </div>

        {/* Stats */}
        <div className="card dark:bg-gray-900 dark:border-gray-800 transform transition-all duration-300 hover:shadow-lg">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">统计信息</h3>
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500 dark:text-gray-400">总评论数</span>
              <span className="text-sm font-semibold text-gray-900 dark:text-gray-100">{comments.length}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500 dark:text-gray-400">AI评论</span>
              <span className="text-sm font-semibold text-blue-600">{aiComments.length}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500 dark:text-gray-400">人工评论</span>
              <span className="text-sm font-semibold text-green-600">{humanComments.length}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500 dark:text-gray-400">严重问题</span>
              <span className="text-sm font-semibold text-red-600">
                {comments.filter(c => c.severity === ReviewCommentSeverity.Critical || c.severity === ReviewCommentSeverity.Error).length}
              </span>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="card dark:bg-gray-900 dark:border-gray-800 transform transition-all duration-300 hover:shadow-lg">
          <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100 mb-4">快速操作</h3>
          <div className="space-y-3">
            <Link
              to={`/projects/${review.projectId}`}
              className="btn btn-secondary w-full inline-flex items-center space-x-1 transition-all hover:scale-105"
            >
              <EyeIcon className="h-5 w-5 mr-2" />
              查看项目
            </Link>
            <button className="btn btn-secondary w-full inline-flex items-center space-x-1 transition-all hover:scale-105">
              <ChatBubbleLeftRightIcon className="h-5 w-5 mr-2" />
              添加评论
            </button>
            {review.status === ReviewState.HumanReview && (
              <>
                <button 
                  className="btn btn-primary w-full transition-all hover:scale-105"
                  onClick={onApproveReview}
                  disabled={isApproving}
                >
                  {isApproving ? '处理中...' : '通过评审'}
                </button>
                <button 
                  className="btn btn-danger w-full transition-all hover:scale-105"
                  onClick={onRejectReview}
                  disabled={isRejecting}
                >
                  {isRejecting ? '处理中...' : '拒绝并要求修改'}
                </button>
              </>
            )}
          </div>
        </div>
      </div>
    </div>
  );
};

interface CommentsTabProps {
  comments: ReviewComment[];
  isLoading: boolean;
  showAddComment: boolean;
  onShowAddComment: (show: boolean) => void;
  onAddComment: (comment: AddCommentRequest) => void;
  isAddingComment: boolean;
  onJumpToCode?: (filePath: string, lineNumber: number) => void;
}

const CommentsTab = ({ comments, isLoading, showAddComment, onShowAddComment, onAddComment, isAddingComment, onJumpToCode }: CommentsTabProps) => {
  const [newComment, setNewComment] = useState<{
    content: string;
    severity: ReviewCommentSeverity;
    category: ReviewCommentCategory;
    filePath: string;
    lineNumber: number | undefined;
    suggestion: string;
  }>({
    content: '',
    severity: ReviewCommentSeverity.Info,
    category: ReviewCommentCategory.Quality,
    filePath: '',
    lineNumber: undefined,
    suggestion: ''
  });

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (!newComment.content.trim()) return;
    
    onAddComment({
      content: newComment.content,
      severity: newComment.severity,
      category: newComment.category,
      filePath: newComment.filePath || undefined,
      lineNumber: newComment.lineNumber,
      suggestion: newComment.suggestion || undefined
    });
    
    setNewComment({
      content: '',
      severity: ReviewCommentSeverity.Info,
      category: ReviewCommentCategory.Quality,
      filePath: '',
      lineNumber: undefined,
      suggestion: ''
    });
  };

  if (isLoading) {
    return (
      <div className="flex items-center justify-center h-32">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
      </div>
    );
  }

  return (
    <div className="space-y-6 fade-in">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">
          评审意见 ({comments.length})
        </h3>
        <button
          onClick={() => onShowAddComment(!showAddComment)}
          className="btn btn-primary inline-flex items-center space-x-1 transition-all hover:scale-105"
        >
          <PlusIcon className="h-5 w-5 mr-2" />
          添加评论
        </button>
      </div>

      {/* Add Comment Form */}
      {showAddComment && (
        <div className="card dark:bg-gray-900 dark:border-gray-800 transform transition-all duration-300">
          <h4 className="text-md font-semibold text-gray-900 dark:text-gray-100 mb-4">添加评审意见</h4>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                评论内容 *
              </label>
              <textarea
                rows={4}
                className="input resize-none dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                placeholder="请输入您的评审意见..."
                value={newComment.content}
                onChange={(e) => setNewComment(prev => ({ ...prev, content: e.target.value }))}
                required
                disabled={isAddingComment}
              />
            </div>
            
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  严重程度
                </label>
                <select
                  className="input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                  value={newComment.severity}
                  onChange={(e) => setNewComment(prev => ({ ...prev, severity: e.target.value as ReviewCommentSeverity }))}
                  disabled={isAddingComment}
                >
                  <option value={ReviewCommentSeverity.Info}>信息</option>
                  <option value={ReviewCommentSeverity.Warning}>警告</option>
                  <option value={ReviewCommentSeverity.Error}>错误</option>
                  <option value={ReviewCommentSeverity.Critical}>严重</option>
                </select>
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  分类
                </label>
                <select
                  className="input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                  value={newComment.category}
                  onChange={(e) => setNewComment(prev => ({ ...prev, category: e.target.value as ReviewCommentCategory }))}
                  disabled={isAddingComment}
                >
                  <option value={ReviewCommentCategory.Quality}>代码质量</option>
                  <option value={ReviewCommentCategory.Security}>安全性</option>
                  <option value={ReviewCommentCategory.Performance}>性能</option>
                  <option value={ReviewCommentCategory.Style}>代码风格</option>
                  <option value={ReviewCommentCategory.Documentation}>文档</option>
                </select>
              </div>
            </div>

            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  文件路径
                </label>
                <input
                  type="text"
                  className="input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                  placeholder="src/components/Example.tsx"
                  value={newComment.filePath}
                  onChange={(e) => setNewComment(prev => ({ ...prev, filePath: e.target.value }))}
                  disabled={isAddingComment}
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                  行号
                </label>
                <input
                  type="number"
                  className="input dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                  placeholder="123"
                  value={newComment.lineNumber || ''}
                  onChange={(e) => setNewComment(prev => ({ ...prev, lineNumber: e.target.value ? parseInt(e.target.value) : undefined }))}
                  disabled={isAddingComment}
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 dark:text-gray-300 mb-2">
                建议修改
              </label>
              <textarea
                rows={3}
                className="input resize-none dark:bg-gray-800 dark:border-gray-700 dark:text-gray-100"
                placeholder="请提供具体的修改建议（可选）"
                value={newComment.suggestion}
                onChange={(e) => setNewComment(prev => ({ ...prev, suggestion: e.target.value }))}
                disabled={isAddingComment}
              />
            </div>

            <div className="flex items-center justify-end space-x-3">
              <button
                type="button"
                onClick={() => onShowAddComment(false)}
                className="btn btn-secondary transition-all hover:scale-105"
                disabled={isAddingComment}
              >
                取消
              </button>
              <button
                type="submit"
                className="btn btn-primary transition-all hover:scale-105"
                disabled={isAddingComment || !newComment.content.trim()}
              >
                {isAddingComment ? '提交中...' : '提交评论'}
              </button>
            </div>
          </form>
        </div>
      )}

      {/* Comments List */}
      {comments.length === 0 ? (
        <div className="card dark:bg-gray-900 dark:border-gray-800 text-center py-8 transform transition-all duration-300 hover:shadow-lg">
          <ChatBubbleLeftRightIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <p className="text-gray-500 dark:text-gray-400">暂无评审意见</p>
          <button
            onClick={() => onShowAddComment(true)}
            className="btn btn-primary mt-4 transition-all hover:scale-105"
          >
            添加第一个评论
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {comments.map((comment, index) => (
            <div 
              key={comment.id} 
              className="animate-fade-in" 
              style={{ animationDelay: `${index * 50}ms` }}
            >
              <CommentCard comment={comment} onJumpToCode={onJumpToCode} />
            </div>
          ))}
        </div>
      )}
    </div>
  );
};

interface CommentCardProps {
  comment: ReviewComment;
  onJumpToCode?: (filePath: string, lineNumber: number) => void;
}

const CommentCard = ({ comment, onJumpToCode }: CommentCardProps) => {
  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case ReviewCommentSeverity.Critical:
        return 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/20 dark:text-red-400 dark:border-red-800';
      case ReviewCommentSeverity.Error:
        return 'bg-red-100 text-red-800 border-red-200 dark:bg-red-900/20 dark:text-red-400 dark:border-red-800';
      case ReviewCommentSeverity.Warning:
        return 'bg-orange-100 text-orange-800 border-orange-200 dark:bg-orange-900/20 dark:text-orange-400 dark:border-orange-800';
      default:
        return 'bg-blue-100 text-blue-800 border-blue-200 dark:bg-blue-900/20 dark:text-blue-400 dark:border-blue-800';
    }
  };

  return (
    <div className={`border rounded-lg p-4 transition-all duration-300 hover:shadow-lg hover:-translate-y-1 ${
      comment.isAIGenerated 
        ? 'bg-blue-50 border-blue-200 dark:bg-blue-900/20 dark:border-blue-800' 
        : 'bg-white border-gray-200 dark:bg-gray-900 dark:border-gray-800'
    }`}>
      <div className="flex items-start space-x-3">
        <div className={`h-8 w-8 rounded-full flex items-center justify-center transition-colors ${
          comment.isAIGenerated 
            ? 'bg-blue-100 dark:bg-blue-900/40' 
            : 'bg-primary-100 dark:bg-primary-900/40'
        }`}>
          {comment.isAIGenerated ? (
            <CpuChipIcon className="h-5 w-5 text-blue-600 dark:text-blue-400" />
          ) : (
            <span className="text-primary-600 dark:text-primary-400 font-medium text-sm">
              {comment.authorName.charAt(0).toUpperCase()}
            </span>
          )}
        </div>
        
        <div className="flex-1">
          <div className="flex items-center space-x-2 mb-2">
            <span className="text-sm font-medium text-gray-900 dark:text-gray-100">
              {comment.isAIGenerated ? 'AI评审' : comment.authorName}
            </span>
            <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium transition-colors ${getSeverityColor(comment.severity)}`}>
              {comment.severity}
            </span>
            <span className="text-xs text-gray-500 dark:text-gray-400">{comment.category}</span>
            <span className="text-xs text-gray-500 dark:text-gray-400">
              {new Date(comment.createdAt).toLocaleString('zh-CN')}
            </span>
          </div>
          
          {comment.filePath && (
            <button
              onClick={() => onJumpToCode && comment.lineNumber && onJumpToCode(comment.filePath!, comment.lineNumber)}
              disabled={!onJumpToCode || !comment.lineNumber}
              className={`text-xs mb-2 flex items-center space-x-1 group ${
                onJumpToCode && comment.lineNumber
                  ? 'text-blue-600 dark:text-blue-400 hover:text-blue-700 dark:hover:text-blue-300 cursor-pointer'
                  : 'text-gray-500 dark:text-gray-400 cursor-default'
              }`}
              title={onJumpToCode && comment.lineNumber ? '点击跳转到代码位置' : undefined}
            >
              <DocumentTextIcon className="h-4 w-4" />
              <span className="font-mono">
                {comment.filePath}
                {comment.lineNumber && `:${comment.lineNumber}`}
              </span>
              {onJumpToCode && comment.lineNumber && (
                <span className="opacity-0 group-hover:opacity-100 transition-opacity">→</span>
              )}
            </button>
          )}
          
          <p className="text-sm text-gray-900 dark:text-gray-100 mb-3">{comment.content}</p>
          
          {comment.suggestion && (
            <div className="bg-gray-50 dark:bg-gray-800 border border-gray-200 dark:border-gray-700 rounded p-3 transition-colors">
              <div className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-1">💡 建议修改:</div>
              <p className="text-sm text-gray-700 dark:text-gray-300">{comment.suggestion}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

interface DiffTabProps {
  review: Review;
  targetFileAndLine?: { filePath: string; lineNumber: number } | null;
}

const DiffTab = ({ review, targetFileAndLine }: DiffTabProps) => {
  const { addNotification } = useNotifications();
  const queryClient = useQueryClient();
  
  // 使用轻量级文件列表API（替代完整diff）
  const { data: fileListData, isLoading: isFileListLoading, error: fileListError } = useQuery({
    queryKey: ['review-diff-files', review.id],
    queryFn: () => reviewService.getReviewDiffFileList(review.id),
  });

  const addLineComment = useMutation({
    mutationFn: async (payload: { filePath: string; lineNumber: number; content: string }) => {
      const request: AddCommentRequest = {
        content: payload.content,
        filePath: payload.filePath,
        lineNumber: payload.lineNumber,
        severity: ReviewCommentSeverity.Info,
        category: ReviewCommentCategory.Quality,
      };
      return reviewService.addComment(review.id, request);
    },
    onSuccess: () => {
      addNotification({
        type: 'review_comment',
        message: '评论已添加',
        timestamp: new Date().toISOString(),
        reviewId: String(review.id),
        content: '已添加行级评论'
      });
      queryClient.invalidateQueries({ queryKey: ['review-diff', review.id] });
      queryClient.invalidateQueries({ queryKey: ['review-comments', review.id] });
    },
    onError: (err: unknown) => {
      const msg = err instanceof Error ? err.message : '添加评论失败';
      addNotification({
        type: 'review_comment',
        message: msg,
        timestamp: new Date().toISOString(),
        reviewId: String(review.id)
      });
    }
  });

  const deleteLineComment = useMutation({
    mutationFn: async (commentId: number) => {
      return reviewService.deleteReviewComment(review.id, commentId);
    },
    onSuccess: () => {
      addNotification({
        type: 'review_comment',
        message: '评论已删除',
        timestamp: new Date().toISOString(),
        reviewId: String(review.id),
      });
      queryClient.invalidateQueries({ queryKey: ['review-diff', review.id] });
      queryClient.invalidateQueries({ queryKey: ['review-comments', review.id] });
    },
    onError: (err: unknown) => {
      const msg = err instanceof Error ? err.message : '删除评论失败';
      addNotification({
        type: 'review_comment',
        message: msg,
        timestamp: new Date().toISOString(),
        reviewId: String(review.id)
      });
    }
  });

  const handleAddComment = (filePath: string, lineNumber: number, content: string) => {
    addLineComment.mutate({ filePath, lineNumber, content });
  };

  const handleDeleteComment = (commentId: string) => {
    const idNum = parseInt(commentId, 10);
    if (!Number.isNaN(idNum)) {
      deleteLineComment.mutate(idNum);
    } else {
      addNotification({
        type: 'review_comment',
        message: '无法删除评论：无效的评论ID',
        timestamp: new Date().toISOString(),
        reviewId: String(review.id)
      });
    }
  };

  if (isFileListLoading) {
    return (
      <div className="flex items-center justify-center h-64 dark:bg-gray-900/50 rounded-lg fade-in">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-primary-600 mx-auto mb-4"></div>
          <p className="text-gray-600 dark:text-gray-400">正在加载文件列表...</p>
        </div>
      </div>
    );
  }

  if (fileListError) {
    return (
      <div className="card dark:bg-gray-900 dark:border-gray-800 text-center py-8 fade-in">
        <ExclamationTriangleIcon className="h-12 w-12 text-red-500 mx-auto mb-4" />
        <h3 className="text-lg font-medium text-gray-900 dark:text-gray-100 mb-2">加载失败</h3>
        <p className="text-gray-600 dark:text-gray-400 mb-2">无法加载文件列表</p>
        <p className="text-sm text-gray-400 dark:text-gray-500 mb-4">
          {fileListError instanceof Error ? fileListError.message : '未知错误'}
        </p>
        <button 
          className="btn btn-primary transition-all hover:scale-105"
          onClick={() => queryClient.invalidateQueries({ queryKey: ['review-diff-files', review.id] })}
        >
          重试
        </button>
      </div>
    );
  }

  if (!fileListData || !fileListData.files || fileListData.files.length === 0) {
    return (
      <div className="card dark:bg-gray-900 dark:border-gray-800 text-center py-8 fade-in">
        <DocumentTextIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <p className="text-gray-500 dark:text-gray-400">暂无代码变更</p>
      </div>
    );
  }

  return (
    <div className="space-y-6 fade-in">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-gray-900 dark:text-gray-100">代码变更</h3>
        <div className="flex items-center space-x-4">
          <div className="text-sm text-gray-500 dark:text-gray-400">
            {fileListData.totalFiles} 个文件 · 
            <span className="text-green-600"> +{fileListData.totalAddedLines}</span> · 
            <span className="text-red-600"> -{fileListData.totalDeletedLines}</span>
          </div>
          <div className="text-sm text-gray-500 dark:text-gray-400">
            {review.branch} ← {review.baseBranch}
          </div>
        </div>
      </div>

      <div className="h-[70vh] md:h-[78vh] rounded-lg overflow-hidden border border-gray-200 dark:border-gray-800 bg-white dark:bg-gray-900 transition-colors">
        <LazyDiffViewer
          reviewId={review.id}
          fileList={fileListData.files}
          comments={fileListData.comments}
          onAddComment={handleAddComment}
          onDeleteComment={handleDeleteComment}
          language="auto"
          targetFileAndLine={targetFileAndLine}
        />
      </div>
    </div>
  );
};

// Analysis Tab Component
interface AnalysisTabProps {
  review: Review;
}

const AnalysisTab = ({ review }: AnalysisTabProps) => {
  const [analysisData, setAnalysisData] = useState<AnalysisData | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [error, setError] = useState<string | null>(null);
  const [isGenerating, setIsGenerating] = useState(false);
  const [activeSection, setActiveSection] = useState<'dashboard' | 'risk' | 'suggestions' | 'summary'>('dashboard');

  const loadAnalysisData = useCallback(async () => {
    try {
      setIsLoading(true);
      setError(null);
      const data = await analysisService.getAnalysisData(review.id);
      setAnalysisData(data);
    } catch (error) {
      console.error('Failed to load analysis data:', error);
      setError(error instanceof Error ? error.message : '加载分析数据失败');
    } finally {
      setIsLoading(false);
    }
  }, [review.id]);

  useEffect(() => {
    loadAnalysisData();
  }, [loadAnalysisData]);

  const handleGenerateAnalysis = async (type: 'risk' | 'suggestions' | 'summary') => {
    try {
      setIsGenerating(true);
      setError(null);
      
      switch (type) {
        case 'risk': {
          const riskData = await analysisService.generateRiskAssessment(review.id);
          setAnalysisData(prev => ({ ...prev!, riskAssessment: riskData }));
          break;
        }
        case 'suggestions': {
          const suggestionsData = await analysisService.generateImprovementSuggestions(review.id);
          setAnalysisData(prev => ({ ...prev!, improvementSuggestions: suggestionsData }));
          break;
        }
        case 'summary': {
          const summaryData = await analysisService.generatePullRequestSummary(review.id);
          setAnalysisData(prev => ({ ...prev!, pullRequestSummary: summaryData }));
          break;
        }
      }
    } catch (error) {
      console.error(`Failed to generate ${type} analysis:`, error);
      setError(`生成${type === 'risk' ? '风险评估' : type === 'suggestions' ? '改进建议' : 'PR摘要'}失败`);
    } finally {
      setIsGenerating(false);
    }
  };

  const handleGenerateComprehensive = async () => {
    try {
      setIsGenerating(true);
      setError(null);
      const data = await analysisService.generateComprehensiveAnalysis(review.id);
      setAnalysisData(data);
    } catch (error) {
      console.error('Failed to generate comprehensive analysis:', error);
      setError('生成综合分析失败');
    } finally {
      setIsGenerating(false);
    }
  };

  if (isLoading) {
    return (
      <div className="space-y-6">
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-4 gap-4">
          {[...Array(4)].map((_, i) => (
            <LoadingCard key={i} title="加载中..." description="正在获取分析数据" />
          ))}
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <ErrorState
        title="分析数据加载失败"
        description={error}
        onRetry={loadAnalysisData}
      />
    );
  }

  return (
    <div className="space-y-6">
      {/* Section Navigation - 改进的二级菜单 */}
      <div className="bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 p-1">
        <nav className="flex space-x-1">
          {[
            { id: 'dashboard', name: '智能概览', icon: CpuChipIcon, description: '全面分析数据总览' },
            { id: 'risk', name: '风险评估', icon: ShieldCheckIcon, description: '代码安全风险分析' },
            { id: 'suggestions', name: '改进建议', icon: BoltIcon, description: 'AI 优化建议' },
            { id: 'summary', name: 'PR摘要', icon: DocumentTextIcon, description: '变更内容总结' },
          ].map((section) => (
            <button
              key={section.id}
              onClick={() => setActiveSection(section.id as 'dashboard' | 'risk' | 'suggestions' | 'summary')}
              className={`${
                activeSection === section.id
                  ? 'bg-primary-50 dark:bg-primary-900/20 text-primary-700 dark:text-primary-300 border-primary-200 dark:border-primary-700'
                  : 'text-gray-600 dark:text-gray-400 hover:text-gray-900 dark:hover:text-gray-200 hover:bg-gray-50 dark:hover:bg-gray-700/50 border-transparent'
              } relative flex-1 px-4 py-3 rounded-md border transition-all duration-200 ease-in-out group`}
              title={section.description}
            >
              <div className="flex flex-col items-center space-y-1">
                <section.icon className={`h-5 w-5 ${
                  activeSection === section.id 
                    ? 'text-primary-600 dark:text-primary-400' 
                    : 'text-gray-400 group-hover:text-gray-600 dark:group-hover:text-gray-300'
                }`} />
                <span className={`text-sm font-medium ${
                  activeSection === section.id 
                    ? 'text-primary-700 dark:text-primary-300' 
                    : 'text-gray-600 dark:text-gray-400 group-hover:text-gray-900 dark:group-hover:text-gray-200'
                }`}>
                  {section.name}
                </span>
                {/* 隐藏描述文本，仅在 tooltip 中显示 */}
                <span className="sr-only">{section.description}</span>
              </div>
              
              {/* 活动状态指示器 */}
              {activeSection === section.id && (
                <div className="absolute bottom-0 left-1/2 transform -translate-x-1/2 w-8 h-0.5 bg-primary-500 rounded-full"></div>
              )}
            </button>
          ))}
        </nav>
      </div>

      {/* 快速操作栏 */}
      <div className="flex items-center justify-between bg-gray-50 dark:bg-gray-800/50 rounded-lg p-4">
        <div className="flex items-center space-x-4">
          <div className="text-sm text-gray-600 dark:text-gray-400">
            AI 分析状态：
            <span className={`ml-2 inline-flex items-center px-2.5 py-0.5 rounded-full text-xs font-medium ${
              analysisData?.riskAssessment && analysisData?.improvementSuggestions?.length && analysisData?.pullRequestSummary
                ? 'bg-green-100 text-green-800 dark:bg-green-900/20 dark:text-green-400'
                : 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/20 dark:text-yellow-400'
            }`}>
              {analysisData?.riskAssessment && analysisData?.improvementSuggestions?.length && analysisData?.pullRequestSummary
                ? '已完成' : '部分完成'}
            </span>
          </div>
        </div>
        <div className="flex space-x-3">
          <button
            onClick={handleGenerateComprehensive}
            disabled={isGenerating}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50 disabled:cursor-not-allowed transition-colors"
          >
            {isGenerating ? (
              <>
                <LoadingSpinner className="w-4 h-4 mr-2" />
                生成中...
              </>
            ) : (
              <>
                <CpuChipIcon className="w-4 h-4 mr-2" />
                一键生成全部分析
              </>
            )}
          </button>
          <button
            onClick={loadAnalysisData}
            disabled={isLoading}
            className="inline-flex items-center px-4 py-2 border border-gray-300 dark:border-gray-600 text-sm font-medium rounded-md text-gray-700 dark:text-gray-300 bg-white dark:bg-gray-800 hover:bg-gray-50 dark:hover:bg-gray-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 transition-colors"
          >
            {isLoading ? '刷新中...' : '刷新数据'}
          </button>
        </div>
      </div>

      {/* Dashboard Section */}
      {activeSection === 'dashboard' && (
        <AnalysisDashboard analysisData={analysisData} />
      )}

      {/* Risk Assessment Section */}
      {activeSection === 'risk' && (
        <RiskAssessmentSection 
          riskAssessment={analysisData?.riskAssessment}
          onGenerate={() => handleGenerateAnalysis('risk')}
          isGenerating={isGenerating}
        />
      )}

      {/* Improvement Suggestions Section */}
      {activeSection === 'suggestions' && (
        <ImprovementSuggestionsSection 
          suggestions={analysisData?.improvementSuggestions || []}
          onGenerate={() => handleGenerateAnalysis('suggestions')}
          isGenerating={isGenerating}
        />
      )}

      {/* PR Summary Section */}
      {activeSection === 'summary' && (
        <PullRequestSummarySection 
          summary={analysisData?.pullRequestSummary}
          onGenerate={() => handleGenerateAnalysis('summary')}
          isGenerating={isGenerating}
        />
      )}
    </div>
  );
};

// Risk Assessment Section Component
interface RiskAssessmentSectionProps {
  riskAssessment?: RiskAssessment;
  onGenerate: () => void;
  isGenerating: boolean;
}

const RiskAssessmentSection = ({ riskAssessment, onGenerate, isGenerating }: RiskAssessmentSectionProps) => {
  if (!riskAssessment) {
    return (
      <EmptyState
        icon={ShieldCheckIcon}
        title="暂无风险评估数据"
        description="AI 分析可以帮助识别代码变更中的潜在风险点"
        action={
          <button
            onClick={onGenerate}
            disabled={isGenerating}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
          >
            {isGenerating ? '生成中...' : '生成风险评估'}
          </button>
        }
      />
    );
  }

  // 准备图表数据
  const riskDistributionData = [
    { level: 'high' as const, count: 1, percentage: 25 },
    { level: 'medium' as const, count: 2, percentage: 50 },
    { level: 'low' as const, count: 1, percentage: 25 }
  ];

  // 计算风险等级
  // 若未引入getRiskLevel, 可在顶部import { getRiskLevel } from '../types/analysis';
  const riskLevel = getRiskLevel(riskAssessment.overallRiskScore);
  const riskMetrics = [
    {
      label: '总体风险评分',
      value: riskAssessment.overallRiskScore.toFixed(2),
      color: (riskAssessment.overallRiskScore > 80 ? 'red' : riskAssessment.overallRiskScore > 60 ? 'orange' : 'green') as 'red' | 'orange' | 'green'
    },
    {
      label: '风险等级',
      value: riskLevel,
      color: (riskLevel === 'Critical' ? 'red' : riskLevel === 'High' ? 'orange' : 'green') as 'red' | 'orange' | 'green'
    },
    {
      label: 'AI 置信度',
      value: riskAssessment.confidenceScore !== undefined ? `${Math.round(riskAssessment.confidenceScore * 100)}%` : '未知',
      color: 'blue' as const
    }
  ];

  return (
    <div className="space-y-6">
      {/* 概览指标 */}
      <AnalysisCard title="风险概览" className="mb-6">
        <MetricGrid metrics={riskMetrics} columns={3} />
      </AnalysisCard>

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* 风险分布图 */}
        <AnalysisCard title="风险分布">
          <RiskDonutChart data={riskDistributionData} />
        </AnalysisCard>

        {/* 风险维度 */}
        <AnalysisCard title="风险维度分析">
          <div className="space-y-4">
            <div>
              <div className="flex justify-between items-center mb-2">
                <span className="text-sm text-gray-600 dark:text-gray-400">复杂性风险</span>
                <span className="text-sm font-medium">{riskAssessment.complexityRisk}%</span>
              </div>
              <ProgressBar 
                value={riskAssessment.complexityRisk} 
                max={100} 
                className="h-2" 
                showValue={false}
              />
            </div>
            
            <div>
              <div className="flex justify-between items-center mb-2">
                <span className="text-sm text-gray-600 dark:text-gray-400">安全风险</span>
                <span className="text-sm font-medium">{riskAssessment.securityRisk}%</span>
              </div>
              <ProgressBar 
                value={riskAssessment.securityRisk} 
                max={100} 
                className="h-2" 
                showValue={false}
              />
            </div>
            
            <div>
              <div className="flex justify-between items-center mb-2">
                <span className="text-sm text-gray-600 dark:text-gray-400">性能风险</span>
                <span className="text-sm font-medium">{riskAssessment.performanceRisk}%</span>
              </div>
              <ProgressBar 
                value={riskAssessment.performanceRisk} 
                max={100} 
                className="h-2" 
                showValue={false}
              />
            </div>
            
            <div>
              <div className="flex justify-between items-center mb-2">
                <span className="text-sm text-gray-600 dark:text-gray-400">可维护性风险</span>
                <span className="text-sm font-medium">{riskAssessment.maintainabilityRisk}%</span>
              </div>
              <ProgressBar 
                value={riskAssessment.maintainabilityRisk} 
                max={100} 
                className="h-2" 
                showValue={false}
              />
            </div>
          </div>
        </AnalysisCard>
      </div>

      {/* 缓解建议 */}
      <AnalysisCard title="缓解建议" collapsible defaultExpanded={true}>
        <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">
          {riskAssessment.mitigationSuggestions || '无具体建议'}
        </div>
      </AnalysisCard>

      {/* 风险描述 */}
      {riskAssessment.riskDescription && (
        <AnalysisCard title="风险描述" collapsible defaultExpanded={false}>
          <p className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed">
            {riskAssessment.riskDescription}
          </p>
        </AnalysisCard>
      )}
    </div>
  );
};

// Improvement Suggestions Section Component
interface ImprovementSuggestionsSectionProps {
  suggestions: ImprovementSuggestion[];
  onGenerate: () => void;
  isGenerating: boolean;
}

const ImprovementSuggestionsSection = ({ suggestions, onGenerate, isGenerating }: ImprovementSuggestionsSectionProps) => {
  if (suggestions.length === 0) {
    return (
      <EmptyState
        icon={BoltIcon}
        title="暂无改进建议"
        description="AI 可以分析代码并提供具体的改进建议"
        action={
          <button
            onClick={onGenerate}
            disabled={isGenerating}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
          >
            {isGenerating ? '生成中...' : '生成改进建议'}
          </button>
        }
      />
    );
  }

  // 统计建议类型
  const suggestionStats = suggestions.reduce((acc, suggestion) => {
    acc[suggestion.type] = (acc[suggestion.type] || 0) + 1;
    return acc;
  }, {} as Record<string, number>);

  const priorityStats = suggestions.reduce((acc, suggestion) => {
    acc[suggestion.priority] = (acc[suggestion.priority] || 0) + 1;
    return acc;
  }, {} as Record<string, number>);

  const statsMetrics = [
    { label: '总建议数', value: suggestions.length, color: 'blue' as const },
    { label: '高优先级', value: priorityStats['High'] || 0, color: 'orange' as const },
    { label: '代码质量', value: suggestionStats['CodeQuality'] || 0, color: 'green' as const }
  ];

  const getTagVariant = (type: string) => {
    switch (type) {
      case 'BugFix': return 'danger' as const;
      case 'Security': return 'warning' as const;
      case 'Performance': return 'info' as const;
      case 'CodeQuality': return 'success' as const;
      case 'Maintainability': return 'secondary' as const;
      case 'Testing': return 'primary' as const;
      default: return 'secondary' as const;
    }
  };

  const getPriorityVariant = (priority: string) => {
    switch (priority) {
      case 'Critical': return 'danger' as const;
      case 'High': return 'warning' as const;
      case 'Medium': return 'info' as const;
      case 'Low': return 'success' as const;
      default: return 'secondary' as const;
    }
  };

  return (
    <div className="space-y-6">
      {/* 统计概览 */}
      <AnalysisCard title="建议概览">
        <MetricGrid metrics={statsMetrics} columns={3} />
      </AnalysisCard>

      {/* 建议列表 */}
      <div className="space-y-4">
        {suggestions.map((suggestion) => (
          <AnalysisCard 
            key={suggestion.id} 
            title={suggestion.title}
            collapsible
            defaultExpanded={suggestion.priority === 'Critical' || suggestion.priority === 'High'}
            headerActions={
              <div className="flex items-center space-x-2">
                <Tag variant={getPriorityVariant(suggestion.priority)} size="sm">
                  {suggestion.priority}
                </Tag>
                <Tag variant={getTagVariant(suggestion.type)} size="sm">
                  {suggestion.type}
                </Tag>
              </div>
            }
          >
            <div className="space-y-4">
              {/* 文件路径和行号 */}
              {suggestion.filePath && (
                <div className="text-xs text-gray-500 dark:text-gray-400 bg-gray-50 dark:bg-gray-900 p-2 rounded">
                  {suggestion.filePath}
                  {suggestion.startLine && suggestion.endLine && 
                    ` (行 ${suggestion.startLine}-${suggestion.endLine})`
                  }
                </div>
              )}

              {/* 描述 */}
              <p className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed">
                {suggestion.description}
              </p>

              {/* 代码对比 */}
              {suggestion.originalCode && suggestion.suggestedCode && (
                <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                  <div>
                    <h5 className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-2">
                      原代码
                    </h5>
                    <pre className="text-xs bg-red-50 dark:bg-red-900/10 border border-red-200 dark:border-red-800 p-3 rounded overflow-x-auto">
                      {suggestion.originalCode}
                    </pre>
                  </div>
                  <div>
                    <h5 className="text-xs font-medium text-gray-700 dark:text-gray-300 mb-2">
                      建议代码
                    </h5>
                    <pre className="text-xs bg-green-50 dark:bg-green-900/10 border border-green-200 dark:border-green-800 p-3 rounded overflow-x-auto">
                      {suggestion.suggestedCode}
                    </pre>
                  </div>
                </div>
              )}

              {/* 推理和收益 */}
              <div className="grid grid-cols-1 lg:grid-cols-2 gap-4">
                {suggestion.reasoning && (
                  <div className="bg-blue-50 dark:bg-blue-900/10 p-3 rounded">
                    <h5 className="text-xs font-medium text-blue-700 dark:text-blue-300 mb-1">
                      分析推理
                    </h5>
                    <p className="text-xs text-blue-600 dark:text-blue-400">
                      {suggestion.reasoning}
                    </p>
                  </div>
                )}

                {suggestion.expectedBenefits && (
                  <div className="bg-green-50 dark:bg-green-900/10 p-3 rounded">
                    <h5 className="text-xs font-medium text-green-700 dark:text-green-300 mb-1">
                      预期收益
                    </h5>
                    <p className="text-xs text-green-600 dark:text-green-400">
                      {suggestion.expectedBenefits}
                    </p>
                  </div>
                )}
              </div>

              {/* 实施信息 */}
              <div className="flex items-center justify-between text-xs bg-gray-50 dark:bg-gray-900 p-2 rounded">
                <div className="flex items-center space-x-4">
                  <span className="text-gray-600 dark:text-gray-400">
                    实施复杂度: 
                    <ProgressBar 
                      value={suggestion.implementationComplexity} 
                      max={10} 
                      className="w-16 h-1.5 ml-1 inline-block" 
                    />
                    <span className="ml-1">{suggestion.implementationComplexity}/10</span>
                  </span>
                </div>
                {suggestion.confidenceScore && (
                  <span className="text-gray-600 dark:text-gray-400">
                    置信度: {Math.round(suggestion.confidenceScore * 100)}%
                  </span>
                )}
              </div>
            </div>
          </AnalysisCard>
        ))}
      </div>
    </div>
  );
};

// Pull Request Summary Section Component
interface PullRequestSummarySectionProps {
  summary?: PullRequestChangeSummary;
  onGenerate: () => void;
  isGenerating: boolean;
}

const PullRequestSummarySection = ({ summary, onGenerate, isGenerating }: PullRequestSummarySectionProps) => {
  if (!summary) {
    return (
      <EmptyState
        icon={DocumentTextIcon}
        title="暂无 PR 摘要"
        description="AI 可以生成详细的变更摘要和影响分析"
        action={
          <button
            onClick={onGenerate}
            disabled={isGenerating}
            className="inline-flex items-center px-4 py-2 border border-transparent text-sm font-medium rounded-md text-white bg-primary-600 hover:bg-primary-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-primary-500 disabled:opacity-50"
          >
            {isGenerating ? '生成中...' : '生成 PR 摘要'}
          </button>
        }
      />
    );
  }

  // 解析变更统计信息
  let changeStats = null;
  if (summary.changeStatistics) {
    try {
      changeStats = typeof summary.changeStatistics === 'string' 
        ? JSON.parse(summary.changeStatistics) 
        : summary.changeStatistics;
    } catch (e) {
      console.error('Failed to parse change statistics:', e);
    }
  }

  // 摘要指标
  const summaryMetrics: Array<{
    label: string;
    value: string | number;
    color?: 'green' | 'yellow' | 'orange' | 'red' | 'blue' | 'gray';
  }> = [
    { label: '变更类型', value: summary.changeType, color: 'orange' },
    { label: '业务影响', value: summary.businessImpact, color: 'blue' },
    { label: '技术影响', value: summary.technicalImpact, color: 'blue' },
    { label: '破坏性风险', value: summary.breakingChangeRisk, color: summary.breakingChangeRisk === 'High' || summary.breakingChangeRisk === 'Critical' ? 'red' : 'green' },
    { 
      label: 'AI 置信度', 
      value: summary.confidenceScore !== undefined ? `${Math.round(summary.confidenceScore * 100)}%` : '未知', 
      color: 'blue'
    },
  ];

  // 如果有变更统计，添加到指标中
  if (changeStats) {
    summaryMetrics.push(
      { label: '修改文件数', value: changeStats.modifiedFiles || 0, color: 'blue' },
      { label: '新增行数', value: `+${changeStats.addedLines || 0}`, color: 'green' },
      { label: '删除行数', value: `-${changeStats.deletedLines || 0}`, color: 'red' }
    );
  }

  return (
    <div className="space-y-6">
      {/* 摘要指标 */}
      <AnalysisCard title="摘要概览">
        <MetricGrid metrics={summaryMetrics} columns={summaryMetrics.length > 6 ? 4 : 3} />
      </AnalysisCard>

      {/* 整体摘要 */}
      <AnalysisCard title="整体摘要" collapsible defaultExpanded={true}>
        <p className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">
          {summary.summary || '暂无摘要'}
        </p>
      </AnalysisCard>

      {/* 详细描述 */}
      {summary.detailedDescription && (
        <AnalysisCard title="详细描述" collapsible defaultExpanded={true}>
          <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">
            {summary.detailedDescription}
          </div>
        </AnalysisCard>
      )}

      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {/* 关键变更 */}
        <AnalysisCard title="关键变更" collapsible defaultExpanded={true}>
          {summary.keyChanges ? (
            <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">{summary.keyChanges}</div>
          ) : (
            <div className="text-sm text-gray-400 italic">无关键变更信息</div>
          )}
        </AnalysisCard>

        {/* 影响分析 */}
        <AnalysisCard title="影响分析" collapsible defaultExpanded={true}>
          {summary.impactAnalysis ? (
            <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">{summary.impactAnalysis}</div>
          ) : (
            <div className="text-sm text-gray-400 italic">无影响分析</div>
          )}
        </AnalysisCard>
      </div>

      {/* 破坏性变更风险 */}
      {summary.breakingChangeRisk && summary.breakingChangeRisk !== 'None' && summary.breakingChangeRisk !== 'Low' && (
        <AnalysisCard 
          title="⚠️ 破坏性变更风险" 
          className="border-orange-200 dark:border-orange-800 bg-orange-50 dark:bg-orange-900/10"
        >
          <div className="flex items-start space-x-3">
            <ExclamationTriangleIcon className="h-6 w-6 text-orange-500 flex-shrink-0 mt-0.5" />
            <div>
              <p className="text-sm text-orange-700 dark:text-orange-400 font-medium mb-2">
                风险等级：{summary.breakingChangeRisk}
              </p>
              {summary.backwardCompatibility && (
                <p className="text-sm text-gray-600 dark:text-gray-400">
                  向后兼容性：{summary.backwardCompatibility}
                </p>
              )}
            </div>
          </div>
        </AnalysisCard>
      )}

      {/* 依赖变更 */}
      {summary.dependencyChanges && (
        <AnalysisCard title="依赖变更" collapsible defaultExpanded={true}>
          <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">
            {summary.dependencyChanges}
          </div>
        </AnalysisCard>
      )}

      {/* 性能和安全影响 */}
      {(summary.performanceImpact || summary.securityImpact) && (
        <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
          {summary.performanceImpact && (
            <AnalysisCard title="性能影响" collapsible defaultExpanded={true}>
              <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">
                {summary.performanceImpact}
              </div>
            </AnalysisCard>
          )}

          {summary.securityImpact && (
            <AnalysisCard title="🔒 安全影响" collapsible defaultExpanded={true}>
              <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">
                {summary.securityImpact}
              </div>
            </AnalysisCard>
          )}
        </div>
      )}

      {/* 测试和部署建议 */}
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        {summary.testingRecommendations && (
          <AnalysisCard title="测试建议" collapsible defaultExpanded={true}>
            <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">
              {summary.testingRecommendations}
            </div>
          </AnalysisCard>
        )}

        {summary.deploymentConsiderations && (
          <AnalysisCard title="部署注意事项" collapsible defaultExpanded={true}>
            <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">
              {summary.deploymentConsiderations}
            </div>
          </AnalysisCard>
        )}
      </div>

      {/* 文档要求 */}
      {summary.documentationRequirements && (
        <AnalysisCard title="文档要求" collapsible defaultExpanded={false}>
          <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">
            {summary.documentationRequirements}
          </div>
        </AnalysisCard>
      )}

      {/* 向后兼容性 */}
      {summary.backwardCompatibility && !summary.breakingChangeRisk && (
        <AnalysisCard title="向后兼容性" collapsible defaultExpanded={false}>
          <div className="text-sm text-gray-600 dark:text-gray-400 leading-relaxed whitespace-pre-line">
            {summary.backwardCompatibility}
          </div>
        </AnalysisCard>
      )}

      {/* 变更统计详情 */}
      {changeStats && (
        <AnalysisCard title="变更统计详情" collapsible defaultExpanded={false}>
          <div className="grid grid-cols-2 md:grid-cols-4 gap-4 text-sm">
            <div className="text-center p-3 bg-gray-50 dark:bg-gray-700 rounded">
              <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                {changeStats.modifiedFiles || 0}
              </div>
              <div className="text-gray-500 dark:text-gray-400 mt-1">修改文件</div>
            </div>
            <div className="text-center p-3 bg-green-50 dark:bg-green-900/20 rounded">
              <div className="text-2xl font-bold text-green-600 dark:text-green-400">
                +{changeStats.addedLines || 0}
              </div>
              <div className="text-gray-500 dark:text-gray-400 mt-1">新增行</div>
            </div>
            <div className="text-center p-3 bg-red-50 dark:bg-red-900/20 rounded">
              <div className="text-2xl font-bold text-red-600 dark:text-red-400">
                -{changeStats.deletedLines || 0}
              </div>
              <div className="text-gray-500 dark:text-gray-400 mt-1">删除行</div>
            </div>
            <div className="text-center p-3 bg-blue-50 dark:bg-blue-900/20 rounded">
              <div className="text-2xl font-bold text-blue-600 dark:text-blue-400">
                {(changeStats.addedLines || 0) + (changeStats.deletedLines || 0)}
              </div>
              <div className="text-gray-500 dark:text-gray-400 mt-1">总变更</div>
            </div>
          </div>
        </AnalysisCard>
      )}

      {/* AI 模型信息 */}
      {summary.aiModelVersion && (
        <div className="text-xs text-gray-500 dark:text-gray-400 text-center bg-gray-50 dark:bg-gray-900 p-2 rounded">
          AI 模型版本: {summary.aiModelVersion} | 生成时间: {new Date(summary.createdAt).toLocaleString('zh-CN')}
        </div>
      )}
    </div>
  );
};