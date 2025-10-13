interface ChartProps {
  className?: string;
}

// 风险分布环形图
interface RiskDonutChartProps extends ChartProps {
  data: Array<{
    level: 'high' | 'medium' | 'low';
    count: number;
    percentage: number;
  }>;
}

export const RiskDonutChart = ({ data, className = '' }: RiskDonutChartProps) => {
  const total = data.reduce((sum, item) => sum + item.count, 0);
  
  const getColor = (level: string) => {
    switch (level) {
      case 'high': return { bg: 'bg-red-500', text: 'text-red-600' };
      case 'medium': return { bg: 'bg-yellow-500', text: 'text-yellow-600' };
      case 'low': return { bg: 'bg-green-500', text: 'text-green-600' };
      default: return { bg: 'bg-gray-500', text: 'text-gray-600' };
    }
  };

  let cumulativePercentage = 0;

  return (
    <div className={`flex items-center justify-center space-x-8 ${className}`}>
      {/* 环形图 */}
      <div className="relative w-32 h-32">
        <svg className="w-full h-full transform -rotate-90" viewBox="0 0 100 100">
          <circle
            cx="50"
            cy="50"
            r="35"
            fill="none"
            stroke="currentColor"
            strokeWidth="8"
            className="text-gray-200 dark:text-gray-700"
          />
          {data.map((item, index) => {
            const { bg } = getColor(item.level);
            const strokeDasharray = `${item.percentage * 2.2} ${220 - item.percentage * 2.2}`;
            const strokeDashoffset = -(cumulativePercentage * 2.2);
            cumulativePercentage += item.percentage;
            
            return (
              <circle
                key={index}
                cx="50"
                cy="50"
                r="35"
                fill="none"
                strokeWidth="8"
                strokeDasharray={strokeDasharray}
                strokeDashoffset={strokeDashoffset}
                className={`${bg.replace('bg-', 'stroke-')} transition-all duration-500`}
                style={{ strokeLinecap: 'round' }}
              />
            );
          })}
        </svg>
        <div className="absolute inset-0 flex items-center justify-center">
          <div className="text-center">
            <div className="text-2xl font-bold text-gray-900 dark:text-gray-100">{total}</div>
            <div className="text-xs text-gray-500 dark:text-gray-400">总计</div>
          </div>
        </div>
      </div>

      {/* 图例 */}
      <div className="space-y-2">
        {data.map((item, index) => {
          const { bg, text } = getColor(item.level);
          const levelLabels = {
            high: '高风险',
            medium: '中风险',
            low: '低风险'
          };
          
          return (
            <div key={index} className="flex items-center space-x-2">
              <div className={`w-3 h-3 rounded-full ${bg}`}></div>
              <div className="text-sm">
                <span className="text-gray-700 dark:text-gray-300">
                  {levelLabels[item.level]}
                </span>
                <span className={`ml-1 font-medium ${text}`}>
                  {item.count} ({item.percentage.toFixed(1)}%)
                </span>
              </div>
            </div>
          );
        })}
      </div>
    </div>
  );
};

// 影响分析条形图
interface ImpactBarChartProps extends ChartProps {
  data: Array<{
    category: string;
    impact: number; // 0-100
    files: number;
  }>;
}

export const ImpactBarChart = ({ data, className = '' }: ImpactBarChartProps) => {
  const maxImpact = Math.max(...data.map(item => item.impact));

  const getImpactColor = (impact: number) => {
    if (impact >= 80) return 'bg-red-500';
    if (impact >= 60) return 'bg-orange-500';
    if (impact >= 40) return 'bg-yellow-500';
    return 'bg-green-500';
  };

  return (
    <div className={`space-y-4 ${className}`}>
      {data.map((item, index) => (
        <div key={index} className="space-y-2">
          <div className="flex justify-between items-center">
            <span className="text-sm font-medium text-gray-700 dark:text-gray-300">
              {item.category}
            </span>
            <span className="text-sm text-gray-500 dark:text-gray-400">
              {item.files} 个文件
            </span>
          </div>
          <div className="relative">
            <div className="w-full bg-gray-200 dark:bg-gray-700 rounded-full h-2">
              <div
                className={`h-2 rounded-full transition-all duration-500 ${getImpactColor(item.impact)}`}
                style={{ width: `${(item.impact / maxImpact) * 100}%` }}
              />
            </div>
            <span className="absolute right-0 -top-6 text-xs text-gray-500 dark:text-gray-400">
              {item.impact}%
            </span>
          </div>
        </div>
      ))}
    </div>
  );
};

