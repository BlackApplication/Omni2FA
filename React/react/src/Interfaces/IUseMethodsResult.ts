import type { TwoFactorMethodDto } from '@omni2fa/core';

export type MethodsStatus = 'idle' | 'loading' | 'ready' | 'removing' | 'failed';

/** Shape returned by <c>useMethods</c>. */
export interface IUseMethodsResult {
    status: MethodsStatus;
    /** Active 2FA methods. Empty until first successful load. */
    items: TwoFactorMethodDto[];
    /** Latest error code, if any. */
    errorCode: string | null;
    /** Latest error message, if any. */
    errorMessage: string | null;
    /** Trigger a fresh fetch. Idempotent — re-runs even if already loaded. */
    load: () => void;
    /** Remove a method by id. Optimistically removed from <c>items</c> on success. */
    remove: (methodId: string) => void;
    /** Wipe state and return to <c>idle</c>. */
    reset: () => void;
}
