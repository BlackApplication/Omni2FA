import type { WebAuthnEnrollmentContext } from '@omni2fa/core';

export type WebAuthnEnrollmentStatus = 'idle' | 'starting' | 'registering' | 'enrolled' | 'failed';

/** Shape returned by <c>useWebAuthnEnrollment</c>. */
export interface IUseWebAuthnEnrollmentResult {
    /** Current state node name. */
    status: WebAuthnEnrollmentStatus;
    /** Snapshot context — fields populated depending on <c>status</c>. */
    context: WebAuthnEnrollmentContext;
    /** Begin enrollment — issues options and immediately runs the browser ceremony. Optional credential label. */
    start: (name?: string) => void;
    /** Retry the whole ceremony after a failure (e.g. the user dismissed the prompt). */
    retry: () => void;
    /** Cancel/restart — wipes context and returns to <c>idle</c>. */
    reset: () => void;
}
