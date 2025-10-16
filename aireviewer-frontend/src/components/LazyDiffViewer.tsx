import { useState, useEffect, memo, useCallback } from 'react';
import { useQuery, useQueryClient } from '@tanstack/react-query';
import { reviewService } from '../services/review.service';
import type { DiffFileMetadata, CodeComment } from '../types/diff';
import { FileViewer } from './DiffViewer';
import { useMemoryMonitor } from '../hooks/usePerformanceMonitor'; // 可选：用于调试

interface LazyDiffViewerProps {
  reviewId: number;
  fileList: DiffFileMetadata[];
  comments: CodeComment[];
  onAddComment?: (filePath: string, lineNumber: number, content: string) => void;
  onDeleteComment?: (commentId: string) => void;
  language?: string;
  targetFileAndLine?: { filePath: string; lineNumber: number } | null;
}

// 性能配置：最多缓存的文件数量
const MAX_CACHED_FILES = 3;

/**
 * 懒加载Diff查看器 - 只在用户选择文件时才加载该文件的diff内容
 * 性能优化：
 * 1. 按需加载文件内容
 * 2. 限制缓存数量（最多3个文件）
 * 3. 自动清理旧缓存
 */
export const LazyDiffViewer = memo(function LazyDiffViewer({
  reviewId,
  fileList,
  comments,
  onAddComment,
  onDeleteComment,
  language = 'javascript',
  targetFileAndLine
}: LazyDiffViewerProps) {
  const queryClient = useQueryClient();
  const [selectedFile, setSelectedFile] = useState<string | null>(
    fileList.length > 0 ? (fileList[0].newPath || fileList[0].oldPath) : null
  );
  const [fileHistory, setFileHistory] = useState<string[]>([]); // 追踪查看历史

  // 性能监控（开发环境，可选）
  useMemoryMonitor('LazyDiffViewer', import.meta.env.DEV);

  // 当targetFileAndLine改变时，自动切换到目标文件
  useEffect(() => {
    if (targetFileAndLine) {
      setSelectedFile(targetFileAndLine.filePath);
    }
  }, [targetFileAndLine]);

  // 清理超出限制的缓存
  useEffect(() => {
    if (fileHistory.length > MAX_CACHED_FILES) {
      const filesToRemove = fileHistory.slice(0, fileHistory.length - MAX_CACHED_FILES);
      filesToRemove.forEach(filePath => {
        queryClient.removeQueries({ 
          queryKey: ['review-diff-file', reviewId, filePath],
          exact: true 
        });
      });
      setFileHistory(prev => prev.slice(-MAX_CACHED_FILES));
    }
  }, [fileHistory, queryClient, reviewId]);

  // 按需加载选中文件的diff内容
  // 性能优化：限制缓存数量，只保留最近3个文件，避免内存泄漏
  const { data: fileDetailData, isLoading: isFileLoading } = useQuery({
    queryKey: ['review-diff-file', reviewId, selectedFile],
    queryFn: () => selectedFile ? reviewService.getReviewDiffFile(reviewId, selectedFile) : null,
    enabled: !!selectedFile,
    staleTime: 2 * 60 * 1000, // 2分钟缓存（进一步降低）
    gcTime: 3 * 60 * 1000, // 3分钟保留（进一步减少）
  });

  const handleSelectFile = useCallback((filePath: string) => {
    setSelectedFile(filePath);
    
    // 更新文件查看历史
    setFileHistory(prev => {
      const newHistory = prev.filter(f => f !== filePath); // 移除重复
      return [...newHistory, filePath]; // 添加到末尾
    });
  }, []);

  // 过滤出当前文件的评论
  const currentFileComments = selectedFile 
    ? comments.filter(c => c.filePath === selectedFile)
    : [];

  if (fileList.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500">
        <div className="text-center">
          <p>暂无代码变更</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-full bg-white border border-gray-200 rounded-lg overflow-hidden">
      {/* 文件树 */}
      <FileTree 
        files={fileList}
        selectedFile={selectedFile}
        onSelectFile={handleSelectFile}
      />
      
      {/* 文件内容区域 */}
      <div className="flex-1 overflow-auto">
        {isFileLoading ? (
          <div className="flex items-center justify-center h-full">
            <div className="text-center">
              <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
              <p className="text-gray-600">加载文件内容中...</p>
              <p className="text-xs text-gray-500 mt-2">{selectedFile}</p>
            </div>
          </div>
        ) : fileDetailData?.file ? (
          <FileViewer
            file={fileDetailData.file}
            comments={currentFileComments}
            onAddComment={onAddComment}
            onDeleteComment={onDeleteComment}
            language={language}
            isActive={true} // 总是激活状态
          />
        ) : (
          <div className="flex items-center justify-center h-full text-gray-500">
            选择一个文件查看详情
          </div>
        )}
      </div>
    </div>
  );
});

// 文件树组件
interface FileTreeProps {
  files: DiffFileMetadata[];
  selectedFile: string | null;
  onSelectFile: (filePath: string) => void;
}

const FileTree = memo(function FileTree({ files, selectedFile, onSelectFile }: FileTreeProps) {
  const getFileIcon = (type: string) => {
    switch (type) {
      case 'add':
        return <span className="text-green-500 font-bold">+</span>;
      case 'delete':
        return <span className="text-red-500 font-bold">-</span>;
      case 'modify':
        return <span className="text-blue-500 font-bold">M</span>;
      case 'rename':
        return <span className="text-orange-500 font-bold">R</span>;
      default:
        return <span className="text-gray-500 font-bold">?</span>;
    }
  };

  const FileTreeItem = memo(({ file }: { file: DiffFileMetadata }) => {
    const filePath = file.newPath || file.oldPath;
    const isSelected = selectedFile === filePath;
    
    return (
      <button
        onClick={() => onSelectFile(filePath)}
        className={`w-full text-left p-2 rounded-md text-sm flex items-center space-x-2 hover:bg-gray-100 transition-colors ${
          isSelected 
            ? 'bg-blue-100 text-blue-800' 
            : 'text-gray-700'
        }`}
        title={`${filePath} (+${file.addedLines} -${file.deletedLines})`}
      >
        {getFileIcon(file.type)}
        <span className="truncate flex-1">{filePath}</span>
        <span className="text-xs text-gray-500">
          +{file.addedLines} -{file.deletedLines}
        </span>
      </button>
    );
  });

  return (
    <div className="w-80 bg-gray-50 border-r border-gray-200 flex flex-col">
      <div className="p-4 border-b border-gray-200 flex-shrink-0">
        <h3 className="text-sm font-semibold text-gray-900">文件变更 ({files.length})</h3>
        {files.length > 50 && (
          <p className="text-xs text-gray-500 mt-1">💡 按需加载，性能优化</p>
        )}
      </div>
      <div className="flex-1 overflow-y-auto p-4">
        <div className="space-y-1">
          {files.map((file, index) => (
            <FileTreeItem
              key={`${file.oldPath}-${file.newPath}-${index}`}
              file={file}
            />
          ))}
        </div>
      </div>
    </div>
  );
});
