import axios from 'axios';

export const apiClient = axios.create({
  baseURL: import.meta.env.VITE_PREMPOINTS_API_BASE_URL,
  headers: {
    'Content-Type': 'application/json',
  },
});
