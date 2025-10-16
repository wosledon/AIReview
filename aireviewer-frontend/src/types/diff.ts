export interface DiffResponse {
  files: DiffFile[];
  comments: CodeComment[];
}

// 新增：轻量级文件列表响应
export interface DiffFileListResponse {
  files: DiffFileMetadata[];
  comments: CodeComment[];
  totalFiles: number;
  totalAddedLines: number;
  totalDeletedLines: number;
}

// 新增：文件元数据（不包含diff内容）
export interface DiffFileMetadata {
  oldPath: string;
  newPath: string;
  type: 'add' | 'delete' | 'modify' | 'rename';
  addedLines: number;
  deletedLines: number;
  totalChanges: number; // hunk数量
}

// 新增：单个文件的完整diff响应
export interface DiffFileDetailResponse {
  file: DiffFile;
  comments: CodeComment[];
}


export interface DiffFile {
  oldPath: string;
  newPath: string;
  type: 'add' | 'delete' | 'modify' | 'rename';
  hunks: DiffHunk[];
}

export interface DiffHunk {
  oldStart: number;
  oldLines: number;
  newStart: number;
  newLines: number;
  changes: DiffChange[];
}

export interface DiffChange {
  type: 'insert' | 'delete' | 'normal';
  lineNumber: number;
  content: string;
  oldLineNumber?: number;
  newLineNumber?: number;
}

export interface CodeComment {
  id: string;
  filePath: string;
  lineNumber: number;
  content: string;
  author: string;
  createdAt: string;
  type: 'human' | 'ai';
  severity?: 'info' | 'warning' | 'error' | 'critical';
}

export interface DiffViewerProps {
  files: DiffFile[];
  comments?: CodeComment[];
  onAddComment?: (filePath: string, lineNumber: number, content: string) => void;
  onDeleteComment?: (commentId: string) => void;
  language?: string;
  showLineNumbers?: boolean;
  splitView?: boolean;
  targetFileAndLine?: { filePath: string; lineNumber: number } | null;
}