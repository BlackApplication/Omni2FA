/** Failure branch of <c>ClientCall&lt;T&gt;</c>. Carries the stable error code and HTTP status. */
export interface ClientCallError {
    ok: false;
    code: string;
    message: string;
    httpStatus: number;
    details?: Record<string, unknown> | null;
}
