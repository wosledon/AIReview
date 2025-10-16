import { useState, useMemo, useCallback, useEffect, memo } from 'react';
import { 
  ChatBubbleLeftIcon, 
  PlusIcon, 
  XMarkIcon,
  ExclamationTriangleIcon,
  InformationCircleIcon,
  ExclamationCircleIcon,
  ChevronDownIcon
} from '@heroicons/react/24/outline';
import { useCodeHighlight } from '../hooks/useCodeHighlight';
import type { DiffFile, DiffChange, CodeComment, DiffViewerProps } from '../types/diff';

// 性能配置常量
const INITIAL_LINES_TO_SHOW = 100; // 默认显示100行
const INITIAL_LINES_FOR_HUGE_FILE = 50; // 超大文件（500行+）只显示50行
const LINES_TO_LOAD_MORE = 50; // 每次加载50行
const LARGE_DIFF_THRESHOLD = 200; // 200行算大文件
const HUGE_DIFF_THRESHOLD = 500; // 500行算超大文件
const HIGHLIGHT_DEBOUNCE_MS = 50; // 高亮延迟
const FILE_SWITCH_CLEANUP_DELAY_MS = 100; // 快速清理
const CHUNK_SIZE = 20; // 分块渲染：每批20行

interface FileTreeProps {
  files: DiffFile[];
  selectedFile: string | null;
  onSelectFile: (filePath: string) => void;
}

function FileTree({ files, selectedFile, onSelectFile }: FileTreeProps) {
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

  // 使用memo优化FileTreeItem
  const FileTreeItem = memo(({ file, index, isSelected }: { file: DiffFile; index: number; isSelected: boolean }) => (
    <button
      key={`${file.oldPath}-${file.newPath}-${index}`}
      onClick={() => onSelectFile(file.newPath || file.oldPath)}
      className={`w-full text-left p-2 rounded-md text-sm flex items-center space-x-2 hover:bg-gray-100 transition-colors ${
        isSelected 
          ? 'bg-blue-100 text-blue-800' 
          : 'text-gray-700'
      }`}
      title={file.newPath || file.oldPath}
    >
      {getFileIcon(file.type)}
      <span className="truncate flex-1">{file.newPath || file.oldPath}</span>
    </button>
  ));

  return (
    <div className="w-80 bg-gray-50 border-r border-gray-200 flex flex-col">
      <div className="p-4 border-b border-gray-200 flex-shrink-0">
        <h3 className="text-sm font-semibold text-gray-900">文件变更 ({files.length})</h3>
        {files.length > 50 && (
          <p className="text-xs text-gray-500 mt-1">💡 大量文件，使用搜索快速定位</p>
        )}
      </div>
      <div className="flex-1 overflow-y-auto p-4">
        <div className="space-y-1">
          {files.map((file, index) => (
            <FileTreeItem
              key={`${file.oldPath}-${file.newPath}-${index}`}
              file={file}
              index={index}
              isSelected={selectedFile === (file.newPath || file.oldPath)}
            />
          ))}
        </div>
      </div>
    </div>
  );
}

interface CommentInputProps {
  filePath: string;
  lineNumber: number;
  onSave: (content: string) => void;
  onCancel: () => void;
}

function CommentInput({ onSave, onCancel }: CommentInputProps) {
  const [content, setContent] = useState('');

  const handleSave = () => {
    if (content.trim()) {
      onSave(content.trim());
      setContent('');
    }
  };

  return (
    <div className="bg-yellow-50 border border-yellow-200 p-3 mt-1">
      <textarea
        value={content}
        onChange={(e) => setContent(e.target.value)}
        placeholder="添加评论..."
        className="w-full p-2 text-sm border border-gray-300 rounded-md resize-none focus:ring-2 focus:ring-blue-500 focus:border-blue-500"
        rows={3}
        autoFocus
      />
      <div className="flex justify-end space-x-2 mt-2">
        <button
          onClick={onCancel}
          className="px-3 py-1 text-sm text-gray-600 hover:text-gray-800"
        >
          取消
        </button>
        <button
          onClick={handleSave}
          disabled={!content.trim()}
          className="px-3 py-1 text-sm bg-blue-600 text-white rounded-md hover:bg-blue-700 disabled:opacity-50 disabled:cursor-not-allowed"
        >
          保存
        </button>
      </div>
    </div>
  );
}

