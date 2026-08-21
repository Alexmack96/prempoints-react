/**
 * The WorkOS client id this app signs in against.
 *
 * Not a secret — it is a public identifier that appears in the sign-in URL, and
 * it is the same value the API validates tokens against in appsettings.json.
 * It is environment-specific though, so VITE_WORKOS_CLIENT_ID overrides it for
 * a deployment pointing at a different WorkOS environment.
 *
 * Blank counts as absent. The Docker build declares the variable whether or not
 * a build arg was passed, so an unset arg arrives here as an empty string, and
 * `??` alone would take it — leaving the client signing in against a WorkOS
 * environment that does not exist.
 */
const configured = import.meta.env.VITE_WORKOS_CLIENT_ID?.trim();

export const workOsClientId =
  configured && configured.length > 0 ? configured : 'client_01KBQJ6BCKS24TJF4RG5GGBCA7';
