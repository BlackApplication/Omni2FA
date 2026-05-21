import type { ClientCallResult } from './ClientCallResult';
import type { ClientCallError } from './ClientCallError';

/** Outcome of an HTTP call — either success carrying a typed value or a failure carrying a stable error code. */
export type ClientCall<T> = ClientCallResult<T> | ClientCallError;
