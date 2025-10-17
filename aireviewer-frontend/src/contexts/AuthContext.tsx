/* eslint-disable react-refresh/only-export-components */
import React, { createContext, useContext, useEffect, useState, useCallback } from 'react';
import { authService } from '../services/auth.service';
import type { User, LoginRequest, RegisterRequest } from '../types/auth';

interface AuthContextType {
  user: User | null;
  isAuthenticated: boolean;
  isLoading: boolean;
  login: (credentials: LoginRequest) => Promise<void>;
  register: (userData: RegisterRequest) => Promise<void>;
  logout: () => void;
  refreshUser: () => Promise<void>;
}

const AuthContext = createContext<AuthContextType | undefined>(undefined);

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (context === undefined) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};

interface AuthProviderProps {
  children: React.ReactNode;
}

export const AuthProvider: React.FC<AuthProviderProps> = ({ children }) => {
  const [user, setUser] = useState<User | null>(null);
  const [isLoading, setIsLoading] = useState(true);

  const isAuthenticated = !!user && authService.isAuthenticated();

  const logout = useCallback(() => {
    authService.logout();
    setUser(null);
  }, []);

  const refreshUser = useCallback(async () => {
    try {
      const currentUser = await authService.getCurrentUser();
      setUser(currentUser);
      authService.setStoredUser(currentUser);
    } catch (error) {
      console.error('Failed to refresh user:', error);
      logout();
    }
  }, [logout]);

  const initializeAuth = useCallback(async () => {
    try {
      // Check if user is already authenticated
      if (authService.isAuthenticated()) {
        const storedUser = authService.getStoredUser();
        if (storedUser) {
          setUser(storedUser);
        } else {
          // Token exists but no user data, fetch from server
          await refreshUser();
        }
      }
    } catch (error) {
      console.error('Failed to initialize authentication:', error);
      // If there's an error, clear any stored auth data
      authService.logout();
    } finally {
      setIsLoading(false);
    }
  }, [refreshUser]);

  useEffect(() => {
    initializeAuth();
    
    // Listen for logout events from other tabs/components
    const handleLogout = () => {
      setUser(null);
    };
    
    window.addEventListener('auth:logout', handleLogout);
    
    return () => {
      window.removeEventListener('auth:logout', handleLogout);
    };
  }, [initializeAuth]);

  const login = async (credentials: LoginRequest) => {
    setIsLoading(true);
    try {
      const response = await authService.login(credentials);
      setUser(response.user);
      authService.setStoredUser(response.user);
      if (response.refreshToken) {
        authService.setStoredRefreshToken(response.refreshToken);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const register = async (userData: RegisterRequest) => {
    setIsLoading(true);
    try {
      const response = await authService.register(userData);
      setUser(response.user);
      authService.setStoredUser(response.user);
      if (response.refreshToken) {
        authService.setStoredRefreshToken(response.refreshToken);
      }
    } finally {
      setIsLoading(false);
    }
  };

  const value: AuthContextType = {
    user,
    isAuthenticated,
    isLoading,
    login,
    register,
    logout,
    refreshUser,
  };

  return (
    <AuthContext.Provider value={value}>
      {children}
    </AuthContext.Provider>
  );
};