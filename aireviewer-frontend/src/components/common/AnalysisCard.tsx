import { useState } from 'react';
import { ChevronDownIcon, ChevronUpIcon } from '@heroicons/react/24/outline';

interface AnalysisCardProps {
  title: string;
  children: React.ReactNode;
  collapsible?: boolean;
  defaultExpanded?: boolean;
  className?: string;
  headerActions?: React.ReactNode;
}

export const AnalysisCard = ({ 
  title, 
  children, 
  collapsible = false, 
  defaultExpanded = true,
  className = '',
  headerActions
}: AnalysisCardProps) => {
  const [isExpanded, setIsExpanded] = useState(defaultExpanded);

  return (
    <div className={`bg-white dark:bg-gray-800 rounded-lg border border-gray-200 dark:border-gray-700 ${className}`}>
      <div className="p-4 border-b border-gray-200 dark:border-gray-700">
        <div className="flex items-center justify-between">
          <div className="flex items-center space-x-2">
            <h4 className="text-lg font-medium text-gray-900 dark:text-gray-100">{title}</h4>
            {collapsible && (
              <button
                onClick={() => setIsExpanded(!isExpanded)}
                className="p-1 rounded-md hover:bg-gray-100 dark:hover:bg-gray-700 transition-colors"
              >
                {isExpanded ? (
                  <ChevronUpIcon className="h-4 w-4 text-gray-500" />
                ) : (
                  <ChevronDownIcon className="h-4 w-4 text-gray-500" />
                )}
              </button>
            )}
          </div>
          {headerActions && <div>{headerActions}</div>}
        </div>
      </div>
      {(isExpanded || !collapsible) && (
        <div className="p-4">
          {children}
        </div>
      )}
    </div>
  );
};

// 指标网格卡片
interface MetricGridProps {
  metrics: Array<{
    label: string;
    value: string | number;
    subLabel?: string;
    color?: 'green' | 'yellow' | 'orange' | 'red' | 'blue' | 'gray';
  }>;
  columns?: 2 | 3 | 4;
}

export const MetricGrid = ({ metrics, columns = 3 }: MetricGridProps) => {
  const getColorClasses = (color?: string) => {
    switch (color) {
      case 'green': return 'text-green-600 dark:text-green-400';
      case 'yellow': return 'text-yellow-600 dark:text-yellow-400';
      case 'orange': return 'text-orange-600 dark:text-orange-400';
      case 'red': return 'text-red-600 dark:text-red-400';
      case 'blue': return 'text-blue-600 dark:text-blue-400';
      default: return 'text-gray-900 dark:text-gray-100';
    }
  };

  const gridClasses = {
    2: 'grid-cols-1 md:grid-cols-2',
    3: 'grid-cols-1 md:grid-cols-3',
    4: 'grid-cols-1 md:grid-cols-2 lg:grid-cols-4'
  };

  return (
    <div className={`grid ${gridClasses[columns]} gap-6`}>
      {metrics.map((metric, index) => (
        <div key={index} className="text-center">
          <div className={`text-2xl font-bold ${getColorClasses(metric.color)}`}>
            {metric.value}
          </div>
          <div className="text-sm text-gray-500 dark:text-gray-400 mt-1">
            {metric.label}
          </div>
          {metric.subLabel && (
            <div className="text-xs text-gray-400 dark:text-gray-500 mt-0.5">
              {metric.subLabel}
            </div>
          )}
        </div>
      ))}
    </div>
  );
};

// 标签组件
interface TagProps {
  children: React.ReactNode;
  variant?: 'primary' | 'secondary' | 'success' | 'warning' | 'danger' | 'info';
  size?: 'sm' | 'md' | 'lg';
  className?: string;
}

export const Tag = ({ children, variant = 'primary', size = 'md', className = '' }: TagProps) => {
  const variantClasses = {
    primary: 'bg-primary-100 text-primary-800 dark:bg-primary-900/20 dark:text-primary-400',
    secondary: 'bg-gray-100 text-gray-800 dark:bg-gray-900/20 dark:text-gray-400',
    success: 'bg-green-100 text-green-800 dark:bg-green-900/20 dark:text-green-400',
    warning: 'bg-yellow-100 text-yellow-800 dark:bg-yellow-900/20 dark:text-yellow-400',
    danger: 'bg-red-100 text-red-800 dark:bg-red-900/20 dark:text-red-400',
    info: 'bg-blue-100 text-blue-800 dark:bg-blue-900/20 dark:text-blue-400'
  };

  const sizeClasses = {
    sm: 'px-2 py-0.5 text-xs',
    md: 'px-2.5 py-1 text-sm',
    lg: 'px-3 py-1.5 text-base'
  };

  return (
    <span className={`inline-flex items-center rounded-full font-medium ${variantClasses[variant]} ${sizeClasses[size]} ${className}`}>
      {children}
    </span>
  );
};

// 列表项组件
interface ListItemProps {
  icon?: React.ComponentType<{ className?: string }>;
  children: React.ReactNode;
  className?: string;
}

export const ListItem = ({ icon: Icon, children, className = '' }: ListItemProps) => {
  return (
    <li className={`flex items-start space-x-2 ${className}`}>
      {Icon ? (
        <Icon className="h-4 w-4 text-gray-400 mt-0.5 flex-shrink-0" />
      ) : (
        <span className="w-1.5 h-1.5 bg-primary-500 rounded-full mt-2 flex-shrink-0"></span>
      )}
      <span className="text-sm text-gray-600 dark:text-gray-400">{children}</span>
    </li>
  );
};