interface CommentBubbleProps {
  comment: CodeComment;
  onDelete?: (commentId: string) => void;
}

function CommentBubble({ comment, onDelete }: CommentBubbleProps) {
  const getSeverityIcon = () => {
    switch (comment.severity) {
      case 'critical':
      case 'error':
        return <ExclamationCircleIcon className="h-4 w-4 text-red-500" />;
      case 'warning':
        return <ExclamationTriangleIcon className="h-4 w-4 text-yellow-500" />;
      default:
        return <InformationCircleIcon className="h-4 w-4 text-blue-500" />;
    }
  };

  const getSeverityBorder = () => {
    switch (comment.severity) {
      case 'critical':
      case 'error':
        return 'border-red-200 bg-red-50';
      case 'warning':
        return 'border-yellow-200 bg-yellow-50';
      default:
        return 'border-blue-200 bg-blue-50';
    }
  };

  return (
    <div className={`border rounded-md p-3 mt-1 ${getSeverityBorder()}`}>
      <div className="flex items-start justify-between">
        <div className="flex items-center space-x-2">
          {getSeverityIcon()}
          <span className="text-sm font-medium text-gray-900">
            {comment.author}
            {comment.type === 'ai' && (
              <span className="ml-2 px-2 py-0.5 text-xs bg-purple-100 text-purple-700 rounded-full">
                AI
              </span>
            )}
          </span>
          <span className="text-xs text-gray-500">
            {new Date(comment.createdAt).toLocaleString('zh-CN')}
          </span>
        </div>
        {onDelete && (
          <button
            onClick={() => onDelete(comment.id)}
            className="text-gray-400 hover:text-gray-600"
          >
            <XMarkIcon className="h-4 w-4" />
          </button>
        )}
      </div>
      <p className="mt-2 text-sm text-gray-700 whitespace-pre-wrap">{comment.content}</p>
    </div>
  );
}

interface DiffLineProps {
  change: DiffChange;
  lineNumber: number;
  comments: CodeComment[];
  onAddComment?: (lineNumber: number) => void;
  onDeleteComment?: (commentId: string) => void;
  highlightedContent: string;
  showAddComment: boolean;
  showCommentInput: boolean;
  onSaveComment: (content: string) => void;
  onCancelComment: () => void;
}

