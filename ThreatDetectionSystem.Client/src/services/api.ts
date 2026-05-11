import axios from 'axios';
import type { Alert, AlertStats } from '../types';

const api = axios.create({
  baseURL: 'http://localhost:5045/api',
  timeout: 10000,
});

export const alertApi = {
  getAlerts: (range?: string) =>
    api.get<Alert[]>('/Alert', { params: range ? { range } : {} }),

  getStats: (range?: string) =>
    api.get<AlertStats>('/Alert/stats', { params: range ? { range } : {} }),

  getHealth: () => api.get('/Alert/health'),
};