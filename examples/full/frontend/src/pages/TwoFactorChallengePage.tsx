import { useEffect, useState } from 'react';
import { Alert, Box, Button, Container, MenuItem, Paper, Stack, TextField, Typography } from '@mui/material';
import { useLocation, useNavigate } from 'react-router-dom';
import { useChallenge } from '@omni2fa/react';
import { authClient } from '../api/authClient';
import { useAuth } from '../auth/useAuth';
import { omni } from '../omni2fa';

interface TwoFactorChallengeNavState {
    availableMethods?: Array<{ id: string; type: string; name: string | null }>;
}

export function TwoFactorChallengePage() {
    const navigate = useNavigate();
    const { setSession } = useAuth();
    const { status, context, pick, submit, resend, useRecoveryCode, reset } = useChallenge();
    const location = useLocation();
    const available = (location.state as TwoFactorChallengeNavState | null)?.availableMethods ?? [];
    const [selectedMethodId, setSelectedMethodId] = useState(available[0]?.id ?? '');
    const [code, setCode] = useState('');
    const [recoveryMode, setRecoveryMode] = useState(false);
    const [recoveryCode, setRecoveryCode] = useState('');
    const [finalizingError, setFinalizingError] = useState<string | null>(null);

    // Once verify succeeds — call host's finalize to get the session JWT.
    useEffect(() => {
        if (status !== 'verified' || !context.verifiedToken) return;
        let cancelled = false;
        (async () => {
            try {
                // The verified-handoff token is the proof the server re-validates.
                const session = await authClient.finalize(context.verifiedToken);
                if (cancelled) return;
                setSession(session);
                omni.client.setPreAuthToken(null);
                reset();
                navigate('/profile');
            } catch (err) {
                if (!cancelled) setFinalizingError(err instanceof Error ? err.message : 'Failed to finalize session');
            }
        })();
        return () => {
            cancelled = true;
        };
    }, [status, context.verifiedToken, navigate, reset, setSession]);

    if (available.length === 0) {
        return (
            <Container maxWidth="xs" sx={{ pt: 10 }}>
                <Alert severity="warning">No 2FA methods available. Go back and sign in again.</Alert>
                <Box mt={2}>
                    <Button onClick={() => navigate('/login')}>Back to sign in</Button>
                </Box>
            </Container>
        );
    }

    function startChallenge() {
        if (!selectedMethodId) return;
        pick(selectedMethodId);
    }

    function verifyCode(e: React.FormEvent) {
        e.preventDefault();
        submit(code);
    }

    function verifyRecovery(e: React.FormEvent) {
        e.preventDefault();
        useRecoveryCode(recoveryCode.trim());
    }

    return (
        <Container maxWidth="xs" sx={{ pt: 10 }}>
            <Paper elevation={2} sx={{ p: 4 }}>
                <Stack spacing={3}>
                    <Typography variant="h5">Two-factor verification</Typography>
                    {finalizingError && <Alert severity="error">{finalizingError}</Alert>}
                    {context.errorMessage && <Alert severity="error">{context.errorMessage}</Alert>}

                    {(status === 'idle' || status === 'failed') && !recoveryMode && (
                        <Stack spacing={2}>
                            <TextField select label="Method" value={selectedMethodId} onChange={(e) => setSelectedMethodId(e.target.value)}>
                                {available.map((m) => (
                                    <MenuItem key={m.id} value={m.id}>{m.name ?? m.type}</MenuItem>
                                ))}
                            </TextField>
                            <Button variant="contained" onClick={startChallenge}>Continue</Button>
                            <Button size="small" onClick={() => setRecoveryMode(true)}>Use a recovery code instead</Button>
                        </Stack>
                    )}

                    {(status === 'idle' || status === 'failed' || status === 'verifyingRecovery') && recoveryMode && (
                        <Stack component="form" spacing={2} onSubmit={verifyRecovery}>
                            <Typography variant="body2" color="text.secondary">Enter one of your saved recovery codes.</Typography>
                            <TextField label="Recovery code" value={recoveryCode} onChange={(e) => setRecoveryCode(e.target.value)} placeholder="XXXX-XXXX-XX" autoFocus />
                            <Button type="submit" variant="contained" disabled={status === 'verifyingRecovery' || recoveryCode.trim().length === 0}>
                                {status === 'verifyingRecovery' ? 'Verifying…' : 'Verify'}
                            </Button>
                            <Button size="small" onClick={() => setRecoveryMode(false)}>Back to methods</Button>
                        </Stack>
                    )}

                    {status === 'starting' && <Typography>Preparing…</Typography>}

                    {status === 'asserting' && <Typography>Follow your browser's prompt to verify with your passkey…</Typography>}

                    {(status === 'awaitingCode' || status === 'resending' || status === 'verifying') && (
                        <Stack component="form" spacing={2} onSubmit={verifyCode}>
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
                                {status === 'verifying' ? 'Verifying…' : 'Verify'}
                            </Button>
                            {context.methodType === 'Email' && (
                                <Button onClick={resend} disabled={status !== 'awaitingCode'}>Resend code</Button>
                            )}
                        </Stack>
                    )}

                    {status === 'verified' && <Typography color="success.main">Verified — signing you in…</Typography>}
                </Stack>
            </Paper>
        </Container>
    );
}
