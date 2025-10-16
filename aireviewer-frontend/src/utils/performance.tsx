import { useEffect, useRef } from 'react';

interface PerformanceMonitorProps {
  componentName: string;
  enabled?: boolean;
}

/**
 * 性能监控组件 - 用于开发环境监控组件渲染性能
 */
export function PerformanceMonitor({ componentName, enabled = import.meta.env.DEV }: PerformanceMonitorProps) {
  const renderCount = useRef(0);
  const lastRenderTime = useRef(Date.now());

  useEffect(() => {
    if (!enabled) return;

    renderCount.current += 1;
    const now = Date.now();
    const timeSinceLastRender = now - lastRenderTime.current;
    lastRenderTime.current = now;

    console.log(`[Perf] ${componentName} - Render #${renderCount.current}, Time since last: ${timeSinceLastRender}ms`);
  });

  return null;
}

/**
 * 测量函数执行时间的工具
 */
export function measureTime<T>(name: string, fn: () => T): T {
  const start = performance.now();
  const result = fn();
  const end = performance.now();
  console.log(`[Perf] ${name} took ${(end - start).toFixed(2)}ms`);
  return result;
}

/**
 * 测量异步函数执行时间的工具
 */
export async function measureTimeAsync<T>(name: string, fn: () => Promise<T>): Promise<T> {
  const start = performance.now();
  const result = await fn();
  const end = performance.now();
  console.log(`[Perf] ${name} took ${(end - start).toFixed(2)}ms`);
  return result;
}

/**
 * Hook: 监控组件挂载和卸载时间
 */
export function useComponentLifecycle(componentName: string, enabled = import.meta.env.DEV) {
  const mountTime = useRef(0);

  useEffect(() => {
    if (!enabled) return;

    mountTime.current = Date.now();
    console.log(`[Perf] ${componentName} mounted`);

    return () => {
      const lifetime = Date.now() - mountTime.current;
      console.log(`[Perf] ${componentName} unmounted after ${lifetime}ms`);
    };
  }, [componentName, enabled]);
}

/**
 * Hook: 监控特定值变化的频率
 */
export function useValueChangeMonitor<T>(name: string, value: T, enabled = import.meta.env.DEV) {
  const changeCount = useRef(0);
  const lastChangeTime = useRef(Date.now());
  const previousValue = useRef<T>(value);

  useEffect(() => {
    if (!enabled) return;

    if (previousValue.current !== value) {
      changeCount.current += 1;
      const now = Date.now();
      const timeSinceLastChange = now - lastChangeTime.current;
      lastChangeTime.current = now;

      console.log(
        `[Perf] ${name} changed (${changeCount.current} times), ` +
        `Time since last: ${timeSinceLastChange}ms, ` +
        `Previous: ${JSON.stringify(previousValue.current)}, ` +
        `Current: ${JSON.stringify(value)}`
      );

      previousValue.current = value;
    }
  }, [name, value, enabled]);
}
