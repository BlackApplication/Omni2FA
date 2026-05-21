/**
 * Error thrown by core machines when an HTTP call fails. Carries the stable error code,
 * HTTP status, and optional structured details. Adapters read it from xstate's <c>onError</c>
 * <c>event.error</c>.
 */
export class Omni2FaApiError extends Error {
    readonly code: string;
    readonly httpStatus: number;
    readonly details: Record<string, unknown> | null;

    constructor(code: string, message: string, httpStatus: number, details: Record<string, unknown> | null = null) {
        super(message);
        this.name = 'Omni2FaApiError';
        this.code = code;
        this.httpStatus = httpStatus;
        this.details = details;
    }
}
