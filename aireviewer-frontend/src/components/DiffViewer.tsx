import { useState } from 'react';
import { 
  ChatBubbleLeftIcon, 
  PlusIcon, 
  XMarkIcon,
  ExclamationTriangleIcon,
  InformationCircleIcon,
  ExclamationCircleIcon
} from '@heroicons/react/24/outline';
import { useCodeHighlight } from '../hooks/useCodeHighlight';
import type { DiffFile, DiffChange, CodeComment, DiffViewerProps } from '../types/diff';

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

  return (
    <div className="w-80 bg-gray-50 border-r border-gray-200 p-4">
      <h3 className="text-sm font-semibold text-gray-900 mb-3">文件变更 ({files.length})</h3>
      <div className="space-y-1">
        {files.map((file, index) => (
          <button
            key={`${file.oldPath}-${file.newPath}-${index}`}
            onClick={() => onSelectFile(file.newPath || file.oldPath)}
            className={`w-full text-left p-2 rounded-md text-sm flex items-center space-x-2 hover:bg-gray-100 ${
              selectedFile === (file.newPath || file.oldPath) 
                ? 'bg-blue-100 text-blue-800' 
                : 'text-gray-700'
            }`}
          >
            {getFileIcon(file.type)}
            <span className="truncate">{file.newPath || file.oldPath}</span>
          </button>
        ))}
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

function DiffLine({ 
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
    <div>
      <div className={`group flex items-center ${getLineClass()}`}>
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
        <div className="flex-1 px-2 py-1 font-mono text-sm">
          <code 
            dangerouslySetInnerHTML={{ __html: highlightedContent }}
            className="whitespace-pre"
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
}

interface FileViewerProps {
  file: DiffFile;
  comments: CodeComment[];
  onAddComment?: (filePath: string, lineNumber: number, content: string) => void;
  onDeleteComment?: (commentId: string) => void;
  language: string;
}

function FileViewer({ file, comments, onAddComment, onDeleteComment, language }: FileViewerProps) {
  const { highlightCode } = useCodeHighlight();
  const [commentingLine, setCommentingLine] = useState<number | null>(null);

  const getCommentsForLine = (lineNumber: number): CodeComment[] => {
    return comments.filter(comment => 
      comment.filePath === (file.newPath || file.oldPath) && 
      comment.lineNumber === lineNumber
    );
  };

  const handleAddComment = (lineNumber: number) => {
    setCommentingLine(lineNumber);
  };

  const handleSaveComment = (content: string) => {
    if (commentingLine && onAddComment) {
      onAddComment(file.newPath || file.oldPath, commentingLine, content);
    }
    setCommentingLine(null);
  };

  const handleCancelComment = () => {
    setCommentingLine(null);
  };

  return (
    <div className="flex-1 overflow-auto">
      <div className="sticky top-0 bg-white border-b border-gray-200 p-4">
        <h2 className="text-lg font-semibold text-gray-900">
          {file.newPath || file.oldPath}
        </h2>
        <div className="flex items-center space-x-4 mt-2 text-sm text-gray-600">
          <span>语言: {language}</span>
          <span>变更类型: {file.type}</span>
          {file.oldPath !== file.newPath && (
            <span>重命名: {file.oldPath} → {file.newPath}</span>
          )}
        </div>
      </div>
      
      <div className="divide-y divide-gray-200">
        {file.hunks.map((hunk, hunkIndex) => (
          <div key={hunkIndex}>
            <div className="bg-gray-100 px-4 py-2 text-sm font-mono text-gray-600">
              @@ -{hunk.oldStart},{hunk.oldLines} +{hunk.newStart},{hunk.newLines} @@
            </div>
            {hunk.changes.map((change, changeIndex) => {
              const lineNumber = change.newLineNumber || change.oldLineNumber || 0;
              const lineComments = getCommentsForLine(lineNumber);
              const highlightedContent = highlightCode(change.content, language);
              
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
        ))}
      </div>
    </div>
  );
}

export function DiffViewer({ 
  files, 
  comments = [], 
  onAddComment, 
  onDeleteComment, 
  language = 'javascript' 
}: DiffViewerProps) {
  const [selectedFile, setSelectedFile] = useState<string | null>(
    files.length > 0 ? (files[0].newPath || files[0].oldPath) : null
  );

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
        />
      ) : (
        <div className="flex-1 flex items-center justify-center text-gray-500">
          选择一个文件查看详情
        </div>
      )}
    </div>
  );
}