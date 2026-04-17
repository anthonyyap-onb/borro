import axios from 'axios';

const apiClient = axios.create({
  // Use a relative base so requests go to the same origin.
  // Vite's dev-server proxy (vite.config.ts) forwards /api → backend.
  baseURL: '',
});

apiClient.interceptors.request.use((config) => {
  const token = localStorage.getItem('borro_token');
  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }
  return config;
});

export default apiClient;
