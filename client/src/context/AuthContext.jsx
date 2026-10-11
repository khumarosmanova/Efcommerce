import React, { createContext, useContext, useState, useEffect, useCallback } from 'react';
import { jwtDecode } from 'jwt-decode';
import { authApi } from '../api/endpoints';
import {
  getStoredAccessToken,
  getStoredRefreshToken,
  getStoredUser,
  setAuthData,
  clearAuthData,
} from '../utils/storage';

const AuthContext = createContext(null);

export const AuthProvider = ({ children }) => {
  const [user, setUser] = useState(getStoredUser());
  const [token, setToken] = useState(getStoredAccessToken());
  const [loading, setLoading] = useState(true);

  // Helper to extract roles and permissions from decoded JWT payload
  const extractClaims = useCallback((jwtToken) => {
    if (!jwtToken) return { roles: [], permissions: [] };
    try {
      const decoded = jwtDecode(jwtToken);
      
      // Extract roles (can be single string or array)
      const roleClaim = decoded['http://schemas.microsoft.com/ws/2008/06/identity/claims/role'] || decoded['role'] || [];
      const roles = Array.isArray(roleClaim) ? roleClaim : [roleClaim].filter(Boolean);

      // Extract permissions (can be single string or array)
      const permClaim = decoded['permission'] || [];
      const permissions = Array.isArray(permClaim) ? permClaim : [permClaim].filter(Boolean);

      return { roles, permissions };
    } catch (err) {
      console.error('Error decoding JWT token:', err);
      return { roles: [], permissions: [] };
    }
  }, []);

  // Compute active roles and permissions (prefer user object, fallback to decoded JWT)
  const decodedClaims = extractClaims(token);
  const roles = user?.roles?.length ? user.roles : decodedClaims.roles;
  const permissions = user?.permissions?.length ? user.permissions : decodedClaims.permissions;

  // Check if current user has SuperAdmin role or a specific permission
  const hasPermission = useCallback((permissionName) => {
    if (!roles || !permissions) return false;
    if (roles.includes('SuperAdmin')) return true;
    return permissions.includes(permissionName);
  }, [roles, permissions]);

  // Check if current user has any of the admin/moderator roles
  const isAdmin = roles.some((r) => ['SuperAdmin', 'Admin', 'Moderator'].includes(r));

  // Initialize and verify session on initial load
  useEffect(() => {
    const initAuth = async () => {
      const storedToken = getStoredAccessToken();
      const storedRefresh = getStoredRefreshToken();

      if (storedToken && storedRefresh) {
        try {
          // Fetch fresh user profile and roles
          const res = await authApi.getCurrentUser();
          if (res?.data) {
            setUser(res.data);
            setAuthData(storedToken, storedRefresh, res.data);
          }
        } catch (error) {
          console.warn('Failed to load current user on init:', error);
        }
      } else {
        clearAuthData();
        setUser(null);
        setToken(null);
      }
      setLoading(false);
    };

    initAuth();
  }, []);

  const login = async (email, password) => {
    const res = await authApi.login({ email, password });
    if (res?.succeeded && res.data) {
      const { accessToken, refreshToken, user: userData } = res.data;
      setToken(accessToken);
      setUser(userData);
      setAuthData(accessToken, refreshToken, userData);
      return { success: true, user: userData };
    }
    return { success: false, message: res?.message || 'Login failed' };
  };

  const register = async (formData) => {
    const res = await authApi.register(formData);
    if (res?.succeeded && res.data) {
      const { accessToken, refreshToken, user: userData } = res.data;
      setToken(accessToken);
      setUser(userData);
      setAuthData(accessToken, refreshToken, userData);
      return { success: true, user: userData };
    }
    return { success: false, message: res?.message || 'Registration failed' };
  };

  const logout = () => {
    clearAuthData();
    setUser(null);
    setToken(null);
  };

  const value = {
    user,
    token,
    loading,
    isAuthenticated: !!token && !!user,
    roles,
    permissions,
    isAdmin,
    hasPermission,
    login,
    register,
    logout,
  };

  return <AuthContext.Provider value={value}>{children}</AuthContext.Provider>;
};

export const useAuth = () => {
  const context = useContext(AuthContext);
  if (!context) {
    throw new Error('useAuth must be used within an AuthProvider');
  }
  return context;
};
