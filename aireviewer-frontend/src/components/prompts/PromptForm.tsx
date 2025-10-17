import React, { useState, useEffect } from 'react';
import type { CreatePromptRequest, PromptDto, PromptType, UpdatePromptRequest } from '../../types/prompt';

interface Props {
  isOpen: boolean;
  onClose: () => void;
  onSubmit: (data: CreatePromptRequest | UpdatePromptRequest, mode: 'create' | 'update') => void;
  editing?: PromptDto | null;
  scope: { user?: boolean; projectId?: number };
}

const PromptForm: React.FC<Props> = ({ isOpen, onClose, onSubmit, editing, scope }) => {
  const [type, setType] = useState<PromptType>('Review');
  const [name, setName] = useState('');
  const [content, setContent] = useState('');

  useEffect(() => {
    if (editing) {
      setType(editing.type);
      setName(editing.name);
      setContent(editing.content);
    } else {
      setType('Review');
      setName('');
      setContent('');
    }
  }, [editing]);

  if (!isOpen) return null;

  const handleSubmit = (e: React.FormEvent) => {
    e.preventDefault();
    if (editing) {
      const req: UpdatePromptRequest = { name, content };
      onSubmit(req, 'update');
    } else {
      const req: CreatePromptRequest = {
        type,
        name,
        content,
        userId: scope.user ? undefined : undefined,
        projectId: scope.projectId,
      };
      onSubmit(req, 'create');
    }
  };

  return (
    <div className="fixed inset-0 z-50 overflow-y-auto">
      <div className="flex min-h-screen items-center justify-center p-4">
        <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" aria-hidden="true" onClick={onClose}></div>
        <div className="relative w-full max-w-3xl transform overflow-hidden rounded-lg bg-white p-6 shadow-xl">
          <div className="flex items-center justify-between mb-4">
            <h3 className="text-lg font-medium text-gray-900">{editing ? '编辑 Prompt' : '新建 Prompt'}</h3>
            <button onClick={onClose} className="text-gray-400 hover:text-gray-600">✕</button>
          </div>
          <form onSubmit={handleSubmit} className="space-y-4">
            {!editing && (
              <div>
                <label className="block text-sm font-medium text-gray-700 mb-1">类型</label>
                <select
                  value={type}
                  onChange={(e: React.ChangeEvent<HTMLSelectElement>) => setType(e.target.value as PromptType)}
                  className="input w-full"
                >
                  <option value="Review">代码评审</option>
                  <option value="RiskAnalysis">风险分析</option>
                  <option value="PullRequestSummary">变更摘要</option>
                  <option value="ImprovementSuggestions">改进建议</option>
                </select>
              </div>
            )}
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1">名称</label>
              <input
                value={name}
                onChange={(e: React.ChangeEvent<HTMLInputElement>) => setName(e.target.value)}
                className="input w-full"
                placeholder="例如：团队标准评审模版"
                required
              />
            </div>
            <div>
              <label className="block text-sm font-medium text-gray-700 mb-1 flex items-center justify-between">
                <span>内容</span>
                <span className="text-xs text-gray-500">支持占位符：{'{{CONTEXT}}'}, {'{{DIFF}}'}, {'{{FILE_NAME}}'}, {'{{FILES_SUMMARY}}'}, {'{{DIFF_HEAD}}'}</span>
              </label>
              <textarea
                value={content}
                onChange={(e: React.ChangeEvent<HTMLTextAreaElement>) => setContent(e.target.value)}
                className="input w-full h-72 font-mono"
                placeholder="在此粘贴你的 Prompt 模板，务必引导 LLM 仅输出固定 JSON Schema"
                required
              />
            </div>
            <div className="flex justify-end space-x-3 pt-2">
              <button type="button" onClick={onClose} className="btn btn-secondary">取消</button>
              <button type="submit" className="btn btn-primary">{editing ? '保存' : '创建'}</button>
            </div>
          </form>
        </div>
      </div>
    </div>
  );
};

export default PromptForm;
