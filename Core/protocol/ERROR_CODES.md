# Omni2FA — Error code catalogue

Every non-2xx response from an Omni2FA endpoint carries an `ErrorResponse`:

```json
{
  "code": "INVALID_CODE",
  "message": "The code you entered is invalid.",
  "details": null
}
```

The `code` is **machine-readable and stable** — frontends switch on it. The `message` is a human-readable fallback that may be localized by the host. Frontends should prefer their own copy/i18n keyed by `code` rather than displaying `message` raw.

This file is the **stable contract**. Adding a new code is a minor version bump; renaming or removing is a major bump.

---

## Codes — alphabetical

### `CHALLENGE_CONSUMED`
**HTTP:** 401
**Meaning:** The challenge tied to this pre-auth token has already been used (verified successfully or explicitly revoked).
**Frontend action:** Send user back to the login screen — they must re-enter their password to get a fresh pre-auth token.

### `CHALLENGE_NOT_FOUND`
**HTTP:** 404
**Meaning:** No active challenge exists for the given `methodId` under the current pre-auth token.
**Frontend action:** Call `/challenge/start` again before retrying `/verify`.

### `INVALID_CODE`
**HTTP:** 401
**Meaning:** The submitted 6-digit code (TOTP or Email OTP) doesn't validate.
**Frontend action:** Show inline error. Allow retry. Watch for `TOO_MANY_ATTEMPTS` on the next request.

### `TYPE_ALREADY_ENROLLED`
**HTTP:** 409
**Meaning:** Attempt to enroll a method type (`Totp` or `Email`) that the user already has. These types are unique per user.
**Frontend action:** Show the existing method in the methods list. Offer "Remove existing and re-enroll" if appropriate.

### `LAST_METHOD_PROTECTED`
**HTTP:** 409
**Meaning:** Attempt to remove the user's last active method while host has configured `AllowDisabling = false`.
**Frontend action:** Inform user 2FA is required by their organization. Offer to enroll an additional method before removing the existing one.

### `MAX_METHODS_REACHED`
**HTTP:** 409
**Meaning:** Attempt to enroll an additional WebAuthn credential beyond the configured per-user cap (default 3 — configurable via `MaxWebAuthnMethodsPerUser`).
**Frontend action:** Show the cap to the user. Offer to remove one before adding another.

### `METHOD_NOT_FOUND`
**HTTP:** 404
**Meaning:** The `methodId` does not exist, or does not belong to the current user.
**Frontend action:** Refresh the methods list — the local state is stale.

### `PREAUTH_EXPIRED`
**HTTP:** 401
**Meaning:** The pre-auth token has expired (default TTL: 3-5 minutes).
**Frontend action:** Send user back to the login screen for a fresh password verification.

### `PREAUTH_INVALID`
**HTTP:** 401
**Meaning:** Pre-auth token is missing, malformed, or signed by an unknown key.
**Frontend action:** Same as `PREAUTH_EXPIRED` — return to login.

### `RECOVERY_CODE_INVALID`
**HTTP:** 401
**Meaning:** Submitted recovery code doesn't match any of the user's hashed codes. Available from v0.4 onward.
**Frontend action:** Show error. Allow retry. Same rate-limit caveats as `INVALID_CODE`.

### `RECOVERY_CODE_USED`
**HTTP:** 401
**Meaning:** Recovery code matched a hash but the code was already marked as used. Available from v0.4 onward.
**Frontend action:** Show explanation. Recovery codes are one-time use — direct user to regenerate codes after current login.

### `STEP_UP_REQUIRED`
**HTTP:** 403
**Meaning:** A step-up-protected action was attempted, the user has at least one active 2FA method, and no valid, unused step-up token was presented. `details.availableMethods` lists the methods to confirm with; `details.stepUpPath` is where the step-up challenge lives. Users with no 2FA enrolled are not blocked.
**Frontend action:** Run a step-up challenge (`/stepup/start` → `/stepup/verify`), then retry the original request with the `X-Omni2FA-StepUp` header. The `useStepUp` hook's `confirmTwoFactor(methods)` runs the challenge and yields the single-use token; your own request layer detects the 403 and replays with the header.

### `TOO_MANY_ATTEMPTS`
**HTTP:** 429
**Meaning:** Rate limit exceeded. Default policy: 20 attempts per minute per IP on verify endpoints.
**Frontend action:** Show countdown via the `Retry-After` header. Disable the verify button until allowed. Do **not** auto-retry.

### `VALIDATION_FAILED`
**HTTP:** 400
**Meaning:** Request body failed validation (missing required fields, wrong types, malformed JSON, etc.).
**Frontend action:** Bug indicator — frontend produced an invalid request. Log; show generic error to user. `details` may carry a field-level breakdown.

### `WEBAUTHN_VERIFICATION_FAILED`
**HTTP:** 401
**Meaning:** WebAuthn assertion did not validate against the stored credential. Available from v0.3 onward.
**Frontend action:** Show error. Common cause is the user pressed the wrong key — allow retry.

---

## Code categories

| HTTP | Codes |
|------|-------|
| **400 Bad Request** | `VALIDATION_FAILED` |
| **401 Unauthorized** | `INVALID_CODE`, `PREAUTH_EXPIRED`, `PREAUTH_INVALID`, `CHALLENGE_CONSUMED`, `RECOVERY_CODE_INVALID`, `RECOVERY_CODE_USED`, `WEBAUTHN_VERIFICATION_FAILED` |
| **403 Forbidden** | `STEP_UP_REQUIRED` |
| **404 Not Found** | `CHALLENGE_NOT_FOUND`, `METHOD_NOT_FOUND` |
| **409 Conflict** | `TYPE_ALREADY_ENROLLED`, `LAST_METHOD_PROTECTED`, `MAX_METHODS_REACHED` |
| **429 Too Many Requests** | `TOO_MANY_ATTEMPTS` |

---

## How frontends consume

The recommended pattern in adapter packages (`@omni2fa/react`, future `@omni2fa/vue`, …):

```ts
const errorKeyMap: Record<string, string> = {
  INVALID_CODE: 'twoFactor.errors.invalidCode',
  PREAUTH_EXPIRED: 'twoFactor.errors.preauthExpired',
  TOO_MANY_ATTEMPTS: 'twoFactor.errors.tooManyAttempts',
  // ...
};

function showError(err: ErrorResponse, t: (key: string) => string) {
  const key = errorKeyMap[err.code] ?? 'common.errors.generic';
  return t(key);
}
```

This keeps the wire format machine-readable while UX strings live in the host's i18n system.
