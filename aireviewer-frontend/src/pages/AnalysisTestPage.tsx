// 测试分析功能组件
import React from 'react';
import { useTranslation } from 'react-i18next';
import { AnalysisCard, MetricGrid, Tag, ListItem } from '../components/common/AnalysisCard';
import { RiskDonutChart, ImpactBarChart, FileHeatmap, TrendLine } from '../components/common/Charts';
import { LoadingSpinner, LoadingCard, EmptyState, ErrorState } from '../components/common/LoadingStates';
import { ProgressBar, RiskLevelIndicator } from '../components/common/ProgressBar';
import { AnalysisDashboard } from '../components/common/AnalysisDashboard';
import type { AnalysisData } from '../types/analysis';
import { DocumentTextIcon } from '@heroicons/react/24/outline';

// 模拟数据（符合 AnalysisData 接口）
const mockAnalysisData: AnalysisData = {
  riskAssessment: {
    id: 1,
    reviewRequestId: 1,
    overallRiskScore: 78,
    complexityRisk: 85,
    securityRisk: 60,
    performanceRisk: 70,
    maintainabilityRisk: 75,
    testCoverageRisk: 40,
    changedFilesCount: 18,
    changedLinesCount: 520,
    riskDescription: '基于代码复杂度分析和潜在安全漏洞检测，该变更具有较高风险。',
    mitigationSuggestions: '增加单元测试覆盖率；进行代码审查；添加错误处理机制；优化数据库查询',
    aiModelVersion: 'GPT-4.1',
    confidenceScore: 0.88,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  },
  improvementSuggestions: [
    {
      id: 1,
      reviewRequestId: 1,
      type: 'Security' as const,
      priority: 'High' as const,
      title: '修复SQL注入漏洞',
      description: '使用参数化查询替代字符串拼接，防止SQL注入攻击',
      filePath: 'src/services/userService.ts',
      startLine: 45,
      endLine: 52,
      originalCode: 'const query = `SELECT * FROM users WHERE id = ${userId}`;',
      suggestedCode: 'const query = `SELECT * FROM users WHERE id = ?`;\nconst result = await db.query(query, [userId]);',
      reasoning: '当前代码直接拼接用户输入到SQL查询中，存在SQL注入风险',
      expectedBenefits: '提高应用安全性，防止恶意SQL注入攻击',
      implementationComplexity: 3,
      confidenceScore: 0.95,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    },
    {
      id: 2,
      reviewRequestId: 1,
      type: 'Performance' as const,
      priority: 'Medium' as const,
      title: '优化数据库查询',
      description: '添加适当的索引以提高查询性能',
      filePath: 'src/models/user.ts',
      startLine: undefined,
      endLine: undefined,
      originalCode: undefined,
      suggestedCode: undefined,
      reasoning: '当前查询缺乏索引支持，在大数据量情况下可能导致性能问题',
      expectedBenefits: '显著提高查询速度，减少数据库负载',
      implementationComplexity: 5,
      confidenceScore: 0.85,
      createdAt: new Date().toISOString(),
      updatedAt: new Date().toISOString()
    }
  ],
  pullRequestSummary: {
    id: 1,
    reviewRequestId: 1,
    changeType: 'Security',
    summary: '重构用户认证系统：JWT 令牌管理、密码加密优化与会话管理改进，涉及多个核心模块。',
    detailedDescription: '此次变更对用户认证流程产生重大影响，需要关注安全性与兼容性。',
    keyChanges: [
      '重构JWT认证中间件',
      '实现密码哈希算法升级',
      '添加会话管理功能',
      '更新用户权限验证逻辑'
    ].join('\n'),
    impactAnalysis: '对登录流程、权限验证和会话管理有重要影响，建议进行充分的集成与回归测试。',
    businessImpact: 'High',
    technicalImpact: 'Medium',
    breakingChangeRisk: 'Medium',
    testingRecommendations: '重点测试登录、权限、会话管理与安全边界；补充安全性测试。',
    deploymentConsiderations: '低峰期部署并清理历史会话，提前通知用户。',
    dependencyChanges: '升级加密与认证相关依赖。',
    performanceImpact: '密码哈希可能略微增加CPU使用率；JWT处理性能有所提升。',
    securityImpact: '整体安全性提高，需要确保密钥管理与令牌刷新机制正确实现。',
    backwardCompatibility: '需要确保对旧会话的平滑迁移。',
    documentationRequirements: '更新认证流程文档与API说明。',
    changeStatistics: {
      addedLines: 320,
      deletedLines: 140,
      modifiedFiles: 18,
      addedFiles: 3,
      deletedFiles: 1
    },
    aiModelVersion: 'GPT-4-Turbo',
    confidenceScore: 0.88,
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  }
};

const AnalysisTestPage: React.FC = () => {
  const { t } = useTranslation();
  const [currentTest, setCurrentTest] = React.useState<string>('dashboard');

  const testComponents = [
    { id: 'dashboard', name: t('analysis.test.dashboard'), component: <AnalysisDashboard analysisData={mockAnalysisData} /> },
    { id: 'charts', name: t('analysis.test.charts'), component: <ChartsTest /> },
    { id: 'loading', name: t('analysis.test.loading'), component: <LoadingStatesTest /> },
    { id: 'cards', name: t('analysis.test.cards'), component: <CardsTest /> }
  ];

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 p-6">
      <div className="max-w-7xl mx-auto">
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-4">
            {t('analysis.test.title')}
          </h1>
          
          {/* 测试导航 */}
          <div className="flex space-x-4 mb-6">
            {testComponents.map((test) => (
              <button
                key={test.id}
                onClick={() => setCurrentTest(test.id)}
                className={`px-4 py-2 rounded-lg font-medium transition-colors ${
                  currentTest === test.id
                    ? 'bg-primary-600 text-white'
                    : 'bg-white dark:bg-gray-800 text-gray-700 dark:text-gray-300 hover:bg-gray-100 dark:hover:bg-gray-700'
                }`}
              >
                {test.name}
              </button>
            ))}
          </div>
        </div>

        {/* 当前测试组件 */}
        <div className="bg-white dark:bg-gray-800 rounded-lg p-6">
          {testComponents.find(t => t.id === currentTest)?.component}
        </div>
      </div>
    </div>
  );
};

