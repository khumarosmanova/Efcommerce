import axios from 'axios';
import toast from 'react-hot-toast';
import {
  getStoredAccessToken,
  getStoredRefreshToken,
  setAuthData,
  clearAuthData,
  getStoredUser,
} from '../utils/storage';

const API_BASE_URL = import.meta.env.VITE_API_URL || 'http://localhost:5078/api';

const api = axios.create({
  baseURL: API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});

// Request Interceptor: Attach JWT Bearer token
api.interceptors.request.use(
  (config) => {
    const token = getStoredAccessToken();
    if (token) {
      config.headers.Authorization = `Bearer ${token}`;
    }
    return config;
  },
  (error) => Promise.reject(error)
);

// Concurrency control for refreshing token
let isRefreshing = false;
let failedQueue = [];

const processQueue = (error, token = null) => {
  failedQueue.forEach((prom) => {
    if (error) {
      prom.reject(error);
    } else {
      prom.resolve(token);
    }
  });
  failedQueue = [];
};

// Response Interceptor: Handle 401 & Automatic Token Refresh & Global Errors
api.interceptors.response.use(
  (response) => {
    return response;
  },
  async (error) => {
    const originalRequest = error.config;

    // Handle 401 Unauthorized (attempt refresh)
    if (error.response?.status === 401 && !originalRequest._retry) {
      if (originalRequest.url?.includes('/auth/login') || originalRequest.url?.includes('/auth/register')) {
        return Promise.reject(error);
      }

      if (isRefreshing) {
        return new Promise((resolve, reject) => {
          failedQueue.push({ resolve, reject });
        })
          .then((token) => {
            originalRequest.headers.Authorization = `Bearer ${token}`;
            return api(originalRequest);
          })
          .catch((err) => Promise.reject(err));
      }

      originalRequest._retry = true;
      isRefreshing = true;

      const refreshToken = getStoredRefreshToken();
      const accessToken = getStoredAccessToken();

      if (!refreshToken || !accessToken) {
        clearAuthData();
        isRefreshing = false;
        return Promise.reject(error);
      }

      try {
        const refreshResponse = await axios.post(`${API_BASE_URL}/auth/refresh`, {
          accessToken,
          refreshToken,
        });

        const authData = refreshResponse.data.data;
        const newAccessToken = authData.accessToken;
        const newRefreshToken = authData.refreshToken;
        const user = authData.user || getStoredUser();

        setAuthData(newAccessToken, newRefreshToken, user);

        api.defaults.headers.common['Authorization'] = `Bearer ${newAccessToken}`;
        originalRequest.headers.Authorization = `Bearer ${newAccessToken}`;

        processQueue(null, newAccessToken);
        isRefreshing = false;

        return api(originalRequest);
      } catch (refreshError) {
        processQueue(refreshError, null);
        isRefreshing = false;
        clearAuthData();
        toast.error('Session expired. Please log in again.');
        window.location.href = '/login';
        return Promise.reject(refreshError);
      }
    }

    // Global error message extraction and toast
    const responseData = error.response?.data;
    const errorMessage =
      responseData?.message ||
      (responseData?.errors && responseData.errors.length > 0 ? responseData.errors[0] : null) ||
      error.message ||
      'An unexpected error occurred';

    // Do not show generic toast on intentional 401 or 404 queries if handled in UI
    if (error.response?.status === 403) {
      toast.error('Access Denied: You do not have permission for this action.');
    } else if (error.response?.status >= 500) {
      toast.error(`Server Error: ${errorMessage}`);
    }

    return Promise.reject(error);
  }
);

export default api;
