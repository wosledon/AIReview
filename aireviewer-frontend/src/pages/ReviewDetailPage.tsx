import { useState, useEffect } from 'react';
import { useParams, useNavigate, Link } from 'react-router-dom';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
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
import { useNotifications } from '../hooks/useNotifications';
import { DiffViewer } from '../components/DiffViewer';
import { ReviewState, ReviewCommentSeverity, ReviewCommentCategory } from '../types/review';
import type { Review, ReviewComment, AddCommentRequest, RejectReviewRequest } from '../types/review';

export const ReviewDetailPage = () => {
  const { id } = useParams<{ id: string }>();
  const navigate = useNavigate();
  const queryClient = useQueryClient();
  const { joinGroup, leaveGroup, addNotification } = useNotifications();
  const [activeTab, setActiveTab] = useState<'overview' | 'comments' | 'diff'>('overview');
  const [showAddComment, setShowAddComment] = useState(false);
  const [showRejectDialog, setShowRejectDialog] = useState(false);
  const [rejectReason, setRejectReason] = useState('');

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
        message: '评审已通过',
        timestamp: new Date().toISOString(),
        reviewId: String(reviewId)
      });
    },
    onError: (error: unknown) => {
      const msg = error instanceof Error ? error.message : '通过评审失败';
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

  const handleStartAIReview = () => {
    // TODO: 实现 AI 评审开始逻辑
    console.log('Starting AI review for', reviewId);
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
        <div className="text-red-600 mb-4">
          <p>加载评审详情时出错</p>
        </div>
        <button 
          onClick={() => navigate('/reviews')}
          className="btn btn-primary"
        >
          返回评审列表
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

  const tabs = [
    { id: 'overview', name: '概览', icon: EyeIcon },
    { id: 'comments', name: '评论', icon: ChatBubbleLeftRightIcon, count: comments?.length || 0 },
    { id: 'diff', name: '代码变更', icon: DocumentTextIcon },
  ] as const;

  return (
    <div className="space-y-6">
      {/* Reject Dialog */}
      {showRejectDialog && (
        <div className="fixed inset-0 bg-gray-600 bg-opacity-50 overflow-y-auto h-full w-full z-50 flex items-center justify-center">
          <div className="relative p-6 border w-96 shadow-lg rounded-md bg-white">
            <div className="mt-3">
              <h3 className="text-lg font-bold text-gray-900 mb-4">拒绝评审</h3>
              <div className="mb-4">
                <label htmlFor="reject-reason" className="block text-sm font-medium text-gray-700 mb-2">
                  拒绝原因 (可选)
                </label>
                <textarea
                  id="reject-reason"
                  rows={4}
                  className="w-full px-3 py-2 text-gray-700 border rounded-lg focus:outline-none focus:border-primary-500"
                  placeholder="请说明拒绝的原因..."
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
                  取消
                </button>
                <button
                  type="button"
                  className="btn btn-danger"
                  onClick={handleConfirmReject}
                  disabled={rejectReviewMutation.isPending}
                >
                  {rejectReviewMutation.isPending ? '处理中...' : '确认拒绝'}
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
            className="p-2 text-gray-400 hover:text-gray-600 rounded-lg hover:bg-gray-100"
          >
            <ArrowLeftIcon className="h-5 w-5" />
          </button>
          <div>
            <div className="flex items-center space-x-3">
              <h1 className="text-2xl font-bold text-gray-900">{review.title}</h1>
              <div className="flex items-center">
                {getStatusIcon(review.status)}
                <span className="ml-2 text-sm font-medium text-gray-700">
                  {getStatusText(review.status)}
                </span>
              </div>
            </div>
            {review.description && (
              <p className="text-gray-500 mt-1">{review.description}</p>
            )}
            <div className="flex items-center space-x-4 mt-2 text-sm text-gray-500">
              <span>项目: {review.projectName}</span>
              <span>分支: {review.branch}</span>
              <span>作者: {review.authorName}</span>
              {review.pullRequestNumber && (
                <span>PR #{review.pullRequestNumber}</span>
              )}
            </div>
          </div>
        </div>

        <div className="flex items-center space-x-3">
          {review.status === ReviewState.Pending && (
            <button 
              className="btn btn-primary"
              onClick={handleStartAIReview}
            >
              <CpuChipIcon className="h-5 w-5 mr-2" />
              开始AI评审
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
      <div className="border-b border-gray-200">
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
                    : 'border-transparent text-gray-500 hover:text-gray-700 hover:border-gray-300'
                } whitespace-nowrap py-2 px-1 border-b-2 font-medium text-sm flex items-center space-x-2`}
              >
                <Icon className="h-5 w-5" />
                <span>{tab.name}</span>
                {'count' in tab && tab.count > 0 && (
                  <span className="bg-gray-100 text-gray-600 py-0.5 px-2 rounded-full text-xs">
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
          />
        )}
        {activeTab === 'diff' && <DiffTab review={review} />}
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
          <div className="card">
            <div className="flex items-center mb-4">
              <CpuChipIcon className="h-6 w-6 text-blue-500 mr-3" />
              <h3 className="text-lg font-semibold text-gray-900">AI评审总结</h3>
            </div>
            
            <div className="space-y-4">
              <div className="bg-blue-50 border border-blue-200 rounded-lg p-4">
                <p className="text-blue-800">
                  AI分析发现了 {aiComments.length} 个问题，建议重点关注安全性和代码质量问题。
                </p>
              </div>

              <div className="space-y-3">
                {aiComments.slice(0, 3).map((comment) => (
                  <div key={comment.id} className="flex items-start space-x-3 p-3 bg-gray-50 rounded-lg">
                    {getSeverityIcon(comment.severity)}
                    <div className="flex-1">
                      <div className="flex items-center space-x-2 mb-1">
                        <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${getSeverityColor(comment.severity)}`}>
                          {comment.severity}
                        </span>
                        <span className="text-xs text-gray-500 flex items-center">
                          {getCategoryIcon(comment.category)}
                          <span className="ml-1">{comment.category}</span>
                        </span>
                      </div>
                      {comment.filePath && (
                        <p className="text-xs text-gray-500 text-left mt-3">
                          {comment.filePath}:{comment.lineNumber}
                        </p>
                      )}
                      <p className="text-sm text-gray-900 text-left mt-1">{comment.content}</p>
                    </div>
                  </div>
                ))}
              </div>

              {aiComments.length > 3 && (
                <div className="text-center">
                  <button className="text-primary-600 hover:text-primary-700 text-sm font-medium">
                    查看全部 {aiComments.length} 个AI评审意见
                  </button>
                </div>
              )}
            </div>
          </div>
        )}

        {/* Human Comments */}
        {humanComments.length > 0 && (
          <div className="card">
            <h3 className="text-lg font-semibold text-gray-900 mb-4">人工评审</h3>
            <div className="space-y-3">
              {humanComments.map((comment) => (
                <div key={comment.id} className="flex items-start space-x-3 p-3 border border-gray-200 rounded-lg">
                  <div className="h-8 w-8 rounded-full bg-primary-100 flex items-center justify-center">
                    <span className="text-primary-600 font-medium text-sm">
                      {comment.authorName.charAt(0).toUpperCase()}
                    </span>
                  </div>
                  <div className="flex-1">
                    <div className="flex items-center space-x-2 mb-1">
                      <span className="text-sm font-medium text-gray-900">{comment.authorName}</span>
                      <span className="text-xs text-gray-500">
                        {new Date(comment.createdAt).toLocaleString('zh-CN')}
                      </span>
                    </div>
                    {comment.filePath && (
                        <p className="text-xs text-gray-500 text-left mt-3">
                          {comment.filePath}:{comment.lineNumber}
                        </p>
                      )}
                    <p className="text-sm text-gray-900 text-left mt-1">{comment.content}</p>
                  </div>
                </div>
              ))}
            </div>
          </div>
        )}

        {/* No Comments */}
        {comments.length === 0 && (
          <div className="card text-center py-8">
            <ChatBubbleLeftRightIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <p className="text-gray-500">暂无评审意见</p>
            <p className="text-sm text-gray-400 mt-1">
              {review.status === ReviewState.Pending ? '等待AI评审完成' : '开始添加评审意见'}
            </p>
          </div>
        )}
      </div>

      {/* Sidebar */}
      <div className="space-y-6">
        {/* Review Info */}
        <div className="card">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">评审信息</h3>
          <dl className="space-y-3">
            <div>
              <dt className="text-sm font-medium text-gray-500">状态</dt>
              <dd className="mt-1 text-sm text-gray-900">{getStatusText(review.status)}</dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">创建时间</dt>
              <dd className="mt-1 text-sm text-gray-900">
                {new Date(review.createdAt).toLocaleString('zh-CN')}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">更新时间</dt>
              <dd className="mt-1 text-sm text-gray-900">
                {new Date(review.updatedAt).toLocaleString('zh-CN')}
              </dd>
            </div>
            <div>
              <dt className="text-sm font-medium text-gray-500">基础分支</dt>
              <dd className="mt-1 text-sm text-gray-900">{review.baseBranch}</dd>
            </div>
          </dl>
        </div>

        {/* Stats */}
        <div className="card">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">统计信息</h3>
          <div className="space-y-3">
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500">总评论数</span>
              <span className="text-sm font-semibold text-gray-900">{comments.length}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500">AI评论</span>
              <span className="text-sm font-semibold text-blue-600">{aiComments.length}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500">人工评论</span>
              <span className="text-sm font-semibold text-green-600">{humanComments.length}</span>
            </div>
            <div className="flex items-center justify-between">
              <span className="text-sm text-gray-500">严重问题</span>
              <span className="text-sm font-semibold text-red-600">
                {comments.filter(c => c.severity === ReviewCommentSeverity.Critical || c.severity === ReviewCommentSeverity.Error).length}
              </span>
            </div>
          </div>
        </div>

        {/* Quick Actions */}
        <div className="card">
          <h3 className="text-lg font-semibold text-gray-900 mb-4">快速操作</h3>
          <div className="space-y-3">
            <Link
              to={`/projects/${review.projectId}`}
              className="btn btn-secondary w-full inline-flex items-center space-x-1"
            >
              <EyeIcon className="h-5 w-5 mr-2" />
              查看项目
            </Link>
            <button className="btn btn-secondary w-full inline-flex items-center space-x-1">
              <ChatBubbleLeftRightIcon className="h-5 w-5 mr-2" />
              添加评论
            </button>
            {review.status === ReviewState.HumanReview && (
              <>
                <button 
                  className="btn btn-primary w-full"
                  onClick={onApproveReview}
                  disabled={isApproving}
                >
                  {isApproving ? '处理中...' : '通过评审'}
                </button>
                <button 
                  className="btn btn-danger w-full"
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
}

const CommentsTab = ({ comments, isLoading, showAddComment, onShowAddComment, onAddComment, isAddingComment }: CommentsTabProps) => {
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
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-gray-900">
          评审意见 ({comments.length})
        </h3>
        <button
          onClick={() => onShowAddComment(!showAddComment)}
          className="btn btn-primary inline-flex items-center space-x-1"
        >
          <PlusIcon className="h-5 w-5 mr-2" />
          添加评论
        </button>
      </div>

      {/* Add Comment Form */}
      {showAddComment && (
        <div className="card">
          <h4 className="text-md font-semibold text-gray-900 mb-4">添加评审意见</h4>
          <form onSubmit={handleSubmit} className="space-y-4">
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                评论内容 *
              </label>
              <textarea
                rows={4}
                className="input resize-none"
                placeholder="请输入您的评审意见..."
                value={newComment.content}
                onChange={(e) => setNewComment(prev => ({ ...prev, content: e.target.value }))}
                required
                disabled={isAddingComment}
              />
            </div>
            
            <div className="grid grid-cols-2 gap-4">
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  严重程度
                </label>
                <select
                  className="input"
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
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  分类
                </label>
                <select
                  className="input"
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
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  文件路径
                </label>
                <input
                  type="text"
                  className="input"
                  placeholder="src/components/Example.tsx"
                  value={newComment.filePath}
                  onChange={(e) => setNewComment(prev => ({ ...prev, filePath: e.target.value }))}
                  disabled={isAddingComment}
                />
              </div>
              
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-2">
                  行号
                </label>
                <input
                  type="number"
                  className="input"
                  placeholder="123"
                  value={newComment.lineNumber || ''}
                  onChange={(e) => setNewComment(prev => ({ ...prev, lineNumber: e.target.value ? parseInt(e.target.value) : undefined }))}
                  disabled={isAddingComment}
                />
              </div>
            </div>

            <div>
              <label className="block text-sm font-medium text-gray-700 mb-2">
                建议修改
              </label>
              <textarea
                rows={3}
                className="input resize-none"
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
                className="btn btn-secondary"
                disabled={isAddingComment}
              >
                取消
              </button>
              <button
                type="submit"
                className="btn btn-primary"
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
        <div className="card text-center py-8">
          <ChatBubbleLeftRightIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
          <p className="text-gray-500">暂无评审意见</p>
          <button
            onClick={() => onShowAddComment(true)}
            className="btn btn-primary mt-4"
          >
            添加第一个评论
          </button>
        </div>
      ) : (
        <div className="space-y-4">
          {comments.map((comment) => (
            <CommentCard key={comment.id} comment={comment} />
          ))}
        </div>
      )}
    </div>
  );
};

