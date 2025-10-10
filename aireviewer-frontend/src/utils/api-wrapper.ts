// API 响应包装器，用于处理不同的响应格式

export interface ApiResponse<T> {
  success: boolean;
  data?: T;
  message?: string;
  errors?: string[];
}

/**
 * 包装 API 响应，统一处理 ApiResponse 格式
 */
export function wrapApiResponse<T>(response: unknown): T {
  // 如果响应已经是期望的格式（非 ApiResponse），直接返回
  if (typeof response !== 'object' || response === null || 
      !('success' in response) && !('data' in response)) {
    return response as T;
  }

  const apiResponse = response as ApiResponse<T>;

  // 如果是 ApiResponse 格式，提取 data 字段
  if (apiResponse.success && apiResponse.data) {
    return apiResponse.data as T;
  }

  // 如果响应失败，抛出错误
  if (!apiResponse.success) {
    const errorMessage = apiResponse.message || '请求失败';
    const errors = apiResponse.errors || [];
    throw new Error(`${errorMessage}: ${errors.join(', ')}`);
  }

  // 兜底返回
  return response as T;
}

/**
 * 处理可能包装在 ApiResponse 中的响应
 */
export async function handleApiResponse<T>(apiCall: () => Promise<unknown>): Promise<T> {
  const response = await apiCall();
  return wrapApiResponse<T>(response);
}