// 使用 React.memo 优化，避免不必要的重渲染
const DiffLine = memo(function DiffLine({ 
  change, 
  lineNumber, 
  comments, 
  onAddComment, 
  onDeleteComment,
  highlightedContent,
  showAddComment,
  showCommentInput,
  onSaveComment,
  onCancelComment
}: DiffLineProps) {
  const getLineClass = () => {
    switch (change.type) {
      case 'insert':
        return 'bg-green-50 border-l-4 border-green-400';
      case 'delete':
        return 'bg-red-50 border-l-4 border-red-400';
      default:
        return 'bg-white hover:bg-gray-50';
    }
  };

  const getLinePrefix = () => {
    switch (change.type) {
      case 'insert':
        return <span className="text-green-600 font-bold">+</span>;
      case 'delete':
        return <span className="text-red-600 font-bold">-</span>;
      default:
        return <span className="text-gray-400"> </span>;
    }
  };

  return (
    <div id={`line-${lineNumber}`}>
      <div className={`group flex items-center transition-all duration-300 ${getLineClass()}`}>
        <div className="flex-shrink-0 w-16 px-2 py-1 text-xs text-gray-500 border-r border-gray-200 bg-gray-50">
          {change.oldLineNumber && (
            <span className="block text-right">{change.oldLineNumber}</span>
          )}
          {change.newLineNumber && (
            <span className="block text-right">{change.newLineNumber}</span>
          )}
        </div>
        <div className="flex-shrink-0 w-8 px-2 py-1 text-center">
          {getLinePrefix()}
        </div>
        <div className="flex-1 px-2 py-1 font-mono text-sm text-left">
          <code 
            dangerouslySetInnerHTML={{ __html: highlightedContent }}
            className="whitespace-pre block leading-5"
          />
        </div>
        {showAddComment && (
          <div className="flex-shrink-0 px-2">
            <button
              onClick={() => onAddComment?.(lineNumber)}
              className="opacity-0 group-hover:opacity-100 p-1 text-gray-400 hover:text-blue-600 transition-opacity"
              title="添加评论"
            >
              <PlusIcon className="h-4 w-4" />
            </button>
          </div>
        )}
      </div>
      
      {/* 显示该行的评论 */}
      {comments.length > 0 && (
        <div className="pl-24 pr-4">
          {comments.map((comment) => (
            <CommentBubble 
              key={comment.id} 
              comment={comment} 
              onDelete={onDeleteComment}
            />
          ))}
        </div>
      )}
      
      {/* 评论输入框 */}
      {showCommentInput && (
        <div className="pl-24 pr-4">
          <CommentInput
            filePath=""
            lineNumber={lineNumber}
            onSave={onSaveComment}
            onCancel={onCancelComment}
          />
        </div>
      )}
    </div>
  );
});

// 导出FileViewerProps供外部使用
export interface FileViewerProps {
  file: DiffFile;
  comments: CodeComment[];
  onAddComment?: (filePath: string, lineNumber: number, content: string) => void;
  onDeleteComment?: (commentId: string) => void;
  language: string;
  isActive: boolean; // 新增：是否是激活状态
}

