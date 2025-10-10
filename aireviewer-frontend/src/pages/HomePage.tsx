import React from 'react';
import { Link } from 'react-router-dom';
import { 
  CodeBracketIcon, 
  CpuChipIcon, 
  ShieldCheckIcon, 
  ClockIcon,
  UsersIcon,
  ChartBarIcon
} from '@heroicons/react/24/outline';
import { useAuth } from '../contexts/AuthContext';

export const HomePage: React.FC = () => {
  const { isAuthenticated, user } = useAuth();

  const features = [
    {
      icon: CpuChipIcon,
      title: 'AI智能评审',
      description: '基于先进的AI技术，自动分析代码质量、安全性和性能问题'
    },
    {
      icon: ShieldCheckIcon,
      title: '安全检测',
      description: '全面检测代码中的安全漏洞和潜在风险'
    },
    {
      icon: ClockIcon,
      title: '快速响应',
      description: '几分钟内完成代码评审，大幅提升开发效率'
    },
    {
      icon: UsersIcon,
      title: '团队协作',
      description: '支持团队成员协作评审，统一代码质量标准'
    },
    {
      icon: ChartBarIcon,
      title: '质量报告',
      description: '详细的质量分析报告，帮助团队持续改进'
    },
    {
      icon: CodeBracketIcon,
      title: '多语言支持',
      description: '支持主流编程语言的代码评审和分析'
    }
  ];

  if (isAuthenticated) {
    return (
      <div className="space-y-8">
        {/* Welcome Section */}
        <div className="bg-white rounded-lg shadow-sm p-6">
          <h1 className="text-2xl font-bold text-gray-900 mb-2">
            欢迎回来，{user?.displayName || user?.userName}！
          </h1>
          <p className="text-gray-600 mb-6">
            开始使用AI评审平台来提升您的代码质量
          </p>
          
          <div className="flex flex-wrap gap-4">
            <Link to="/projects/new" className="btn btn-primary">
              创建新项目
            </Link>
            <Link to="/projects" className="btn btn-secondary">
              查看项目
            </Link>
            <Link to="/reviews" className="btn btn-secondary">
              代码评审
            </Link>
          </div>
        </div>

        {/* Quick Stats */}
        <div className="grid grid-cols-1 md:grid-cols-3 gap-6">
          <div className="card">
            <div className="flex items-center">
              <div className="p-3 bg-primary-100 rounded-lg">
                <CodeBracketIcon className="h-6 w-6 text-primary-600" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-500">我的项目</p>
                <p className="text-2xl font-semibold text-gray-900">0</p>
              </div>
            </div>
          </div>

          <div className="card">
            <div className="flex items-center">
              <div className="p-3 bg-green-100 rounded-lg">
                <ShieldCheckIcon className="h-6 w-6 text-green-600" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-500">已完成评审</p>
                <p className="text-2xl font-semibold text-gray-900">0</p>
              </div>
            </div>
          </div>

          <div className="card">
            <div className="flex items-center">
              <div className="p-3 bg-orange-100 rounded-lg">
                <ClockIcon className="h-6 w-6 text-orange-600" />
              </div>
              <div className="ml-4">
                <p className="text-sm font-medium text-gray-500">待处理评审</p>
                <p className="text-2xl font-semibold text-gray-900">0</p>
              </div>
            </div>
          </div>
        </div>

        {/* Recent Activity */}
        <div className="card">
          <h2 className="text-lg font-semibold text-gray-900 mb-4">最近活动</h2>
          <div className="text-center py-8">
            <CodeBracketIcon className="h-12 w-12 text-gray-400 mx-auto mb-4" />
            <p className="text-gray-500">暂无活动记录</p>
            <p className="text-sm text-gray-400 mt-1">创建您的第一个项目开始使用</p>
          </div>
        </div>
      </div>
    );
  }

  return (
    <div className="space-y-16">
      {/* Hero Section */}
      <div className="text-center">
        <div className="flex justify-center mb-8">
          <div className="p-4 bg-primary-100 rounded-2xl">
            <CpuChipIcon className="h-16 w-16 text-primary-600" />
          </div>
        </div>
        
        <h1 className="text-4xl md:text-6xl font-bold text-gray-900 mb-6">
          AI 代码评审平台
        </h1>
        
        <p className="text-xl text-gray-600 mb-8 max-w-3xl mx-auto">
          利用先进的人工智能技术，为您的代码提供智能、全面、快速的评审服务，
          帮助团队提升代码质量，减少bug，提高开发效率。
        </p>
        
        <div className="flex flex-col sm:flex-row gap-4 justify-center">
          <Link to="/register" className="btn btn-primary text-lg px-8 py-3">
            免费开始使用
          </Link>
          <Link to="/login" className="btn btn-secondary text-lg px-8 py-3">
            登录账户
          </Link>
        </div>
      </div>

      {/* Features Section */}
      <div>
        <h2 className="text-3xl font-bold text-center text-gray-900 mb-12">
          平台特色
        </h2>
        
        <div className="grid grid-cols-1 md:grid-cols-2 lg:grid-cols-3 gap-8">
          {features.map((feature, index) => (
            <div key={index} className="card hover:shadow-lg transition-shadow">
              <div className="flex items-center mb-4">
                <div className="p-3 bg-primary-100 rounded-lg">
                  <feature.icon className="h-6 w-6 text-primary-600" />
                </div>
                <h3 className="text-lg font-semibold text-gray-900 ml-3">
                  {feature.title}
                </h3>
              </div>
              <p className="text-gray-600">
                {feature.description}
              </p>
            </div>
          ))}
        </div>
      </div>

      {/* CTA Section */}
      <div className="bg-primary-50 rounded-2xl p-8 text-center">
        <h2 className="text-3xl font-bold text-gray-900 mb-4">
          准备开始了吗？
        </h2>
        <p className="text-lg text-gray-600 mb-8 max-w-2xl mx-auto">
          立即注册，体验AI驱动的代码评审服务，让您的代码质量更上一层楼。
        </p>
        <Link to="/register" className="btn btn-primary text-lg px-8 py-3">
          立即注册
        </Link>
      </div>
    </div>
  );
};