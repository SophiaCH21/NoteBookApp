import { jwtDecode } from 'jwt-decode';

interface TokenPayload {
  exp: number;
  email: string;
  id: string;
}

export const getToken = (): string | null => {
  return localStorage.getItem('token');
};

export const setToken = (token: string): void => {
  localStorage.setItem('token', token);
};

export const removeToken = (): void => {
  localStorage.removeItem('token');
};

export const isTokenExpired = (token: string): boolean => {
  try {
    const decoded = jwtDecode<TokenPayload>(token);
    const currentTime = Date.now() / 1000;
    return decoded.exp < currentTime;
  } catch {
    return true;
  }
};

export const getTokenPayload = (token: string): TokenPayload | null => {
  try {
    return jwtDecode<TokenPayload>(token);
  } catch {
    return null;
  }
};

export const getAuthHeader = (): { Authorization: string } | Record<string, never> => {
  const token = getToken();
  if (!token || isTokenExpired(token)) {
    removeToken();
    return {};
  }
  return { Authorization: `Bearer ${token}` };
}; 