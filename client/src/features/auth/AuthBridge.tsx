import { useAuth } from '@workos-inc/authkit-react';
import { useEffect } from 'react';
import { setAccessTokenGetter } from '../../lib/api';

/**
 * Hands AuthKit's token getter to the axios client.
 *
 * The getter has to be published from inside the provider, because useAuth is a
 * hook and the axios interceptor is not a component. Rendering this once near
 * the root is what connects the two.
 */
export const AuthBridge = ({ children }: { children: React.ReactNode }) => {
  const { getAccessToken, user } = useAuth();

  useEffect(() => {
    setAccessTokenGetter(async () => {
      if (!user) {
        return undefined;
      }

      try {
        // AuthKit refreshes the token behind this call when it is close to
        // expiring, so it is asked for per request rather than cached here.
        return await getAccessToken();
      } catch {
        // A failed refresh means the session is gone. Sending no header lets
        // the API answer 401 and the UI prompt a fresh sign-in, which is
        // better than throwing inside an interceptor.
        return undefined;
      }
    });
  }, [getAccessToken, user]);

  return children;
};
