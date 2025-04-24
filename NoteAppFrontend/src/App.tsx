import { useEffect } from 'react';
import { Routes, Route, Navigate } from 'react-router-dom';
import Layout from './components/layout/Layout';
import LoginPage from './pages/LoginPage';
import RegisterPage from './pages/RegisterPage';
import NotesPage from './pages/NotesPage';
import ProtectedRoute from './components/auth/ProtectedRoute';
import { useSelector } from 'react-redux';
import { selectAuth } from './store/authSlice';

function App() {
  const { isAuthenticated } = useSelector(selectAuth);

  return (
    <Routes>
      <Route path="/" element={<Layout />}>
        <Route index element={<Navigate to={isAuthenticated ? "/notes" : "/login"} replace />} />
        <Route path="login" element={<LoginPage />} />
        <Route path="register" element={<RegisterPage />} />
        <Route
          path="notes"
          element={
            <ProtectedRoute>
              <NotesPage />
            </ProtectedRoute>
          }
        />
        <Route path="*" element={<Navigate to="/" replace />} />
      </Route>
    </Routes>
  );
}

export default App;
