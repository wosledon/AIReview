interface ProgressBarProps {
  value: number;
  max?: number;
  size?: 'sm' | 'md' | 'lg';
  color?: 'primary' | 'green' | 'yellow' | 'orange' | 'red' | 'gray';
  showValue?: boolean;
  label?: string;
  className?: string;
}

export const ProgressBar = ({ 
  value, 
  max = 100, 
  size = 'md', 
  color = 'primary', 
  showValue = true, 
  label,
  className = '' 
}: ProgressBarProps) => {
  const percentage = Math.min(Math.max((value / max) * 100, 0), 100);
  
  const sizeClasses = {
    sm: 'h-1.5',
    md: 'h-2',
    lg: 'h-3'
  };

  const colorClasses = {
    primary: 'bg-primary-500',
    green: 'bg-green-500',
    yellow: 'bg-yellow-500',
    orange: 'bg-orange-500',
    red: 'bg-red-500',
    gray: 'bg-gray-500'
  };

  const getColorForValue = (val: number): keyof typeof colorClasses => {
    if (val >= 80) return 'red';
    if (val >= 60) return 'orange';
    if (val >= 40) return 'yellow';
    if (val >= 20) return 'green';
    return 'green';
  };

  const finalColor = color === 'primary' ? color : getColorForValue(percentage);

  return (
    <div className={`w-full ${className}`}>
      {(label || showValue) && (
        <div className="flex justify-between items-center mb-1">
          {label && <span className="text-sm font-medium text-gray-700 dark:text-gray-300">{label}</span>}
          {showValue && (
            <span className="text-sm text-gray-500 dark:text-gray-400">
              {Math.round(value)}{max === 100 ? '%' : `/${max}`}
            </span>
          )}
        </div>
      )}
      <div className={`w-full bg-gray-200 dark:bg-gray-700 rounded-full ${sizeClasses[size]}`}>
        <div 
          className={`${sizeClasses[size]} ${colorClasses[finalColor]} rounded-full transition-all duration-300 ease-in-out`}
          style={{ width: `${percentage}%` }}
        />
      </div>
    </div>
  );
};

// 风险等级圆形指示器
interface RiskLevelIndicatorProps {
  level: 'Low' | 'Medium' | 'High' | 'Critical' | string;
  score?: number;
  size?: 'sm' | 'md' | 'lg';
}

export const RiskLevelIndicator = ({ level, score, size = 'md' }: RiskLevelIndicatorProps) => {
  const getColors = (riskLevel: string) => {
    switch (riskLevel) {
      case 'Low':
        return { bg: 'bg-green-100 dark:bg-green-900/20', text: 'text-green-600 dark:text-green-400', ring: 'ring-green-500' };
      case 'Medium':
        return { bg: 'bg-yellow-100 dark:bg-yellow-900/20', text: 'text-yellow-600 dark:text-yellow-400', ring: 'ring-yellow-500' };
      case 'High':
        return { bg: 'bg-orange-100 dark:bg-orange-900/20', text: 'text-orange-600 dark:text-orange-400', ring: 'ring-orange-500' };
      case 'Critical':
        return { bg: 'bg-red-100 dark:bg-red-900/20', text: 'text-red-600 dark:text-red-400', ring: 'ring-red-500' };
      default:
        return { bg: 'bg-gray-100 dark:bg-gray-900/20', text: 'text-gray-600 dark:text-gray-400', ring: 'ring-gray-500' };
    }
  };

  const sizeClasses = {
    sm: { container: 'w-16 h-16', text: 'text-xs', score: 'text-lg' },
    md: { container: 'w-20 h-20', text: 'text-sm', score: 'text-xl' },
    lg: { container: 'w-24 h-24', text: 'text-base', score: 'text-2xl' }
  };

  const colors = getColors(level);
  const sizes = sizeClasses[size];

  return (
    <div className={`${sizes.container} rounded-full ${colors.bg} ${colors.text} ring-2 ${colors.ring} flex flex-col items-center justify-center`}>
      {score !== undefined && (
        <div className={`font-bold ${sizes.score}`}>{Math.round(score)}</div>
      )}
      <div className={`font-medium ${sizes.text} text-center leading-tight`}>
        {level}
      </div>
    </div>
  );
};