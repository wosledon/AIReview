export interface User {
  id: string;
  email: string;
  userName: string;
  displayName?: string;
  avatar?: string;
  createdAt: string;
  updatedAt: string;
}

export interface AuthResponse {
  token: string;
  refreshToken: string;
  user: User;
  expiresAt: string;
}

export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  email: string;
  password: string;
  confirmPassword: string;
  userName: string;
  displayName?: string;
}

export interface RefreshTokenRequest {
  refreshToken: string;
}