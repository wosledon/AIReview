import React, { useMemo, useState } from 'react';
import { useQuery, useMutation, useQueryClient } from '@tanstack/react-query';
import { promptsService } from '../../services/prompts.service';
import type { PromptDto, PromptType, EffectivePromptResponse, CreatePromptRequest, UpdatePromptRequest } from '../../types/prompt';
import { PlusIcon, PencilIcon, TrashIcon, EyeIcon, ChevronDownIcon, ChevronUpIcon } from '@heroicons/react/24/outline';
import PromptForm from '../../components/prompts/PromptForm';
import { useParams } from 'react-router-dom';
import { useTranslation } from 'react-i18next';

const SourceBadge: React.FC<{ source: EffectivePromptResponse['source'] }> = ({ source }) => {
  const { t } = useTranslation();
  const map: Record<string, { text: string; cls: string }> = {
    'project': { text: t('prompts.sources.project'), cls: 'bg-indigo-100 text-indigo-800' },
    'user': { text: t('prompts.sources.user'), cls: 'bg-green-100 text-green-800' },
    'built-in': { text: t('prompts.sources.built-in'), cls: 'bg-gray-100 text-gray-800' },
  };
  const v = map[source];
  return <span className={`inline-flex items-center rounded-full px-2.5 py-0.5 text-xs font-medium ${v.cls}`}>{v.text}</span>;
};

