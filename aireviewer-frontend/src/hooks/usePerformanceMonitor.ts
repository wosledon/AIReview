import { useEffect, useRef } from 'react';

interface PerformanceMetrics {
  renderCount: number;
  lastRenderTime: number;
  averageRenderTime: number;
}

/**
 * 性能监控Hook - 用于调试和优化
 * 在开发环境中监控组件的渲染性能
 */
export function usePerformanceMonitor(componentName: string, enabled = false) {
  const metricsRef = useRef<PerformanceMetrics>({
    renderCount: 0,
    lastRenderTime: 0,
    averageRenderTime: 0,
  });
  const renderStartTimeRef = useRef<number>(0);

  // 记录渲染开始时间
  if (enabled) {
    renderStartTimeRef.current = performance.now();
  }

  useEffect(() => {
    if (!enabled) return;

    const renderEndTime = performance.now();
    const renderTime = renderEndTime - renderStartTimeRef.current;
    const metrics = metricsRef.current;

    metrics.renderCount++;
    metrics.lastRenderTime = renderTime;
    metrics.averageRenderTime = 
      (metrics.averageRenderTime * (metrics.renderCount - 1) + renderTime) / metrics.renderCount;

    // 性能警告阈值
    if (renderTime > 16) { // 超过一帧的时间（60fps = 16.67ms）
      console.warn(
        `[Performance] ${componentName} 渲染耗时 ${renderTime.toFixed(2)}ms (超过16ms)`,
        {
          renderCount: metrics.renderCount,
          averageRenderTime: metrics.averageRenderTime.toFixed(2),
        }
      );
    }

    // 每10次渲染输出统计
    if (metrics.renderCount % 10 === 0) {
      console.log(
        `[Performance] ${componentName} 统计:`,
        {
          totalRenders: metrics.renderCount,
          lastRenderTime: metrics.lastRenderTime.toFixed(2) + 'ms',
          averageRenderTime: metrics.averageRenderTime.toFixed(2) + 'ms',
        }
      );
    }
  }, [componentName, enabled]);
}

/**
 * 内存使用监控Hook
 */
export function useMemoryMonitor(componentName: string, enabled = false) {
  useEffect(() => {
    if (!enabled) return;
    
    // 扩展Performance接口以包含memory属性
    interface PerformanceWithMemory extends Performance {
      memory?: {
        usedJSHeapSize: number;
        totalJSHeapSize: number;
        jsHeapSizeLimit: number;
      };
    }
    
    // 检查浏览器是否支持性能API
    const perf = performance as PerformanceWithMemory;
    if (!perf.memory) {
      console.warn('[Memory] 浏览器不支持内存监控（仅Chrome支持）');
      return;
    }

    const checkMemory = () => {
      const memory = perf.memory!;
      const used = (memory.usedJSHeapSize / 1024 / 1024).toFixed(2);
      const total = (memory.totalJSHeapSize / 1024 / 1024).toFixed(2);
      const limit = (memory.jsHeapSizeLimit / 1024 / 1024).toFixed(2);

      console.log(
        `[Memory] ${componentName}: ${used}MB / ${total}MB (限制: ${limit}MB)`
      );

      // 内存使用超过80%时警告
      if (memory.usedJSHeapSize / memory.jsHeapSizeLimit > 0.8) {
        console.warn(
          `[Memory] ${componentName} 内存使用过高！建议清理缓存或减少数据加载。`
        );
      }
    };

    // 立即检查一次
    checkMemory();

    // 每5秒检查一次
    const interval = setInterval(checkMemory, 5000);

    return () => clearInterval(interval);
  }, [componentName, enabled]);
}

/**
 * 清理建议Hook - 检测潜在的内存泄漏
 */
export function useCleanupDetector(componentName: string, enabled = false) {
  useEffect(() => {
    if (!enabled) return;

    console.log(`[Cleanup] ${componentName} 已挂载`);

    return () => {
      console.log(`[Cleanup] ${componentName} 已卸载`);
      
      // 检查是否有未清理的定时器或监听器
      // 这需要在组件中手动维护清理列表
      console.log(`[Cleanup] ${componentName} 清理完成，请确保所有资源已释放`);
    };
  }, [componentName, enabled]);
}
