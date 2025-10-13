// 测试分析功能组件
import React from 'react';
import { AnalysisCard, MetricGrid, Tag, ListItem } from '../components/common/AnalysisCard';
import { RiskDonutChart, ImpactBarChart, FileHeatmap, TrendLine } from '../components/common/Charts';
import { LoadingSpinner, LoadingCard, EmptyState, ErrorState } from '../components/common/LoadingStates';
import { ProgressBar, RiskLevelIndicator } from '../components/common/ProgressBar';
import { AnalysisDashboard } from '../components/common/AnalysisDashboard';
import { DocumentTextIcon } from '@heroicons/react/24/outline';

// 模拟数据
const mockAnalysisData = {
  riskAssessment: {
    id: 1,
    reviewRequestId: 1,
    basicRiskScore: 75,
    aiRiskScore: 82,
    combinedRiskScore: 78,
    riskLevel: 'High' as const,
    complexityRisk: 85,
    securityRisk: 60,
    performanceRisk: 70,
    maintainabilityRisk: 75,
    mitigationStrategies: [
      '增加单元测试覆盖率',
      '进行代码审查',
      '添加错误处理机制',
      '优化数据库查询'
    ],
    reasoning: '基于代码复杂度分析和潜在安全漏洞检测，该变更具有较高风险。主要风险来源于新增的数据库操作和复杂的业务逻辑。',
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
    overallSummary: '本次PR主要实现了用户认证系统的重构，包括JWT令牌管理、密码加密优化和会话管理改进。变更涉及多个核心模块，需要仔细审查安全相关的实现。',
    keyChanges: [
      '重构JWT认证中间件',
      '实现密码哈希算法升级',
      '添加会话管理功能',
      '更新用户权限验证逻辑'
    ],
    impactAnalysis: '此次变更对用户认证流程产生重大影响，可能影响现有登录用户的会话状态。建议在部署前进行充分的兼容性测试。',
    riskAssessment: '主要风险集中在安全性和向后兼容性方面。新的认证机制需要确保与现有系统的平滑过渡。',
    breakingChanges: [
      '修改了JWT payload结构',
      '更新了密码验证接口'
    ],
    testingRecommendations: '建议进行全面的集成测试，特别关注用户登录、权限验证和会话管理功能。同时需要进行安全性测试以验证新的认证机制。',
    deploymentNotes: '部署时需要清理现有用户会话，建议在低峰期进行部署并提前通知用户。',
    affectedComponents: [
      '认证中间件',
      '用户服务',
      '权限管理',
      '会话存储',
      'API路由'
    ],
    performanceImpact: '新的密码哈希算法可能略微增加CPU使用率，但在可接受范围内。JWT处理性能有所提升。',
    securityConsiderations: '新的认证机制提高了整体安全性，但需要确保密钥管理和令牌刷新机制的正确实现。',
    confidenceLevel: 0.88,
    aiModelVersion: 'GPT-4-Turbo',
    createdAt: new Date().toISOString(),
    updatedAt: new Date().toISOString()
  }
};

const AnalysisTestPage: React.FC = () => {
  const [currentTest, setCurrentTest] = React.useState<string>('dashboard');

  const testComponents = [
    { id: 'dashboard', name: '分析仪表板', component: <AnalysisDashboard analysisData={mockAnalysisData} /> },
    { id: 'charts', name: '图表组件', component: <ChartsTest /> },
    { id: 'loading', name: '加载状态', component: <LoadingStatesTest /> },
    { id: 'cards', name: '卡片组件', component: <CardsTest /> }
  ];

  return (
    <div className="min-h-screen bg-gray-50 dark:bg-gray-900 p-6">
      <div className="max-w-7xl mx-auto">
        <div className="mb-6">
          <h1 className="text-3xl font-bold text-gray-900 dark:text-gray-100 mb-4">
            AI 分析功能组件测试
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
  const riskData = [
    { level: 'high' as const, count: 3, percentage: 30 },
    { level: 'medium' as const, count: 5, percentage: 50 },
    { level: 'low' as const, count: 2, percentage: 20 }
  ];

  const impactData = [
    { category: '核心功能', impact: 85, files: 12 },
    { category: '测试覆盖', impact: 70, files: 8 },
    { category: '配置文件', impact: 30, files: 3 }
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
      <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">图表组件测试</h2>
      
      <div className="grid grid-cols-1 lg:grid-cols-2 gap-6">
        <AnalysisCard title="风险分布图">
          <RiskDonutChart data={riskData} />
        </AnalysisCard>

        <AnalysisCard title="影响分析图">
          <ImpactBarChart data={impactData} />
        </AnalysisCard>

        <AnalysisCard title="文件热力图">
          <FileHeatmap data={fileHeatmapData} />
        </AnalysisCard>

        <AnalysisCard title="风险趋势">
          <TrendLine data={trendData} title="风险变化趋势" />
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
  const metrics = [
    { label: '风险评分', value: 78, color: 'orange' as const },
    { label: '改进建议', value: 12, color: 'blue' as const },
    { label: 'AI 置信度', value: '88%', color: 'green' as const }
  ];

  return (
    <div className="space-y-6">
      <h2 className="text-xl font-semibold text-gray-900 dark:text-gray-100">卡片组件测试</h2>
      
      <div className="space-y-6">
        <AnalysisCard title="指标网格">
          <MetricGrid metrics={metrics} />
        </AnalysisCard>

        <AnalysisCard title="标签示例" collapsible defaultExpanded={true}>
          <div className="flex flex-wrap gap-2">
            <Tag variant="primary">主要</Tag>
            <Tag variant="secondary">次要</Tag>
            <Tag variant="success">成功</Tag>
            <Tag variant="warning">警告</Tag>
            <Tag variant="danger">危险</Tag>
            <Tag variant="info">信息</Tag>
          </div>
        </AnalysisCard>

        <AnalysisCard title="列表项示例" collapsible defaultExpanded={false}>
          <ul className="space-y-2">
            <ListItem>这是一个基础列表项</ListItem>
            <ListItem>这是另一个列表项</ListItem>
            <ListItem>支持多行文本的列表项，可以包含更多详细信息</ListItem>
          </ul>
        </AnalysisCard>

        <AnalysisCard title="进度条示例">
          <div className="space-y-4">
            <div>
              <div className="mb-2">基础进度条 (75%)</div>
              <ProgressBar value={75} max={100} />
            </div>
            <div>
              <div className="mb-2">风险等级指示器</div>
              <RiskLevelIndicator level="High" />
            </div>
          </div>
        </AnalysisCard>
      </div>
    </div>
  );
};

export default AnalysisTestPage;