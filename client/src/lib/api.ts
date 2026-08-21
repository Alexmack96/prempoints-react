import axios from 'axios';

export const apiClient = axios.create({
  // Same-origin. The Vite dev server proxies /api to the .NET API (see
  // vite.config.ts); in a deployment whatever serves these assets does the same.
  baseURL: '/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
});
