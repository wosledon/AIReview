import { AnalysisCard, MetricGrid } from './AnalysisCard';
import { RiskDonutChart, TrendLine } from './Charts';
import { RiskLevelIndicator } from './ProgressBar';
import type { AnalysisData, RiskAssessment, ImprovementSuggestion } from '../../types/analysis';
import { getRiskLevel } from '../../types/analysis';

interface AnalysisDashboardProps {
  analysisData: AnalysisData | null;
  className?: string;
}

export const AnalysisDashboard = ({ analysisData, className = '' }: AnalysisDashboardProps) => {
  if (!analysisData) {
    return null;
  }

  // 计算综合指标
  const calculateOverallMetrics = () => {
    const riskAssessment = analysisData.riskAssessment;
    const suggestions = analysisData.improvementSuggestions || [];

    const overallRiskScore = riskAssessment?.overallRiskScore || 0;
    const totalSuggestions = suggestions.length;
    const highPrioritySuggestions = suggestions.filter(s => s.priority === 'High' || s.priority === 'Critical').length;
    const confidenceLevel = (riskAssessment?.confidenceScore || 0) * 100;

    return {
      overallRiskScore,
      totalSuggestions,
      highPrioritySuggestions,
      confidenceLevel: Math.round(confidenceLevel),
    };
  };

  const metrics = calculateOverallMetrics();

  // 风险分布数据
  const riskDistributionData = [
    { level: 'high' as const, count: 1, percentage: 25 },
    { level: 'medium' as const, count: 2, percentage: 50 },
    { level: 'low' as const, count: 1, percentage: 25 }
  ];

  // 趋势数据（模拟）
  const riskTrendData = [
    { period: '上周', value: 75 },
    { period: '本周', value: metrics.overallRiskScore },
    { period: '预测', value: Math.max(0, metrics.overallRiskScore - 10) }
  ];

  const dashboardMetrics = [
    {
      label: '综合风险评分',
      value: metrics.overallRiskScore.toFixed(2),
      color: (metrics.overallRiskScore > 80 ? 'red' : metrics.overallRiskScore > 60 ? 'orange' : 'green') as 'red' | 'orange' | 'green'
    },
    {
      label: '改进建议',
      value: metrics.totalSuggestions,
      subLabel: `${metrics.highPrioritySuggestions} 个高优先级`,
      color: 'blue' as const
    },
    {
      label: 'AI 置信度',
      value: `${metrics.confidenceLevel}%`,
      color: (metrics.confidenceLevel > 80 ? 'green' : metrics.confidenceLevel > 60 ? 'yellow' : 'orange') as 'green' | 'yellow' | 'orange'
    },
    {
      label: '分析完成度',
      value: `${Math.round(((analysisData.riskAssessment ? 1 : 0) + 
        (analysisData.improvementSuggestions?.length ? 1 : 0) + 
        (analysisData.pullRequestSummary ? 1 : 0)) / 3 * 100)}%`,
      color: 'gray' as const
    }
  ];

  return (
    <div className={`space-y-6 ${className}`}>
      {/* 综合指标概览 */}
      <AnalysisCard title="分析概览">
        <MetricGrid metrics={dashboardMetrics} columns={4} />
      </AnalysisCard>

      <div className="grid grid-cols-1 lg:grid-cols-3 gap-6">
        {/* 风险分布 */}
        <AnalysisCard title="风险分布">
          <RiskDonutChart data={riskDistributionData} />
        </AnalysisCard>

        {/* 风险趋势 */}
        <AnalysisCard title="风险趋势">
          <TrendLine data={riskTrendData} />
        </AnalysisCard>

        {/* 风险等级指示器 */}
        <AnalysisCard title="当前风险等级">
          <div className="flex flex-col items-center justify-center h-full">
            <div className="mb-4">
              <RiskLevelIndicator 
                level={analysisData.riskAssessment ? getRiskLevel(analysisData.riskAssessment.overallRiskScore) : 'Medium'} 
              />
            </div>
            <div className="text-center">
              <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">
                {analysisData.riskAssessment ? getRiskLevel(analysisData.riskAssessment.overallRiskScore) : 'Medium'}
              </div>
              <div className="text-sm text-gray-500 dark:text-gray-400">
                风险等级
              </div>
            </div>
          </div>
        </AnalysisCard>
      </div>

      {/* 关键洞察 */}
      {analysisData.riskAssessment && (
        <AnalysisCard title="关键洞察" collapsible defaultExpanded={true}>
          <div className="grid grid-cols-1 md:grid-cols-2 gap-4">
            <div className="bg-blue-50 dark:bg-blue-900/10 p-4 rounded-lg">
              <h4 className="font-medium text-blue-900 dark:text-blue-100 mb-2">
                最高风险维度
              </h4>
              <p className="text-sm text-blue-700 dark:text-blue-300">
                {getHighestRiskDimension(analysisData.riskAssessment)}
              </p>
            </div>
            
            {analysisData.improvementSuggestions && analysisData.improvementSuggestions.length > 0 && (
              <div className="bg-green-50 dark:bg-green-900/10 p-4 rounded-lg">
                <h4 className="font-medium text-green-900 dark:text-green-100 mb-2">
                  优先建议
                </h4>
                <p className="text-sm text-green-700 dark:text-green-300">
                  {getTopSuggestion(analysisData.improvementSuggestions)}
                </p>
              </div>
            )}
          </div>
        </AnalysisCard>
      )}

      {/* 快速操作 */}
      <AnalysisCard title="快速操作">
        <div className="flex flex-wrap gap-3">
          <button className="px-4 py-2 bg-primary-100 text-primary-700 dark:bg-primary-900/20 dark:text-primary-400 rounded-lg text-sm font-medium hover:bg-primary-200 dark:hover:bg-primary-900/30 transition-colors">
            导出分析报告
          </button>
          <button className="px-4 py-2 bg-gray-100 text-gray-700 dark:bg-gray-800 dark:text-gray-300 rounded-lg text-sm font-medium hover:bg-gray-200 dark:hover:bg-gray-700 transition-colors">
            分享分析结果
          </button>
          <button className="px-4 py-2 bg-orange-100 text-orange-700 dark:bg-orange-900/20 dark:text-orange-400 rounded-lg text-sm font-medium hover:bg-orange-200 dark:hover:bg-orange-900/30 transition-colors">
            重新分析
          </button>
        </div>
      </AnalysisCard>
    </div>
  );
};

// 辅助函数：获取最高风险维度
const getHighestRiskDimension = (riskAssessment: RiskAssessment) => {
  const dimensions = [
    { name: '复杂性', value: riskAssessment.complexityRisk },
    { name: '安全性', value: riskAssessment.securityRisk },
    { name: '性能', value: riskAssessment.performanceRisk },
    { name: '可维护性', value: riskAssessment.maintainabilityRisk }
  ];
  
  const highest = dimensions.reduce((max, curr) => curr.value > max.value ? curr : max);
  return `${highest.name}风险最高（${highest.value}%）`;
};

// 辅助函数：获取最优先的建议
const getTopSuggestion = (suggestions: ImprovementSuggestion[]) => {
  const priorityOrder: Record<string, number> = { 'Critical': 4, 'High': 3, 'Medium': 2, 'Low': 1 };
  const topSuggestion = suggestions.sort((a, b) => 
    priorityOrder[b.priority] - priorityOrder[a.priority]
  )[0];
  
  return topSuggestion?.title || '暂无建议';
};