interface CommentCardProps {
  comment: ReviewComment;
}

const CommentCard = ({ comment }: CommentCardProps) => {
  const getSeverityColor = (severity: string) => {
    switch (severity) {
      case ReviewCommentSeverity.Critical:
        return 'bg-red-100 text-red-800 border-red-200';
      case ReviewCommentSeverity.Error:
        return 'bg-red-100 text-red-800 border-red-200';
      case ReviewCommentSeverity.Warning:
        return 'bg-orange-100 text-orange-800 border-orange-200';
      default:
        return 'bg-blue-100 text-blue-800 border-blue-200';
    }
  };

  return (
    <div className={`border rounded-lg p-4 ${comment.isAIGenerated ? 'bg-blue-50 border-blue-200' : 'bg-white border-gray-200'}`}>
      <div className="flex items-start space-x-3">
        <div className={`h-8 w-8 rounded-full flex items-center justify-center ${
          comment.isAIGenerated ? 'bg-blue-100' : 'bg-primary-100'
        }`}>
          {comment.isAIGenerated ? (
            <CpuChipIcon className="h-5 w-5 text-blue-600" />
          ) : (
            <span className="text-primary-600 font-medium text-sm">
              {comment.authorName.charAt(0).toUpperCase()}
            </span>
          )}
        </div>
        
        <div className="flex-1">
          <div className="flex items-center space-x-2 mb-2">
            <span className="text-sm font-medium text-gray-900">
              {comment.isAIGenerated ? 'AI评审' : comment.authorName}
            </span>
            <span className={`inline-flex items-center px-2 py-0.5 rounded text-xs font-medium ${getSeverityColor(comment.severity)}`}>
              {comment.severity}
            </span>
            <span className="text-xs text-gray-500">{comment.category}</span>
            <span className="text-xs text-gray-500">
              {new Date(comment.createdAt).toLocaleString('zh-CN')}
            </span>
          </div>
          
          {comment.filePath && (
            <div className="text-xs text-gray-500 mb-2">
              📁 {comment.filePath}
              {comment.lineNumber && `:${comment.lineNumber}`}
            </div>
          )}
          
          <p className="text-sm text-gray-900 mb-3">{comment.content}</p>
          
          {comment.suggestion && (
            <div className="bg-gray-50 border border-gray-200 rounded p-3">
              <div className="text-xs font-medium text-gray-700 mb-1">💡 建议修改:</div>
              <p className="text-sm text-gray-700">{comment.suggestion}</p>
            </div>
          )}
        </div>
      </div>
    </div>
  );
};

