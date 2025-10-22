import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
import { UISettingsProvider } from './providers/UISettingsProvider';
import { NotificationProvider } from './contexts/NotificationContext';
import { Layout } from './components/Layout';
import { ProtectedRoute } from './components/ProtectedRoute';
import { NotificationContainer, ConnectionStatus } from './components/NotificationContainer';
import { HomePage } from './pages/HomePage';
import { LoginPage } from './pages/LoginPage';
import { RegisterPage } from './pages/RegisterPage';
import { ProjectsPage } from './pages/ProjectsPage';
import { CreateProjectPage } from './pages/CreateProjectPage';
import { ProjectDetailPage } from './pages/ProjectDetailPage';
import { ReviewsPage } from './pages/ReviewsPage';
import { CreateReviewPage } from './pages/CreateReviewPage';
import { ReviewDetailPage } from './pages/ReviewDetailPage';
import { ReviewSettingsPage } from './pages/ReviewSettingsPage';
import { ProfilePage } from './pages/ProfilePage';
import { NotificationsPage } from './pages/NotificationsPage';
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import LLMConfigurationPage from './pages/admin/LLMConfigurationPage';
import PromptsPage from './pages/admin/PromptsPage';
import TokenUsagePage from './pages/TokenUsagePage';
import GitCredentialsPage from './pages/GitCredentialsPage';
import RepositoryStatusPage from './pages/RepositoryStatusPage';
import './App.css';

// Create a client with optimized settings
// 性能优化：限制缓存大小，防止内存泄漏
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 3 * 60 * 1000, // 3分钟（降低默认缓存时间）
      gcTime: 5 * 60 * 1000, // 5分钟（原cacheTime，降低内存占用）
      retry: 1,
      // 性能优化：限制并发请求数
      refetchOnWindowFocus: false, // 关闭窗口聚焦时自动刷新
      refetchOnReconnect: false, // 关闭重新连接时自动刷新
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
      <UISettingsProvider>
        <AuthProvider>
          <NotificationProvider>
          <Router>
            <Routes>
              {/* Public routes without layout */}
              <Route path="/login" element={<LoginPage />} />
              <Route path="/register" element={<RegisterPage />} />
              <Route path="/forgot-password" element={<ForgotPasswordPage />} />
              
              {/* Routes with layout */}
              <Route path="/" element={<Layout />}>
                <Route index element={<HomePage />} />
                <Route 
                  path="projects" 
                  element={
                    <ProtectedRoute>
                      <ProjectsPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="projects/new" 
                  element={
                    <ProtectedRoute>
                      <CreateProjectPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="projects/:id" 
                  element={
                    <ProtectedRoute>
                      <ProjectDetailPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="projects/:id/reviews/new" 
                  element={
                    <ProtectedRoute>
                      <CreateReviewPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="reviews" 
                  element={
                    <ProtectedRoute>
                      <ReviewsPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="reviews/new" 
                  element={
                    <ProtectedRoute>
                      <CreateReviewPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="reviews/:id" 
                  element={
                    <ProtectedRoute>
                      <ReviewDetailPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="settings/reviews" 
                  element={
                    <ProtectedRoute>
                      <ReviewSettingsPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="profile" 
                  element={
                    <ProtectedRoute>
                      <ProfilePage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="notifications" 
                  element={
                    <ProtectedRoute>
                      <NotificationsPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="admin/llm-config" 
                  element={
                    <ProtectedRoute>
                      <LLMConfigurationPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="admin/prompts" 
                  element={
                    <ProtectedRoute>
                      <PromptsPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="usage/tokens" 
                  element={
                    <ProtectedRoute>
                      <TokenUsagePage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="settings/git-credentials" 
                  element={
                    <ProtectedRoute>
                      <GitCredentialsPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="repositories/:id/status" 
                  element={
                    <ProtectedRoute>
                      <RepositoryStatusPage />
                    </ProtectedRoute>
                  } 
                />
                <Route 
                  path="projects/:id/prompts" 
                  element={
                    <ProtectedRoute>
                      <PromptsPage />
                    </ProtectedRoute>
                  } 
                />
              </Route>

              {/* Catch all route */}
              <Route path="*" element={<Navigate to="/" replace />} />
            </Routes>
          </Router>
          <NotificationContainer />
          <ConnectionStatus />
        </NotificationProvider>
        </AuthProvider>
      </UISettingsProvider>
    </QueryClientProvider>
  );
}

export default App;
