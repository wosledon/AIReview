import { apiClient } from './api-client';
import type { 
  User, 
  AuthResponse, 
  LoginRequest, 
  RegisterRequest, 
  RefreshTokenRequest 
} from '../types/auth';

export class AuthService {
  async login(request: LoginRequest): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>('/auth/login', request);
    
    if (response.token) {
      apiClient.setToken(response.token);
    }
    
    return response;
  }

  async register(request: RegisterRequest): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>('/auth/register', request);
    
    if (response.token) {
      apiClient.setToken(response.token);
    }
    
    return response;
  }

  async refreshToken(request: RefreshTokenRequest): Promise<AuthResponse> {
    const response = await apiClient.post<AuthResponse>('/auth/refresh', request);
    
    if (response.token) {
      apiClient.setToken(response.token);
    }
    
    return response;
  }

  async getCurrentUser(): Promise<User> {
    return await apiClient.get<User>('/auth/profile');
  }

  logout(): void {
    apiClient.setToken(null);
    // Clear any other user data from localStorage/sessionStorage
    localStorage.removeItem('refresh_token');
    localStorage.removeItem('user_data');
  }

  isAuthenticated(): boolean {
    const token = apiClient.getToken();
    if (!token) return false;

    try {
      // Simple JWT token expiration check
      const payload = JSON.parse(atob(token.split('.')[1]));
      const currentTime = Date.now() / 1000;
      return payload.exp > currentTime;
    } catch {
      return false;
    }
  }

  getStoredUser(): User | null {
    try {
      const userData = localStorage.getItem('user_data');
      return userData ? JSON.parse(userData) : null;
    } catch {
      return null;
    }
  }

  setStoredUser(user: User): void {
    localStorage.setItem('user_data', JSON.stringify(user));
  }

  getStoredRefreshToken(): string | null {
    return localStorage.getItem('refresh_token');
  }

  setStoredRefreshToken(token: string): void {
    localStorage.setItem('refresh_token', token);
  }
}

export const authService = new AuthService();