// 导出FileViewer组件供LazyDiffViewer使用
export function FileViewer({ file, comments, onAddComment, onDeleteComment, language, isActive }: FileViewerProps) {
  const { highlightCode } = useCodeHighlight();
  const [commentingLine, setCommentingLine] = useState<number | null>(null);
  
  // 计算总行数
  const totalLines = useMemo(() => {
    return file.hunks.reduce((total, hunk) => total + hunk.changes.length, 0);
  }, [file.hunks]);
  
  // 根据文件大小动态设置初始行数
  const initialLines = totalLines > HUGE_DIFF_THRESHOLD ? INITIAL_LINES_FOR_HUGE_FILE : INITIAL_LINES_TO_SHOW;
  const [visibleLines, setVisibleLines] = useState(initialLines);
  const [renderedLines, setRenderedLines] = useState(initialLines); // 实际渲染的行数（用于分块DOM渲染）
  const [isExpanding, setIsExpanding] = useState(false); // 是否正在展开
  const [highlightedLines, setHighlightedLines] = useState<Map<number, string>>(new Map());
  const [isRendered, setIsRendered] = useState(false); // 新增：延迟渲染标记

  const isLargeDiff = totalLines > LARGE_DIFF_THRESHOLD;
  const isHugeDiff = totalLines > HUGE_DIFF_THRESHOLD; // 超大文件标记
  const hasMoreToShow = renderedLines < totalLines; // 使用renderedLines判断
  
  // 激活状态切换：立即渲染，快速清理
  useEffect(() => {
    if (isActive) {
      // 立即渲染，不延迟
      setIsRendered(true);
    } else {
      // 文件失去激活状态时，快速清理内存（100ms）
      const timer = setTimeout(() => {
        setIsRendered(false);
        setHighlightedLines(new Map()); // 立即清空高亮缓存
        setVisibleLines(initialLines); // 重置行数到初始值
        setRenderedLines(initialLines); // 重置渲染行数
        setIsExpanding(false); // 重置展开状态
        setCommentingLine(null); // 清空评论状态
      }, FILE_SWITCH_CLEANUP_DELAY_MS);
      return () => clearTimeout(timer);
    }
  }, [isActive, initialLines]);
  
  // 使用useEffect管理高亮任务的生命周期
  // 修复无限循环：直接在useEffect中执行高亮，避免依赖performHighlighting
  // 性能优化：超大文件使用分块渲染，避免阻塞主线程
  useEffect(() => {
    if (!isRendered) return; // 未渲染时不执行高亮
    
    let timerId: number | undefined;
    let idleCallbackId: number | undefined;
    let isCancelled = false; // 取消标记
    
    // 清理函数：取消所有待执行的任务
    const cleanup = () => {
      isCancelled = true;
      if (timerId !== undefined) clearTimeout(timerId);
      if (idleCallbackId !== undefined && 'cancelIdleCallback' in window) {
        window.cancelIdleCallback(idleCallbackId);
      }
    };
    
    // 分块高亮函数：将大任务拆分成小任务，避免阻塞
    const doChunkedHighlighting = async () => {
      const newHighlightedLines = new Map<number, string>();
      let lineCount = 0;
      const lang = detectLanguageFromPath(file.newPath || file.oldPath, language);
      const allChanges: Array<{ change: DiffChange }> = [];
      
      // 收集所有要处理的行
      for (const hunk of file.hunks) {
        if (lineCount >= visibleLines) break;
        for (const change of hunk.changes) {
          if (lineCount >= visibleLines) break;
          allChanges.push({ change });
          lineCount++;
        }
      }
      
      // 分块处理：超大文件时，每批只处理CHUNK_SIZE行
      if (isHugeDiff) {
        // 分块渲染模式
        for (let i = 0; i < allChanges.length; i += CHUNK_SIZE) {
          if (isCancelled) return; // 检查是否已取消
          
          const chunk = allChanges.slice(i, i + CHUNK_SIZE);
          
          // 处理当前批次
          await new Promise<void>(resolve => {
            requestAnimationFrame(() => {
              for (const { change } of chunk) {
                const lineNumber = change.newLineNumber || change.oldLineNumber || 0;
                
                if (change.type === 'normal') {
                  newHighlightedLines.set(lineNumber, escapeHtml(change.content));
                } else {
                  const highlighted = highlightCode(change.content, lang);
                  newHighlightedLines.set(lineNumber, highlighted);
                }
              }
              resolve();
            });
          });
          
          // 每批次后更新UI，让用户看到进度
          if ((i + CHUNK_SIZE) % (CHUNK_SIZE * 3) === 0) {
            setHighlightedLines(new Map(newHighlightedLines));
          }
        }
      } else {
        // 小文件直接处理
        for (const { change } of allChanges) {
          const lineNumber = change.newLineNumber || change.oldLineNumber || 0;
          
          if (change.type === 'normal') {
            newHighlightedLines.set(lineNumber, escapeHtml(change.content));
          } else {
            const highlighted = highlightCode(change.content, lang);
            newHighlightedLines.set(lineNumber, highlighted);
          }
        }
      }
      
      // 最终批量更新
      if (!isCancelled) {
        setHighlightedLines(newHighlightedLines);
      }
    };
    
    if (isLargeDiff) {
      // 对于大文件，延迟高亮并使用空闲时间
      timerId = window.setTimeout(() => {
        if ('requestIdleCallback' in window) {
          idleCallbackId = window.requestIdleCallback(() => {
            doChunkedHighlighting();
          });
        } else {
          doChunkedHighlighting();
        }
      }, HIGHLIGHT_DEBOUNCE_MS);
    } else {
      // 小文件立即高亮
      doChunkedHighlighting();
    }
    
    // 组件卸载或依赖变化时，取消所有待执行任务
    return cleanup;
    // eslint-disable-next-line react-hooks/exhaustive-deps
  }, [file, visibleLines, language, isRendered, isLargeDiff, isHugeDiff]); // 移除highlightCode避免无限循环

  // 简单的HTML转义
  const escapeHtml = (text: string) => {
    const div = document.createElement('div');
    div.textContent = text;
    return div.innerHTML;
  };
  
  const detectLanguageFromPath = (path: string, fallback: string) => {
    if (fallback && fallback !== 'auto') return fallback;
    const lower = (path || '').toLowerCase();
    if (lower.endsWith('.ts')) return 'typescript';
    if (lower.endsWith('.tsx')) return 'tsx';
    if (lower.endsWith('.js')) return 'javascript';
    if (lower.endsWith('.jsx')) return 'jsx';
    if (lower.endsWith('.cs')) return 'csharp';
    if (lower.endsWith('.json')) return 'json';
    if (lower.endsWith('.css')) return 'css';
    if (lower.endsWith('.scss')) return 'scss';
    if (lower.endsWith('.less')) return 'less';
    if (lower.endsWith('.html') || lower.endsWith('.htm')) return 'markup';
    if (lower.endsWith('.xml')) return 'markup';
    if (lower.endsWith('.py')) return 'python';
    if (lower.endsWith('.java')) return 'java';
    if (lower.endsWith('.go')) return 'go';
    if (lower.endsWith('.rs')) return 'rust';
    if (lower.endsWith('.sql')) return 'sql';
    if (lower.endsWith('.yml') || lower.endsWith('.yaml')) return 'yaml';
    if (lower.endsWith('.md')) return 'markdown';
    if (lower.endsWith('.sh')) return 'bash';
    if (lower.endsWith('.ps1')) return 'powershell';
    if (lower.includes('dockerfile')) return 'docker';
    if (lower.endsWith('.toml')) return 'toml';
    return 'javascript';
  };

  const getCommentsForLine = useCallback((lineNumber: number): CodeComment[] => {
    return comments.filter(comment => 
      comment.filePath === (file.newPath || file.oldPath) && 
      comment.lineNumber === lineNumber
    );
  }, [comments, file.newPath, file.oldPath]);

  const handleAddComment = useCallback((lineNumber: number) => {
    setCommentingLine(lineNumber);
  }, []);

  const handleSaveComment = useCallback((content: string) => {
    if (commentingLine && onAddComment) {
      onAddComment(file.newPath || file.oldPath, commentingLine, content);
    }
    setCommentingLine(null);
  }, [commentingLine, onAddComment, file.newPath, file.oldPath]);

  const handleCancelComment = useCallback(() => {
    setCommentingLine(null);
  }, []);

  const handleLoadMore = useCallback(() => {
    const newVisibleLines = Math.min(visibleLines + LINES_TO_LOAD_MORE, totalLines);
    setVisibleLines(newVisibleLines);
    // 同步更新渲染行数
    setRenderedLines(newVisibleLines);
  }, [visibleLines, totalLines]);

  const handleShowAll = useCallback(() => {
    if (isHugeDiff) {
      // 超大文件：分块渲染DOM，避免一次性渲染导致卡顿
      setIsExpanding(true);
      setVisibleLines(totalLines);
      
      // 使用requestAnimationFrame分批渲染
      let currentRendered = renderedLines;
      const batchSize = LINES_TO_LOAD_MORE * 2; // 每批渲染100行
      
      const renderNextBatch = () => {
        if (currentRendered >= totalLines) {
          setIsExpanding(false);
          return;
        }
        
        currentRendered = Math.min(currentRendered + batchSize, totalLines);
        setRenderedLines(currentRendered);
        
        // 继续下一批
        requestAnimationFrame(renderNextBatch);
      };
      
      requestAnimationFrame(renderNextBatch);
    } else {
      // 小文件：直接展开
      setVisibleLines(totalLines);
      setRenderedLines(totalLines);
    }
  }, [isHugeDiff, totalLines, renderedLines]);

  // 如果未渲染，显示加载占位符
  if (!isRendered) {
    return (
      <div className="flex-1 flex items-center justify-center bg-gray-50">
        <div className="text-center">
          <div className="animate-spin rounded-full h-12 w-12 border-b-2 border-blue-600 mx-auto mb-4"></div>
          <p className="text-gray-600">加载文件内容中...</p>
          <p className="text-xs text-gray-500 mt-2">{file.newPath || file.oldPath}</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex-1 overflow-auto">
      {/* 内容容器，设置min-width确保长行代码不会被截断 */}
      <div className="min-w-max">
        <div className="sticky top-0 bg-white border-b border-gray-200 p-4 z-10">
          <h2 className="text-lg font-semibold text-gray-900">
            {file.newPath || file.oldPath}
          </h2>
          <div className="flex items-center space-x-4 mt-2 text-sm text-gray-600">
            <span>语言: {detectLanguageFromPath(file.newPath || file.oldPath, language)}</span>
            <span>变更类型: {file.type}</span>
            {file.oldPath !== file.newPath && (
              <span>重命名: {file.oldPath} → {file.newPath}</span>
            )}
            {isHugeDiff && (
              <span className="text-red-600 font-medium">
                🔥 超大文件 ({totalLines} 行) - 分块渲染中
              </span>
            )}
            {isLargeDiff && !isHugeDiff && (
              <span className="text-orange-600 font-medium">
                ⚠️ 大文件 ({totalLines} 行变更)
              </span>
            )}
          </div>
          {isLargeDiff && hasMoreToShow && (
            <div className="mt-2 text-xs text-gray-500">
              正在显示前 {visibleLines} / {totalLines} 行
              {isExpanding && renderedLines < visibleLines && (
                <span className="ml-2 text-blue-600 animate-pulse">
                  • 渲染进度: {renderedLines} / {visibleLines} 行
                </span>
              )}
              {isHugeDiff && highlightedLines.size < visibleLines && !isExpanding && (
                <span className="ml-2 text-blue-600">
                  • 高亮进度: {highlightedLines.size} / {visibleLines} 行
                </span>
              )}
            </div>
          )}
        </div> {/* 闭合 sticky header */}
      
        <div className="divide-y divide-gray-200">
          {file.hunks.map((hunk, hunkIndex) => {
            // 计算当前hunk之前已经显示的行数
            let linesBefore = 0;
          for (let i = 0; i < hunkIndex; i++) {
            linesBefore += file.hunks[i].changes.length;
          }
          
          // 使用renderedLines而不是visibleLines来控制实际渲染的DOM
          if (linesBefore >= renderedLines) {
            return null;
          }

          // 计算这个hunk中要显示的行数
          const linesToShowInThisHunk = Math.min(
            hunk.changes.length,
            renderedLines - linesBefore
          );

          return (
            <div key={hunkIndex}>
              <div className="bg-gray-100 px-4 py-2 text-sm font-mono text-gray-600">
                @@ -{hunk.oldStart},{hunk.oldLines} +{hunk.newStart},{hunk.newLines} @@
              </div>
              {hunk.changes.slice(0, linesToShowInThisHunk).map((change, changeIndex) => {
                const lineNumber = change.newLineNumber || change.oldLineNumber || 0;
                const lineComments = getCommentsForLine(lineNumber);
                // 使用缓存的高亮结果，如果不存在则使用原始内容
                const highlightedContent = highlightedLines.get(lineNumber) || escapeHtml(change.content);

                return (
                  <DiffLine
                    key={`${hunkIndex}-${changeIndex}`}
                    change={change}
                    lineNumber={lineNumber}
                    comments={lineComments}
                    onAddComment={handleAddComment}
                    onDeleteComment={onDeleteComment}
                    highlightedContent={highlightedContent}
                    showAddComment={!!onAddComment && change.type !== 'delete'}
                    showCommentInput={commentingLine === lineNumber}
                    onSaveComment={handleSaveComment}
                    onCancelComment={handleCancelComment}
                  />
                );
              })}
            </div>
          );
        })}
        </div> {/* 闭合 divide-y divide-gray-200 */}
      
        {/* 加载更多按钮 */}
        {hasMoreToShow && (
          <div className="sticky bottom-0 bg-gradient-to-t from-white via-white to-transparent p-6 text-center border-t border-gray-200">
            <div className="space-y-3">
              <p className="text-sm text-gray-600">
                已显示 {visibleLines} / {totalLines} 行变更
              </p>
              <div className="flex items-center justify-center space-x-3">
                <button
                  onClick={handleLoadMore}
                  className="inline-flex items-center px-4 py-2 border border-gray-300 rounded-md shadow-sm text-sm font-medium text-gray-700 bg-white hover:bg-gray-50 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
                >
                  <ChevronDownIcon className="h-4 w-4 mr-2" />
                  加载更多 ({LINES_TO_LOAD_MORE} 行)
                </button>
                <button
                  onClick={handleShowAll}
                  className="inline-flex items-center px-4 py-2 border border-transparent rounded-md shadow-sm text-sm font-medium text-white bg-blue-600 hover:bg-blue-700 focus:outline-none focus:ring-2 focus:ring-offset-2 focus:ring-blue-500 transition-colors"
                >
                  显示全部 ({totalLines - visibleLines} 行)
                </button>
              </div>
              <p className="text-xs text-gray-500">
                💡 提示：大文件分批加载可以提升浏览器性能
              </p>
            </div>
          </div>
        )}
      </div>
    </div>
  );
}

export function DiffViewer({ 
  files, 
  comments = [], 
  onAddComment, 
  onDeleteComment, 
  language = 'javascript',
  targetFileAndLine
}: DiffViewerProps) {
  const [selectedFile, setSelectedFile] = useState<string | null>(
    files.length > 0 ? (files[0].newPath || files[0].oldPath) : null
  );

  // 当targetFileAndLine改变时，自动切换到目标文件并滚动到目标行
  useEffect(() => {
    if (targetFileAndLine) {
      const { filePath, lineNumber } = targetFileAndLine;
      // 切换到目标文件
      setSelectedFile(filePath);
      // 延迟滚动，确保DOM已渲染
      setTimeout(() => {
        const element = document.getElementById(`line-${lineNumber}`);
        if (element) {
          element.scrollIntoView({ behavior: 'smooth', block: 'center' });
          // 添加高亮效果
          element.classList.add('highlight-flash');
          setTimeout(() => element.classList.remove('highlight-flash'), 2000);
        }
      }, 300);
    }
  }, [targetFileAndLine]);

  const selectedFileData = files.find(f => 
    (f.newPath || f.oldPath) === selectedFile
  );

  if (files.length === 0) {
    return (
      <div className="flex items-center justify-center h-64 text-gray-500">
        <div className="text-center">
          <ChatBubbleLeftIcon className="h-12 w-12 mx-auto mb-4 text-gray-400" />
          <p>暂无代码变更</p>
        </div>
      </div>
    );
  }

  return (
    <div className="flex h-full bg-white border border-gray-200 rounded-lg overflow-hidden">
      <FileTree 
        files={files}
        selectedFile={selectedFile}
        onSelectFile={setSelectedFile}
      />
      {selectedFileData ? (
        <FileViewer
          file={selectedFileData}
          comments={comments}
          onAddComment={onAddComment}
          onDeleteComment={onDeleteComment}
          language={language}
          isActive={true} // 当前显示的文件总是激活状态
        />
      ) : (
        <div className="flex-1 flex items-center justify-center text-gray-500">
          选择一个文件查看详情
        </div>
      )}
    </div>
  );
}