// 趋势线图（简化版）
interface TrendLineProps extends ChartProps {
  data: Array<{
    period: string;
    value: number;
  }>;
  title?: string;
}

export const TrendLine = ({ data, title, className = '' }: TrendLineProps) => {
  const maxValue = Math.max(...data.map(item => item.value));
  const minValue = Math.min(...data.map(item => item.value));
  const range = maxValue - minValue || 1;

  const points = data.map((item, index) => {
    const x = (index / (data.length - 1)) * 100;
    const y = 100 - ((item.value - minValue) / range) * 100;
    return `${x},${y}`;
  }).join(' ');

  return (
    <div className={`${className}`}>
      {title && (
        <h5 className="text-sm font-medium text-gray-700 dark:text-gray-300 mb-3">
          {title}
        </h5>
      )}
      <div className="relative h-24 bg-gray-50 dark:bg-gray-800 rounded-lg p-4">
        <svg className="w-full h-full" viewBox="0 0 100 100" preserveAspectRatio="none">
          <polyline
            fill="none"
            stroke="currentColor"
            strokeWidth="2"
            points={points}
            className="text-primary-500"
          />
          {data.map((item, index) => {
            const x = (index / (data.length - 1)) * 100;
            const y = 100 - ((item.value - minValue) / range) * 100;
            return (
              <circle
                key={index}
                cx={x}
                cy={y}
                r="2"
                fill="currentColor"
                className="text-primary-500"
              />
            );
          })}
        </svg>
        
        {/* X轴标签 */}
        <div className="absolute bottom-0 left-0 right-0 flex justify-between text-xs text-gray-500 dark:text-gray-400 px-2">
          {data.map((item, index) => (
            <span key={index}>{item.period}</span>
          ))}
        </div>
      </div>
    </div>
  );
};

// 热力图（用于显示文件修改热点）
interface HeatmapProps extends ChartProps {
  data: Array<{
    file: string;
    changes: number;
    risk: 'high' | 'medium' | 'low';
  }>;
  maxItems?: number;
}

export const FileHeatmap = ({ data, maxItems = 10, className = '' }: HeatmapProps) => {
  const sortedData = data
    .sort((a, b) => b.changes - a.changes)
    .slice(0, maxItems);
  
  const maxChanges = Math.max(...sortedData.map(item => item.changes));

  const getRiskColor = (risk: string) => {
    switch (risk) {
      case 'high': return 'border-l-red-500 bg-red-50 dark:bg-red-900/10';
      case 'medium': return 'border-l-yellow-500 bg-yellow-50 dark:bg-yellow-900/10';
      case 'low': return 'border-l-green-500 bg-green-50 dark:bg-green-900/10';
      default: return 'border-l-gray-500 bg-gray-50 dark:bg-gray-900/10';
    }
  };

  return (
    <div className={`space-y-2 ${className}`}>
      {sortedData.map((item, index) => (
        <div
          key={index}
          className={`p-3 rounded-lg border-l-4 ${getRiskColor(item.risk)} transition-all duration-200 hover:shadow-sm`}
        >
          <div className="flex justify-between items-center">
            <div className="flex-1 min-w-0">
              <div className="text-sm font-medium text-gray-900 dark:text-gray-100 truncate">
                {item.file}
              </div>
              <div className="text-xs text-gray-500 dark:text-gray-400">
                {item.changes} 处变更
              </div>
            </div>
            <div className="ml-4 flex-shrink-0">
              <div className="w-16 bg-gray-200 dark:bg-gray-700 rounded-full h-1.5">
                <div
                  className="h-1.5 bg-primary-500 rounded-full transition-all duration-300"
                  style={{ width: `${(item.changes / maxChanges) * 100}%` }}
                />
              </div>
            </div>
          </div>
        </div>
      ))}
    </div>
  );
};