const PromptsPage: React.FC = () => {
  const { t } = useTranslation();
  const queryClient = useQueryClient();
  const params = useParams();
  const projectId = params.id ? Number(params.id) : undefined;

  const scope = useMemo(() => ({ projectId }), [projectId]);

  const typeLabels: Record<PromptType, string> = {
    Review: t('prompts.types.Review'),
    RiskAnalysis: t('prompts.types.RiskAnalysis'),
    PullRequestSummary: t('prompts.types.PullRequestSummary'),
    ImprovementSuggestions: t('prompts.types.ImprovementSuggestions'),
  };

  // 获取用户/项目模板
  const { data: userList = [], isLoading: isLoadingUser } = useQuery({
    queryKey: ['prompts', scope.projectId ?? 'user'],
    queryFn: () => scope.projectId ? promptsService.listProjectPrompts(scope.projectId) : promptsService.listUserPrompts(),
  });

  // 获取内置模板
  const { data: builtInList = [], isLoading: isLoadingBuiltIn } = useQuery({
    queryKey: ['prompts', 'built-in'],
    queryFn: () => promptsService.listBuiltInPrompts(),
    enabled: !scope.projectId, // 只在用户级页面显示内置模板
  });

  // 合并列表
  const list = useMemo(() => [...userList, ...builtInList], [userList, builtInList]);
  const isLoading = isLoadingUser || isLoadingBuiltIn;

  const { data: effReview } = useQuery({
    queryKey: ['prompts-effective', 'Review', scope.projectId ?? 'user'],
    queryFn: () => promptsService.getEffective('Review', scope.projectId),
  });

  const { data: effRisk } = useQuery({
    queryKey: ['prompts-effective', 'RiskAnalysis', scope.projectId ?? 'user'],
    queryFn: () => promptsService.getEffective('RiskAnalysis', scope.projectId),
  });

  const { data: effPRSummary } = useQuery({
    queryKey: ['prompts-effective', 'PullRequestSummary', scope.projectId ?? 'user'],
    queryFn: () => promptsService.getEffective('PullRequestSummary', scope.projectId),
  });

  const { data: effImprovements } = useQuery({
    queryKey: ['prompts-effective', 'ImprovementSuggestions', scope.projectId ?? 'user'],
    queryFn: () => promptsService.getEffective('ImprovementSuggestions', scope.projectId),
  });

  const [isFormOpen, setIsFormOpen] = useState(false);
  const [editing, setEditing] = useState<PromptDto | null>(null);
  const [previewing, setPreviewing] = useState<PromptDto | null>(null);
  const [expandedCards, setExpandedCards] = useState<Set<number>>(new Set());

  const createMutation = useMutation({
    mutationFn: (data: CreatePromptRequest) => promptsService.create(data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['prompts'] });
      queryClient.invalidateQueries({ queryKey: ['prompts-effective'] });
      setIsFormOpen(false);
      setEditing(null);
    },
  });

  const updateMutation = useMutation({
    mutationFn: ({ id, data }: { id: number; data: UpdatePromptRequest }) => promptsService.update(id, data),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['prompts'] });
      queryClient.invalidateQueries({ queryKey: ['prompts-effective'] });
      setIsFormOpen(false);
      setEditing(null);
    },
  });

  const deleteMutation = useMutation({
    mutationFn: (id: number) => promptsService.delete(id),
    onSuccess: () => {
      queryClient.invalidateQueries({ queryKey: ['prompts'] });
      queryClient.invalidateQueries({ queryKey: ['prompts-effective'] });
    },
  });

  const openCreate = () => { setEditing(null); setIsFormOpen(true); };
  const openEdit = (p: PromptDto) => { setEditing(p); setIsFormOpen(true); };
  
  const toggleCard = (idx: number) => {
    setExpandedCards(prev => {
      const next = new Set(prev);
      if (next.has(idx)) {
        next.delete(idx);
      } else {
        next.add(idx);
      }
      return next;
    });
  };

  const handleSubmit = (data: CreatePromptRequest | UpdatePromptRequest, mode: 'create' | 'update') => {
    if (mode === 'create') {
      const payload = data as CreatePromptRequest;
      if (scope.projectId) payload.projectId = scope.projectId;
      createMutation.mutate(payload);
    } else if (editing) {
      updateMutation.mutate({ id: editing.id, data: data as UpdatePromptRequest });
    }
  };

  const effCards = [
    { title: t('prompts.effective.reviewTemplate'), eff: effReview },
    { title: t('prompts.effective.riskTemplate'), eff: effRisk },
    { title: t('prompts.effective.prSummaryTemplate'), eff: effPRSummary },
    { title: t('prompts.effective.improvementsTemplate'), eff: effImprovements },
  ];

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="sm:flex sm:items-center">
        <div className="sm:flex-auto">
          <h1 className="text-2xl font-semibold text-gray-900">{t('prompts.title')} {scope.projectId ? t('prompts.scopes.project') : t('prompts.scopes.user')}</h1>
          <p className="mt-2 text-sm text-gray-700">{t('prompts.subtitle')}</p>
        </div>
        <div className="mt-4 sm:ml-16 sm:mt-0 sm:flex-none">
          <button onClick={openCreate} className="inline-flex items-center rounded-md bg-blue-600 px-3 py-2 text-sm font-semibold text-white shadow-sm hover:bg-blue-500">
            <PlusIcon className="-ml-0.5 mr-1.5 h-5 w-5" /> {t('prompts.actions.create')}
          </button>
        </div>
      </div>

      {/* Effective cards - Compact view */}
      <div className="mt-6 bg-white border border-gray-200 rounded-lg divide-y divide-gray-200">
        {effCards.map((c, idx) => {
          const isExpanded = expandedCards.has(idx);
          return (
            <div key={idx} className="p-4">
              <div className="flex items-center justify-between">
                <div className="flex items-center space-x-3 flex-1">
                  <h3 className="text-sm font-medium text-gray-900">{c.title}</h3>
                  {c.eff && <SourceBadge source={c.eff.source} />}
                </div>
                <button
                  onClick={() => toggleCard(idx)}
                  className="ml-4 p-1 text-gray-400 hover:text-gray-600 rounded"
                  title={isExpanded ? t('prompts.actions.collapse') : t('prompts.actions.expand')}
                >
                  {isExpanded ? (
                    <ChevronUpIcon className="h-5 w-5" />
                  ) : (
                    <ChevronDownIcon className="h-5 w-5" />
                  )}
                </button>
              </div>
              {isExpanded && (
                <div className="mt-3 bg-gray-50 p-3 rounded-lg border border-gray-200">
                  <pre className="text-xs font-mono whitespace-pre-wrap break-words max-h-64 overflow-y-auto text-gray-700">
                    {c.eff?.content || t('prompts.loading')}
                  </pre>
                </div>
              )}
            </div>
          );
        })}
      </div>

      {/* List */}
      <div className="mt-8 flow-root">
        <div className="-mx-4 -my-2 overflow-x-auto sm:-mx-6 lg:-mx-8">
          <div className="inline-block min-w-full py-2 align-middle sm:px-6 lg:px-8">
            <div className="overflow-hidden shadow ring-1 ring-black ring-opacity-5 md:rounded-lg">
              <table className="min-w-full divide-y divide-gray-300">
                <thead className="bg-gray-50">
                  <tr>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">{t('prompts.table.name')}</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">{t('prompts.table.type')}</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">{t('prompts.table.scope')}</th>
                    <th className="px-6 py-3 text-left text-xs font-medium text-gray-500 uppercase tracking-wide">{t('prompts.table.updatedAt')}</th>
                    <th className="relative px-6 py-3"><span className="sr-only">{t('prompts.table.actions')}</span></th>
                  </tr>
                </thead>
                <tbody className="divide-y divide-gray-200 bg-white">
                  {isLoading ? (
                    <tr><td className="px-6 py-4" colSpan={5}>{t('prompts.loading')}</td></tr>
                  ) : list.length === 0 ? (
                    <tr><td className="px-6 py-8 text-center text-gray-500" colSpan={5}>{t('prompts.empty')}</td></tr>
                  ) : (
                    list.map(p => {
                      const isBuiltIn = !p.userId && !p.projectId;
                      return (
                      <tr key={p.id} className="hover:bg-gray-50">
                        <td className="px-6 py-4 text-sm text-gray-900">
                          {p.name}
                          {isBuiltIn && <span className="ml-2 inline-flex items-center rounded-full bg-gray-100 px-2 py-0.5 text-xs font-medium text-gray-600">{t('prompts.sources.built-in')}</span>}
                        </td>
                        <td className="px-6 py-4 text-sm text-gray-700">{typeLabels[p.type]}</td>
                        <td className="px-6 py-4 text-sm text-gray-700">{p.projectId ? t('prompts.scopes.project') : (p.userId ? t('prompts.scopes.user') : t('prompts.scopes.system'))}</td>
                        <td className="px-6 py-4 text-sm text-gray-500">{new Date(p.updatedAt).toLocaleString()}</td>
                        <td className="px-6 py-4 text-right">
                          <div className="flex items-center justify-end space-x-2">
                            <button onClick={() => setPreviewing(p)} className="text-gray-600 hover:text-gray-900" title={t('prompts.actions.view')}><EyeIcon className="h-5 w-5" /></button>
                            {!isBuiltIn && (
                              <>
                                <button onClick={() => openEdit(p)} className="text-blue-600 hover:text-blue-900" title={t('prompts.actions.edit')}><PencilIcon className="h-5 w-5" /></button>
                                <button onClick={() => deleteMutation.mutate(p.id)} className="text-red-600 hover:text-red-900" title={t('prompts.actions.delete')}><TrashIcon className="h-5 w-5" /></button>
                              </>
                            )}
                          </div>
                        </td>
                      </tr>
                      );
                    })
                  )}
                </tbody>
              </table>
            </div>
          </div>
        </div>
      </div>

      <PromptForm
        isOpen={isFormOpen}
        onClose={() => { setIsFormOpen(false); setEditing(null); }}
        onSubmit={handleSubmit}
        editing={editing}
        scope={{ projectId: scope.projectId }}
      />

      {/* Preview Modal */}
      {previewing && (
        <div className="fixed inset-0 z-50 overflow-y-auto">
          <div className="flex min-h-screen items-center justify-center p-4">
            <div className="fixed inset-0 bg-gray-500 bg-opacity-75 transition-opacity" onClick={() => setPreviewing(null)}></div>
            <div className="relative w-full max-w-4xl transform overflow-hidden rounded-lg bg-white shadow-xl">
              <div className="bg-white px-6 py-4 border-b border-gray-200">
                <div className="flex items-center justify-between">
                  <div>
                    <h3 className="text-lg font-semibold text-gray-900">{previewing.name}</h3>
                    <p className="mt-1 text-sm text-gray-500">{typeLabels[previewing.type]} · {previewing.projectId ? t('prompts.scopes.project') : (previewing.userId ? t('prompts.scopes.user') : t('prompts.scopes.system'))}</p>
                  </div>
                  <button onClick={() => setPreviewing(null)} className="text-gray-400 hover:text-gray-600">
                    <span className="text-2xl">×</span>
                  </button>
                </div>
              </div>
              <div className="p-6 max-h-[70vh] overflow-y-auto">
                <div className="bg-gray-50 p-4 rounded-lg border border-gray-200">
                  <pre className="text-sm font-mono whitespace-pre-wrap break-words text-gray-800 leading-relaxed">
                    {previewing.content}
                  </pre>
                </div>
              </div>
              <div className="bg-gray-50 px-6 py-4 border-t border-gray-200 flex justify-end space-x-3">
                <button 
                  onClick={() => navigator.clipboard.writeText(previewing.content).then(() => alert(t('prompts.messages.copied')))} 
                  className="btn btn-secondary"
                >
                  {t('prompts.actions.copy')}
                </button>
                <button onClick={() => setPreviewing(null)} className="btn btn-primary">{t('prompts.actions.close')}</button>
              </div>
            </div>
          </div>
        </div>
      )}
    </div>
  );
};

export default PromptsPage;
