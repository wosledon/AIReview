import { BrowserRouter as Router, Routes, Route, Navigate } from 'react-router-dom';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { AuthProvider } from './contexts/AuthContext';
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
import { ForgotPasswordPage } from './pages/ForgotPasswordPage';
import LLMConfigurationPage from './pages/admin/LLMConfigurationPage';
import './App.css';

// Create a client
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000, // 5 minutes
      retry: 1,
    },
  },
});

function App() {
  return (
    <QueryClientProvider client={queryClient}>
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
                  path="admin/llm-config" 
                  element={
                    <ProtectedRoute>
                      <LLMConfigurationPage />
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
    </QueryClientProvider>
  );
}

export default App;
