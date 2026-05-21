import type { ReactNode } from 'react';
import type { IOmni2Fa } from '@omni2fa/core';

export interface IOmni2FaProviderProps {
    /** Omni2FA instance built via <c>createOmni2Fa(...)</c>. Host owns its lifecycle (call <c>value.dispose()</c> on teardown). */
    value: IOmni2Fa;
    children: ReactNode;
}
