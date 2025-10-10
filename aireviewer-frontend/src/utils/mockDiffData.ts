import type { DiffFile, CodeComment } from '../types/diff';

export function generateMockDiffData(): DiffFile[] {
  return [
    {
      oldPath: 'src/components/UserProfile.tsx',
      newPath: 'src/components/UserProfile.tsx',
      type: 'modify',
      hunks: [
        {
          oldStart: 1,
          oldLines: 20,
          newStart: 1,
          newLines: 25,
          changes: [
            {
              type: 'normal',
              lineNumber: 1,
              oldLineNumber: 1,
              newLineNumber: 1,
              content: 'import React, { useState, useEffect } from \'react\';'
            },
            {
              type: 'normal',
              lineNumber: 2,
              oldLineNumber: 2,
              newLineNumber: 2,
              content: 'import { User } from \'../types/user\';'
            },
            {
              type: 'insert',
              lineNumber: 3,
              newLineNumber: 3,
              content: 'import { useAuth } from \'../hooks/useAuth\';'
            },
            {
              type: 'normal',
              lineNumber: 4,
              oldLineNumber: 3,
              newLineNumber: 4,
              content: ''
            },
            {
              type: 'normal',
              lineNumber: 5,
              oldLineNumber: 4,
              newLineNumber: 5,
              content: 'interface UserProfileProps {'
            },
            {
              type: 'normal',
              lineNumber: 6,
              oldLineNumber: 5,
              newLineNumber: 6,
              content: '  user: User;'
            },
            {
              type: 'delete',
              lineNumber: 7,
              oldLineNumber: 6,
              content: '  onUpdate: (user: User) => void;'
            },
            {
              type: 'insert',
              lineNumber: 8,
              newLineNumber: 7,
              content: '  onUpdate?: (user: User) => void;'
            },
            {
              type: 'normal',
              lineNumber: 9,
              oldLineNumber: 7,
              newLineNumber: 8,
              content: '}'
            },
            {
              type: 'normal',
              lineNumber: 10,
              oldLineNumber: 8,
              newLineNumber: 9,
              content: ''
            },
            {
              type: 'normal',
              lineNumber: 11,
              oldLineNumber: 9,
              newLineNumber: 10,
              content: 'export function UserProfile({ user, onUpdate }: UserProfileProps) {'
            },
            {
              type: 'insert',
              lineNumber: 12,
              newLineNumber: 11,
              content: '  const { currentUser } = useAuth();'
            },
            {
              type: 'normal',
              lineNumber: 13,
              oldLineNumber: 10,
              newLineNumber: 12,
              content: '  const [editing, setEditing] = useState(false);'
            },
            {
              type: 'normal',
              lineNumber: 14,
              oldLineNumber: 11,
              newLineNumber: 13,
              content: ''
            },
            {
              type: 'insert',
              lineNumber: 15,
              newLineNumber: 14,
              content: '  const canEdit = currentUser?.id === user.id;'
            },
            {
              type: 'normal',
              lineNumber: 16,
              oldLineNumber: 12,
              newLineNumber: 15,
              content: '  const handleSave = async () => {'
            },
            {
              type: 'delete',
              lineNumber: 17,
              oldLineNumber: 13,
              content: '    onUpdate(user);'
            },
            {
              type: 'insert',
              lineNumber: 18,
              newLineNumber: 16,
              content: '    onUpdate?.(user);'
            },
            {
              type: 'normal',
              lineNumber: 19,
              oldLineNumber: 14,
              newLineNumber: 17,
              content: '    setEditing(false);'
            },
            {
              type: 'normal',
              lineNumber: 20,
              oldLineNumber: 15,
              newLineNumber: 18,
              content: '  };'
            }
          ]
        }
      ]
    },
    {
      oldPath: 'src/utils/validation.ts',
      newPath: 'src/utils/validation.ts',
      type: 'add',
      hunks: [
        {
          oldStart: 0,
          oldLines: 0,
          newStart: 1,
          newLines: 15,
          changes: [
            {
              type: 'insert',
              lineNumber: 1,
              newLineNumber: 1,
              content: 'export function validateEmail(email: string): boolean {'
            },
            {
              type: 'insert',
              lineNumber: 2,
              newLineNumber: 2,
              content: '  const emailRegex = /^[^\\s@]+@[^\\s@]+\\.[^\\s@]+$/;'
            },
            {
              type: 'insert',
              lineNumber: 3,
              newLineNumber: 3,
              content: '  return emailRegex.test(email);'
            },
            {
              type: 'insert',
              lineNumber: 4,
              newLineNumber: 4,
              content: '}'
            },
            {
              type: 'insert',
              lineNumber: 5,
              newLineNumber: 5,
              content: ''
            },
            {
              type: 'insert',
              lineNumber: 6,
              newLineNumber: 6,
              content: 'export function validatePassword(password: string): { valid: boolean; errors: string[] } {'
            },
            {
              type: 'insert',
              lineNumber: 7,
              newLineNumber: 7,
              content: '  const errors: string[] = [];'
            },
            {
              type: 'insert',
              lineNumber: 8,
              newLineNumber: 8,
              content: '  '
            },
            {
              type: 'insert',
              lineNumber: 9,
              newLineNumber: 9,
              content: '  if (password.length < 6) {'
            },
            {
              type: 'insert',
              lineNumber: 10,
              newLineNumber: 10,
              content: '    errors.push(\'密码长度至少6位\');'
            },
            {
              type: 'insert',
              lineNumber: 11,
              newLineNumber: 11,
              content: '  }'
            },
            {
              type: 'insert',
              lineNumber: 12,
              newLineNumber: 12,
              content: '  '
            },
            {
              type: 'insert',
              lineNumber: 13,
              newLineNumber: 13,
              content: '  return { valid: errors.length === 0, errors };'
            },
            {
              type: 'insert',
              lineNumber: 14,
              newLineNumber: 14,
              content: '}'
            }
          ]
        }
      ]
    },
    {
      oldPath: 'src/components/OldComponent.tsx',
      newPath: '',
      type: 'delete',
      hunks: [
        {
          oldStart: 1,
          oldLines: 10,
          newStart: 0,
          newLines: 0,
          changes: [
            {
              type: 'delete',
              lineNumber: 1,
              oldLineNumber: 1,
              content: 'import React from \'react\';'
            },
            {
              type: 'delete',
              lineNumber: 2,
              oldLineNumber: 2,
              content: ''
            },
            {
              type: 'delete',
              lineNumber: 3,
              oldLineNumber: 3,
              content: '// This component is deprecated'
            },
            {
              type: 'delete',
              lineNumber: 4,
              oldLineNumber: 4,
              content: 'export function OldComponent() {'
            },
            {
              type: 'delete',
              lineNumber: 5,
              oldLineNumber: 5,
              content: '  return <div>Old functionality</div>;'
            },
            {
              type: 'delete',
              lineNumber: 6,
              oldLineNumber: 6,
              content: '}'
            }
          ]
        }
      ]
    }
  ];
}

