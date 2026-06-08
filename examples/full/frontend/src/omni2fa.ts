import { createOmni2Fa } from '@omni2fa/core';

/**
 * The Omni2FA core singleton. With the v0.6 session-token client API the custom-fetch workaround is
 * gone: the client routes the pre-auth token to /challenge/* and the host session token (set via
 * AuthContext → omni.client.setSessionToken) to everything else.
 */
export const omni = createOmni2Fa({ baseUrl: '/api/2fa' });