// 图表测试组件
const ChartsTest: React.FC = () => {
  const { t } = useTranslation();
  const riskData = [
    { level: 'high' as const, count: 3, percentage: 30 },
    { level: 'medium' as const, count: 5, percentage: 50 },
    { level: 'low' as const, count: 2, percentage: 20 }
  ];

  const impactData = [
    { category: t('analysis.test.core_function'), impact: 85, files: 12 },
    { category: t('analysis.test.test_coverage'), impact: 70, files: 8 },
    { category: t('analysis.test.config_files'), impact: 30, files: 3 }
  ];

  const fileHeatmapData = [
    { file: 'src/auth/auth.service.ts', changes: 45, risk: 'high' as const },
    { file: 'src/user/user.controller.ts', changes: 32, risk: 'medium' as const },
    { file: 'src/utils/crypto.ts', changes: 28, risk: 'high' as const },
    { file: 'src/config/database.ts', changes: 15, risk: 'low' as const }
  ];

  const trendData = [
    { period: '上周', value: 75 },
    { period: '本周', value: 68 },
    { period: '预测', value: 60 }
  ];

  return (
    <div className="space-y-6">
      <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">{t('analysis.test.charts_title')}</h2>
      
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <AnalysisCard title={t('analysis.test.risk_distribution')}>
          <RiskDonutChart data={riskData} />
        </AnalysisCard>

        <AnalysisCard title={t('analysis.test.impact_analysis')}>
          <ImpactBarChart data={impactData} />
        </AnalysisCard>

        <AnalysisCard title={t('analysis.test.file_heatmap')}>
          <FileHeatmap data={fileHeatmapData} />
        </AnalysisCard>

        <AnalysisCard title={t('analysis.test.risk_trend')}>
          <TrendLine data={trendData} title={t('analysis.test.risk_trend')} />
        </AnalysisCard>
      </div>
    </div>
  );
};

// 加载状态测试组件
const LoadingStatesTest: React.FC = () => {
  return (
    <div className="space-y-6">
      <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">加载状态组件测试</h2>
      
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <div>
          <h3 className="text-lg font-medium mb-4">加载微调器</h3>
          <LoadingSpinner size="lg" />
        </div>

        <div>
          <h3 className="text-lg font-medium mb-4">加载卡片</h3>
          <LoadingCard title="正在加载分析数据" description="请稍候，AI 正在分析您的代码..." />
        </div>

        <div>
          <h3 className="text-lg font-medium mb-4">空状态</h3>
          <EmptyState
            icon={DocumentTextIcon}
            title="暂无数据"
            description="还没有分析数据，点击下方按钮开始分析"
            action={
              <button className="px-4 py-2 bg-primary-600 text-white rounded-lg">
                开始分析
              </button>
            }
          />
        </div>

        <div>
          <h3 className="text-lg font-medium mb-4">错误状态</h3>
          <ErrorState
            title="分析失败"
            description="无法完成代码分析，请稍后重试"
            onRetry={() => console.log('重试')}
          />
        </div>
      </div>
    </div>
  );
};

// 卡片组件测试
const CardsTest: React.FC = () => {
  const { t } = useTranslation();
  const metrics = [
    { label: '风险评分', value: 78, color: 'orange' as const },
    { label: '改进建议', value: 12, color: 'blue' as const },
    { label: 'AI 置信度', value: '88%', color: 'green' as const }
  ];

  return (
    <div className="space-y-6">
      <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">{t('analysis.test.cards_title')}</h2>
      
      <div className="space-y-6">
        <AnalysisCard title={t('analysis.test.metrics_grid')}>
          <MetricGrid metrics={metrics} />
        </AnalysisCard>

        <AnalysisCard title={t('analysis.test.tags_example')} collapsible defaultExpanded={true}>
          <div className="flex flex-wrap gap-2">
            <Tag variant="primary">主要</Tag>
            <Tag variant="secondary">次要</Tag>
            <Tag variant="success">成功</Tag>
            <Tag variant="warning">警告</Tag>
            <Tag variant="danger">危险</Tag>
            <Tag variant="info">信息</Tag>
          </div>
        </AnalysisCard>

        <AnalysisCard title={t('analysis.test.list_example')} collapsible defaultExpanded={false}>
          <ul className="space-y-2">
            <ListItem>这是一个基础列表项</ListItem>
            <ListItem>这是另一个列表项</ListItem>
            <ListItem>支持多行文本的列表项，可以包含更多详细信息</ListItem>
          </ul>
        </AnalysisCard>

        <AnalysisCard title={t('analysis.test.progress_example')}>
          <div className="space-y-4">
            <div>
              <div className="mb-2">{t('analysis.test.basic_progress')} (75%)</div>
              <ProgressBar value={75} max={100} />
            </div>
            <div>
              <div className="mb-2">{t('analysis.test.risk_indicator')}</div>
              <RiskLevelIndicator level="High" />
            </div>
          </div>
        </AnalysisCard>
      </div>
    </div>
  );
};

export default AnalysisTestPage;