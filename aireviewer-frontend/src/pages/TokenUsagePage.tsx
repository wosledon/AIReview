import React, { useState } from 'react';
import { useQuery } from '@tanstack/react-query';
import { tokenUsageService } from '../services/tokenUsage.service';
import { useTranslation } from 'react-i18next';

const number = (v: number) => new Intl.NumberFormat().format(v);
const currency = (v: number) => new Intl.NumberFormat(undefined, { style: 'currency', currency: 'USD' }).format(v);

export const TokenUsagePage: React.FC = () => {
  const { t } = useTranslation();
  const [range, setRange] = useState<{ start?: string; end?: string }>({});

  const { data: dashboard } = useQuery({
    queryKey: ['token-usage', 'dashboard', range],
    queryFn: () => tokenUsageService.getMyDashboard({ startDate: range.start, endDate: range.end }),
  });

  const { data: records = [] } = useQuery({
    queryKey: ['token-usage', 'records', range],
    queryFn: () => tokenUsageService.getMyRecords({ page: 1, pageSize: 50, startDate: range.start, endDate: range.end }),
  });

  const stats = dashboard?.statistics;

  return (
    <div className="max-w-7xl mx-auto px-4 sm:px-6 lg:px-8 py-8">
      <div className="flex items-center justify-between mb-6">
        <div>
          <h1 className="text-2xl font-semibold text-gray-900">{t('tokenUsage.title')}</h1>
          <p className="text-sm text-gray-600 mt-1">{t('tokenUsage.subtitle')}</p>
        </div>
        <div className="flex items-center space-x-2">
          <input type="date" className="input" value={range.start || ''} onChange={e => setRange(r => ({ ...r, start: e.target.value }))} />
          <span className="text-gray-400">—</span>
          <input type="date" className="input" value={range.end || ''} onChange={e => setRange(r => ({ ...r, end: e.target.value }))} />
        </div>
      </div>

      {/* 概览统计 */}
      <div className="grid grid-cols-1 md:grid-cols-4 gap-4">
        <StatCard title={t('tokenUsage.stats.totalRequests')} value={stats ? number(stats.totalRequests) : '—'} />
        <StatCard title={t('tokenUsage.stats.totalTokens')} value={stats ? number(stats.totalTokens) : '—'} />
        <StatCard title={t('tokenUsage.stats.totalCost')} value={stats ? currency(stats.totalCost) : '—'} />
        <StatCard title={t('tokenUsage.stats.avgCostPerRequest')} value={stats ? currency(stats.avgCostPerRequest) : '—'} />
      </div>

      {/* Provider / Operation / 趋势 */}
      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6 mt-6">
        <div className="bg-white rounded-lg border p-4">
          <h3 className="font-medium text-gray-900 mb-3">{t('tokenUsage.charts.byProvider')}</h3>
          <div className="space-y-2 max-h-72 overflow-auto">
            {dashboard?.providerStats?.map((p, idx) => (
              <div key={idx} className="text-sm flex items-center justify-between">
                <div className="truncate pr-2 text-gray-800">{p.provider} / {p.model}</div>
                <div className="text-gray-600">{number(p.tokens)} · {currency(p.cost)}</div>
              </div>
            )) || <div className="text-sm text-gray-500">{t('tokenUsage.noData')}</div>}
          </div>
        </div>

        <div className="bg-white rounded-lg border p-4">
          <h3 className="font-medium text-gray-900 mb-3">{t('tokenUsage.charts.byOperation')}</h3>
          <div className="space-y-2">
            {dashboard?.operationStats?.map((o, idx) => (
              <div key={idx} className="text-sm flex items-center justify-between">
                <div className="truncate pr-2 text-gray-800">{o.operationType}</div>
                <div className="text-gray-600">{number(o.tokens)} · {currency(o.cost)}</div>
              </div>
            )) || <div className="text-sm text-gray-500">{t('tokenUsage.noData')}</div>}
          </div>
        </div>

        <div className="bg-white rounded-lg border p-4">
          <h3 className="font-medium text-gray-900 mb-3">{t('tokenUsage.charts.dailyTrend')}</h3>
          <div className="space-y-2 max-h-72 overflow-auto">
            {dashboard?.dailyTrends?.map((d, idx) => (
              <div key={idx} className="text-sm flex items-center justify-between">
                <div className="text-gray-800">{d.date}</div>
                <div className="text-gray-600">{number(d.tokens)} · {currency(d.cost)}</div>
              </div>
            )) || <div className="text-sm text-gray-500">{t('tokenUsage.noData')}</div>}
          </div>
        </div>
      </div>

      {/* 明细列表 */}
      <div className="mt-8">
        <div className="bg-white rounded-lg border overflow-hidden">
          <div className="border-b px-4 py-3 font-medium text-gray-900">{t('tokenUsage.table.recentRecords')}</div>
          <div className="overflow-x-auto">
            <table className="min-w-full">
              <thead className="bg-gray-50">
                <tr>
                  <Th>{t('tokenUsage.table.headers.time')}</Th>
                  <Th>{t('tokenUsage.table.headers.provider')}</Th>
                  <Th>{t('tokenUsage.table.headers.model')}</Th>
                  <Th>{t('tokenUsage.table.headers.operation')}</Th>
                  <Th className="text-right">{t('tokenUsage.table.headers.tokens')}</Th>
                  <Th className="text-right">{t('tokenUsage.table.headers.cost')}</Th>
                </tr>
              </thead>
              <tbody className="divide-y">
                {records.length === 0 ? (
                  <tr><td className="px-4 py-6 text-center text-gray-500" colSpan={6}>{t('tokenUsage.empty')}</td></tr>
                ) : records.map(r => (
                  <tr key={r.id} className="hover:bg-gray-50">
                    <Td>{new Date(r.createdAt).toLocaleString()}</Td>
                    <Td>{r.provider}</Td>
                    <Td>{r.model}</Td>
                    <Td>{r.operationType}</Td>
                    <Td className="text-right">{number(r.totalTokens)}</Td>
                    <Td className="text-right">{currency(r.totalCost)}</Td>
                  </tr>
                ))}
              </tbody>
            </table>
          </div>
        </div>
      </div>
    </div>
  );
};

const StatCard: React.FC<{ title: string; value: string | number }> = ({ title, value }) => (
  <div className="bg-white rounded-lg border p-4">
    <div className="text-sm text-gray-500">{title}</div>
    <div className="text-2xl font-semibold text-gray-900 mt-1">{value}</div>
  </div>
);

const Th: React.FC<React.PropsWithChildren<{ className?: string }>> = ({ children, className }) => (
  <th className={`px-4 py-2 text-left text-sm font-medium text-gray-600 ${className || ''}`}>{children}</th>
);

const Td: React.FC<React.PropsWithChildren<{ className?: string }>> = ({ children, className }) => (
  <td className={`px-4 py-2 text-sm text-gray-800 ${className || ''}`}>{children}</td>
);

export default TokenUsagePage;
