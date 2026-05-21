/** Success branch of <c>ClientCall&lt;T&gt;</c>. */
export interface ClientCallResult<T> {
    ok: true;
    value: T;
}