export function generateMockComments(): CodeComment[] {
  return [
    {
      id: 'comment-1',
      filePath: 'src/components/UserProfile.tsx',
      lineNumber: 3,
      content: '建议添加类型导入以提高类型安全性。',
      author: 'AI Assistant',
      createdAt: new Date(Date.now() - 3600000).toISOString(),
      type: 'ai',
      severity: 'info'
    },
    {
      id: 'comment-2',
      filePath: 'src/components/UserProfile.tsx',
      lineNumber: 8,
      content: '很好的改进！使用可选参数可以提高组件的灵活性。',
      author: 'AI Assistant',
      createdAt: new Date(Date.now() - 3000000).toISOString(),
      type: 'ai',
      severity: 'info'
    },
    {
      id: 'comment-3',
      filePath: 'src/components/UserProfile.tsx',
      lineNumber: 15,
      content: '添加权限检查是一个很好的安全实践。',
      author: '张三',
      createdAt: new Date(Date.now() - 1800000).toISOString(),
      type: 'human'
    },
    {
      id: 'comment-4',
      filePath: 'src/utils/validation.ts',
      lineNumber: 2,
      content: '建议使用更严格的邮箱验证正则表达式。',
      author: 'AI Assistant',
      createdAt: new Date(Date.now() - 1200000).toISOString(),
      type: 'ai',
      severity: 'warning'
    },
    {
      id: 'comment-5',
      filePath: 'src/utils/validation.ts',
      lineNumber: 9,
      content: '密码验证规则可以考虑添加更多复杂度要求，如大小写字母、数字和特殊字符。',
      author: 'AI Assistant',
      createdAt: new Date(Date.now() - 600000).toISOString(),
      type: 'ai',
      severity: 'warning'
    }
  ];
}