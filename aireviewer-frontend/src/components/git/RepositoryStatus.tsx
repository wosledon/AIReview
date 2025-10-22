import React, { useState, useEffect, useCallback } from 'react';
import { 
  ArrowPathIcon, 
  CheckCircleIcon, 
  ExclamationCircleIcon, 
  ClockIcon,
  InformationCircleIcon
} from '@heroicons/react/24/outline';
import { gitService, type GitRepositoryStatus } from '../../services/git.service';

interface RepositoryStatusProps {
  repositoryId: number;
  className?: string;
}

const RepositoryStatus: React.FC<RepositoryStatusProps> = ({ repositoryId, className = '' }) => {
  const [status, setStatus] = useState<GitRepositoryStatus | null>(null);
  const [loading, setLoading] = useState(true);
  const [pulling, setPulling] = useState(false);
  const [error, setError] = useState<string | null>(null);

  const fetchStatus = useCallback(async () => {
    try {
      setLoading(true);
      const response = await gitService.getRepositoryStatus(repositoryId);
      if (response.success && response.data) {
        setStatus(response.data);
        setError(null);
      } else {
        setError(response.message || '获取状态失败');
      }
    } catch (err) {
      setError('获取仓库状态失败');
      console.error('Error fetching repository status:', err);
    } finally {
      setLoading(false);
    }
  }, [repositoryId]);

  const handlePull = async () => {
    try {
      setPulling(true);
      const response = await gitService.pullRepository(repositoryId, status?.currentBranch);
      if (response.success) {
        // 拉取成功后刷新状态
        setTimeout(fetchStatus, 1000);
      } else {
        setError(response.message || '拉取失败');
      }
    } catch (err) {
      setError('拉取操作失败');
      console.error('Error pulling repository:', err);
    } finally {
      setPulling(false);
    }
  };

  useEffect(() => {
    fetchStatus();
  }, [fetchStatus]);

  useEffect(() => {
    // 如果正在拉取，定期刷新状态
    let interval: number | null = null;
    if (status?.isPulling) {
      interval = window.setInterval(fetchStatus, 2000);
    }
    
    return () => {
      if (interval) clearInterval(interval);
    };
  }, [status?.isPulling, fetchStatus]);

  const getStatusIcon = () => {
    if (loading) return <ArrowPathIcon className="h-5 w-5 animate-spin text-gray-500" />;
    
    switch (status?.status) {
      case 'Success':
        return <CheckCircleIcon className="h-5 w-5 text-green-500" />;
      case 'Failed':
        return <ExclamationCircleIcon className="h-5 w-5 text-red-500" />;
      case 'Pulling':
        return <ArrowPathIcon className="h-5 w-5 animate-spin text-blue-500" />;
      case 'Never':
        return <InformationCircleIcon className="h-5 w-5 text-gray-400" />;
      default:
        return <ClockIcon className="h-5 w-5 text-gray-400" />;
    }
  };

  const getStatusText = () => {
    switch (status?.status) {
      case 'Success': return '同步成功';
      case 'Failed': return '同步失败';
      case 'Pulling': return '同步中...';
      case 'Never': return '未同步';
      default: return '未知状态';
    }
  };

  const getStatusColor = () => {
    switch (status?.status) {
      case 'Success': return 'text-green-600';
      case 'Failed': return 'text-red-600';
      case 'Pulling': return 'text-blue-600';
      default: return 'text-gray-600';
    }
  };

  if (loading && !status) {
    return (
      <div className={`bg-white rounded-lg border border-gray-200 p-4 ${className}`}>
        <div className="flex items-center space-x-2">
          <ArrowPathIcon className="h-5 w-5 animate-spin text-gray-500" />
          <span className="text-sm text-gray-500">加载状态中...</span>
        </div>
      </div>
    );
  }

  if (error) {
    return (
      <div className={`bg-red-50 border border-red-200 rounded-lg p-4 ${className}`}>
        <div className="flex items-center space-x-2">
          <ExclamationCircleIcon className="h-5 w-5 text-red-500" />
          <span className="text-sm text-red-600">{error}</span>
          <button
            onClick={fetchStatus}
            className="text-red-600 hover:text-red-800 text-sm underline"
          >
            重试
          </button>
        </div>
      </div>
    );
  }

  return (
    <div className={`bg-white rounded-lg border border-gray-200 p-4 ${className}`}>
      <div className="flex items-center justify-between">
        <div className="flex items-center space-x-3">
          {getStatusIcon()}
          <div>
            <div className={`text-sm font-medium ${getStatusColor()}`}>
              {getStatusText()}
            </div>
            {status?.currentBranch && (
              <div className="text-xs text-gray-500">
                分支: {status.currentBranch}
              </div>
            )}
            {status?.lastPullTime && (
              <div className="text-xs text-gray-500">
                最后同步: {new Date(status.lastPullTime).toLocaleString()}
              </div>
            )}
          </div>
        </div>

        <div className="flex items-center space-x-2">
          {status?.isPulling && status.progress > 0 && (
            <div className="text-xs text-blue-600">
              {status.progress}%
            </div>
          )}
          
          <button
            onClick={handlePull}
            disabled={pulling || status?.isPulling}
            className="inline-flex items-center px-3 py-1.5 border border-transparent text-xs font-medium rounded-md text-white bg-blue-600 hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
          >
            {pulling || status?.isPulling ? (
              <>
                <ArrowPathIcon className="h-3 w-3 mr-1 animate-spin" />
                拉取中
              </>
            ) : (
              '拉取'
            )}
          </button>
        </div>
      </div>

      {status?.errorMessage && (
        <div className="mt-2 p-2 bg-red-50 border border-red-200 rounded text-xs text-red-600">
          {status.errorMessage}
        </div>
      )}

      {status?.isPulling && (
        <div className="mt-2">
          <div className="bg-gray-200 rounded-full h-1.5">
            <div 
              className="bg-blue-500 h-1.5 rounded-full transition-all duration-300"
              style={{ width: `${status.progress}%` }}
            />
          </div>
        </div>
      )}

      {status && (status.totalFiles > 0 || status.totalLines > 0) && (
        <div className="mt-2 text-xs text-gray-500">
          {status.totalFiles > 0 && `${status.totalFiles} 文件`}
          {status.totalFiles > 0 && status.totalLines > 0 && ' • '}
          {status.totalLines > 0 && `${status.totalLines.toLocaleString()} 行代码`}
        </div>
      )}
    </div>
  );
};

export default RepositoryStatus;