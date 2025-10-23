import axios from 'axios';
import type { AxiosInstance, AxiosRequestConfig, AxiosError } from 'axios';

export interface ApiConfig {
  baseURL: string;
  timeout?: number;
  defaultHeaders?: Record<string, string>;
}

export interface EnhancedAxiosError extends AxiosError {
  userMessage?: string;
  errorCode?: string;
  details?: unknown[];
}

export class ApiClient {
  private client: AxiosInstance;
  private token: string | null = null;

  constructor(config: ApiConfig) {
    // 初始化时从 localStorage 加载 token
    this.token = localStorage.getItem('auth_token');
    
    this.client = axios.create({
      baseURL: config.baseURL,
      timeout: config.timeout || 10000,
      headers: {
        'Content-Type': 'application/json',
        ...config.defaultHeaders,
      },
    });

    // Request interceptor to add authentication token
    this.client.interceptors.request.use(
      (config) => {
        if (this.token) {
          config.headers.Authorization = `Bearer ${this.token}`;
        }
        return config;
      },
      (error) => {
        return Promise.reject(error);
      }
    );

    // Response interceptor to handle common errors
    this.client.interceptors.response.use(
      (response) => response,
      async (error) => {
        const originalRequest = error.config;

        // Handle token refresh for 401 errors (if needed)
        if (error.response?.status === 401 && !originalRequest._retry) {
          originalRequest._retry = true;
          this.setToken(null);
          window.dispatchEvent(new CustomEvent('auth:logout'));
          return Promise.reject(error);
        }

        // Enhance error with user-friendly message
        const enhancedError = this.enhanceError(error);
        return Promise.reject(enhancedError);
      }
    );
  }

  private enhanceError(error: unknown): EnhancedAxiosError {
    const axiosError = error as EnhancedAxiosError;

    if (!axiosError.response) {
      // Network error
      axiosError.userMessage = '网络连接失败，请检查网络连接';
      axiosError.errorCode = 'NETWORK_ERROR';
      return axiosError;
    }

    const { status, data } = axiosError.response;

    // Use server-provided error message if available
    if (data && typeof data === 'object' && 'message' in data) {
      axiosError.userMessage = (data as { message: string }).message;
    } else {
      // Fallback to status-based messages
      axiosError.userMessage = this.getErrorMessageByStatus(status);
    }

    if (data && typeof data === 'object') {
      axiosError.errorCode = ('errorCode' in data ? (data as { errorCode: string }).errorCode : undefined) || `HTTP_${status}`;
      axiosError.details = ('errors' in data ? (data as { errors: unknown[] }).errors : undefined) || [];
    } else {
      axiosError.errorCode = `HTTP_${status}`;
      axiosError.details = [];
    }

    return axiosError;
  }

  private getErrorMessageByStatus(status: number): string {
    switch (status) {
      case 400:
        return '请求参数有误，请检查输入信息';
      case 401:
        return '登录已过期，请重新登录';
      case 403:
        return '没有权限执行此操作';
      case 404:
        return '请求的资源不存在';
      case 409:
        return '资源冲突，请稍后重试';
      case 422:
        return '数据验证失败，请检查输入';
      case 429:
        return '请求过于频繁，请稍后再试';
      case 500:
        return '服务器内部错误，请稍后重试';
      case 502:
        return '服务暂时不可用，请稍后重试';
      case 503:
        return '服务维护中，请稍后重试';
      default:
        return '发生未知错误，请稍后重试';
    }
  }

  setToken(token: string | null): void {
    this.token = token;
    if (token) {
      localStorage.setItem('auth_token', token);
    } else {
      localStorage.removeItem('auth_token');
    }
  }

  getToken(): string | null {
    if (!this.token) {
      this.token = localStorage.getItem('auth_token');
    }
    return this.token;
  }

  async get<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.get<T>(url, config);
    return response.data;
  }

  async post<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.post<T>(url, data, config);
    return response.data;
  }

  async put<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.put<T>(url, data, config);
    return response.data;
  }

  async patch<T>(url: string, data?: unknown, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.patch<T>(url, data, config);
    return response.data;
  }

  async delete<T>(url: string, config?: AxiosRequestConfig): Promise<T> {
    const response = await this.client.delete<T>(url, config);
    return response.data;
  }
}

// Create default API client instance
const apiConfig: ApiConfig = {
  baseURL: import.meta.env.VITE_API_BASE_URL || 'https://localhost:5000/api/v1',
  timeout: 15000,
};

export const apiClient = new ApiClient(apiConfig);