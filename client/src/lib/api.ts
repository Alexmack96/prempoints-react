import axios from 'axios';

export const apiClient = axios.create({
  // Same-origin. The Vite dev server proxies /api to the .NET API (see
  // vite.config.ts); in a deployment the API serves these assets itself.
  baseURL: '/api/v1',
  headers: {
    'Content-Type': 'application/json',
  },
});

/**
 * How the interceptor below reaches AuthKit's token.
 *
 * getAccessToken comes from a hook, and an axios interceptor is not a React
 * component, so the token getter is handed over once at startup by a component
 * that *is* inside the provider. The alternative — creating the axios client
 * inside a hook — would rebuild it on every render and break query caching.
 */
type TokenGetter = () => Promise<string | undefined>;

let getToken: TokenGetter = async () => undefined;

export const setAccessTokenGetter = (getter: TokenGetter) => {
  getToken = getter;
};

apiClient.interceptors.request.use(async (config) => {
  const token = await getToken();

  if (token) {
    config.headers.Authorization = `Bearer ${token}`;
  }

  return config;
});
