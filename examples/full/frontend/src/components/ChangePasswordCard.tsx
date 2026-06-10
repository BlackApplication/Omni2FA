import { useEffect, useState } from 'react';
import {
    Alert,
    Button,
    Card,
    CardContent,
    Dialog,
    DialogContent,
    DialogTitle,
    MenuItem,
    Stack,
    TextField,
    Typography,
} from '@mui/material';
import { STEP_UP_HEADER, Omni2FaErrorCodes } from '@omni2fa/core';
import { useStepUp } from '@omni2fa/react';
import { useAuth } from '../auth/useAuth';

/**
 * Change-password card demonstrating step-up 2FA. The `/user/change-password` endpoint is decorated
 * with `[RequireTwoFactor]` on the server. On `403 STEP_UP_REQUIRED` we prompt for 2FA via
 * `confirmTwoFactor` and retry the request with the step-up token in the `X-Omni2FA-StepUp` header.
 * Users without 2FA are never prompted. (An app with a central fetch/axios interceptor would do this
 * once there, instead of per call.)
 */
export function ChangePasswordCard() {
    const { authFetch } = useAuth();
    const { confirmTwoFactor, active, methods, status, context, pick, submit, resend, cancel } = useStepUp();

    const [currentPassword, setCurrentPassword] = useState('');
    const [newPassword, setNewPassword] = useState('');
    const [result, setResult] = useState<{ severity: 'success' | 'error'; text: string } | null>(null);

    // Step-up dialog local state.
    const [selectedMethodId, setSelectedMethodId] = useState('');
    const [code, setCode] = useState('');
    useEffect(() => {
        if (active) {
            setSelectedMethodId(methods[0]?.id ?? '');
            setCode('');
        }
    }, [active, methods]);

    // The request itself — `extra` lets the retry add the step-up header. The session rides however
    // the app carries it (here a Bearer token via authFetch; a cookie app needs no change here).
    const send = (extra: Record<string, string> = {}) =>
        authFetch('/user/change-password', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json', ...extra },
            body: JSON.stringify({ currentPassword, newPassword }),
        });

    async function onSubmit(e: React.FormEvent) {
        e.preventDefault();
        setResult(null);

        let res = await send();
        if (res.status === 403) {
            const body = (await res.clone().json().catch(() => null)) as { code?: string; details?: { availableMethods?: typeof methods } } | null;
            if (body?.code === Omni2FaErrorCodes.StepUpRequired) {
                const token = await confirmTwoFactor(body.details?.availableMethods ?? []);
                if (!token) {
                    return; // user dismissed the 2FA prompt
                }
                res = await send({ [STEP_UP_HEADER]: token });
            }
        }

        if (res.ok) {
            setResult({ severity: 'success', text: 'Password changed.' });
            setCurrentPassword('');
            setNewPassword('');
        } else {
            setResult({ severity: 'error', text: `Could not change password (HTTP ${res.status}).` });
        }
    }

    return (
        <Card>
            <CardContent>
                <Stack component="form" spacing={2} onSubmit={onSubmit}>
                    <Typography variant="h6">Change password</Typography>
                    {result && <Alert severity={result.severity}>{result.text}</Alert>}
                    <TextField
                        type="password"
                        label="Current password"
                        value={currentPassword}
                        onChange={(e) => setCurrentPassword(e.target.value)}
                        autoComplete="current-password"
                    />
                    <TextField
                        type="password"
                        label="New password"
                        value={newPassword}
                        onChange={(e) => setNewPassword(e.target.value)}
                        autoComplete="new-password"
                    />
                    <Button type="submit" variant="contained" disabled={currentPassword.length === 0 || newPassword.length === 0}>
                        Change password
                    </Button>
                </Stack>
            </CardContent>

            <Dialog open={active} onClose={cancel} fullWidth maxWidth="xs">
                <DialogTitle>Confirm it's you</DialogTitle>
                <DialogContent>
                    <Stack spacing={2} sx={{ mt: 1 }}>
                        <Typography variant="body2" color="text.secondary">
                            Confirm two-factor authentication to change your password.
                        </Typography>
                        {context.errorMessage && <Alert severity="error">{context.errorMessage}</Alert>}

                        {(status === 'idle' || status === 'failed') && (
                            <>
                                <TextField select label="Method" value={selectedMethodId} onChange={(e) => setSelectedMethodId(e.target.value)}>
                                    {methods.map((m) => (
                                        <MenuItem key={m.id} value={m.id}>{m.name ?? m.type}</MenuItem>
                                    ))}
                                </TextField>
                                <Button variant="contained" onClick={() => selectedMethodId && pick(selectedMethodId)}>Continue</Button>
                            </>
                        )}

                        {status === 'starting' && <Typography>Preparing…</Typography>}
                        {status === 'asserting' && <Typography>Follow your browser's prompt to verify with your passkey…</Typography>}

                        {(status === 'awaitingCode' || status === 'resending' || status === 'verifying') && (
                            <Stack
                                component="form"
                                spacing={2}
                                onSubmit={(e) => {
                                    e.preventDefault();
                                    submit(code);
                                }}
                            >
                                <Typography variant="body2" color="text.secondary">
                                    {context.methodType === 'Email'
                                        ? 'Enter the 6-digit code we emailed you.'
                                        : 'Open your authenticator app and enter the 6-digit code.'}
                                </Typography>
                                <TextField
                                    label="Code"
                                    value={code}
                                    onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                                    inputProps={{ inputMode: 'numeric', autoComplete: 'one-time-code' }}
                                    autoFocus
                                />
                                <Button type="submit" variant="contained" disabled={status !== 'awaitingCode' || code.length !== 6}>
                                    {status === 'verifying' ? 'Verifying…' : 'Confirm'}
                                </Button>
                                {context.methodType === 'Email' && (
                                    <Button onClick={resend} disabled={status !== 'awaitingCode'}>Resend code</Button>
                                )}
                            </Stack>
                        )}

                        <Button size="small" onClick={cancel}>Cancel</Button>
                    </Stack>
                </DialogContent>
            </Dialog>
        </Card>
    );
}