interface DiffTabProps {
  review: Review;
}

const DiffTab = ({ review }: DiffTabProps) => {
  const { addNotification } = useNotifications();
  // 使用真实的API数据
  const { data: diffData, isLoading: isDiffLoading, error: diffError } = useQuery({
    queryKey: ['review-diff', review.id],
    queryFn: () => reviewService.getReviewDiff(review.id),
  });

  const queryClient = useQueryClient();

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

  if (isDiffLoading) {
    return (
      <div className="flex items-center justify-center py-8">
        <div className="animate-spin rounded-full h-8 w-8 border-b-2 border-primary-600"></div>
        <span className="ml-2 text-gray-600">加载代码变更中...</span>
      </div>
    );
  }

  if (diffError) {
    return (
      <div className="text-center py-8">
        <ExclamationTriangleIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <p className="text-gray-500">无法加载代码变更</p>
        <p className="text-sm text-gray-400 mt-2">
          {diffError instanceof Error ? diffError.message : '未知错误'}
        </p>
      </div>
    );
  }

  if (!diffData || !diffData.files || diffData.files.length === 0) {
    return (
      <div className="text-center py-8">
        <DocumentTextIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
        <p className="text-gray-500">暂无代码变更</p>
      </div>
    );
  }

  return (
    <div className="space-y-6">
      <div className="flex items-center justify-between">
        <h3 className="text-lg font-semibold text-gray-900">代码变更</h3>
        <div className="text-sm text-gray-500">
          {review.branch} ← {review.baseBranch}
        </div>
      </div>

      <div className="h-[70vh] md:h-[78vh]">
        <DiffViewer
          files={diffData.files}
          comments={diffData.comments}
          onAddComment={handleAddComment}
          onDeleteComment={handleDeleteComment}
          language="auto"
        />
      </div>
    </div>
  );
};