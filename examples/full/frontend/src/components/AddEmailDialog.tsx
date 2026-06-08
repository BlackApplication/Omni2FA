import { useEffect, useState } from 'react';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField, Typography } from '@mui/material';
import { useEmailEnrollment, useMethods } from '@omni2fa/react';
import { RecoveryCodesView } from './RecoveryCodesView';

/**
 * When @omni2fa/react-mui v0.5 ships, replace this whole component with the drop-in
 * <EnrollEmailDialog /> from the styled package — same behavior, no maintenance burden.
 */
export function AddEmailDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
    const { status, context, start, submit, resend, reset } = useEmailEnrollment();
    const { load } = useMethods();
    const [email, setEmail] = useState('');
    const [code, setCode] = useState('');
    const [name, setName] = useState('');

    useEffect(() => {
        if (status === 'enrolled') {
            load();
            if (context.recoveryCodes) {
                return undefined;
            }
            const t = setTimeout(() => {
                reset();
                setEmail('');
                setCode('');
                setName('');
                onClose();
            }, 1200);
            return () => clearTimeout(t);
        }
        return undefined;
    }, [status, context.recoveryCodes, load, reset, onClose]);

    function handleStart(e: React.FormEvent) {
        e.preventDefault();
        if (email) start(email);
    }

    function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        submit(code, name || undefined);
    }

    function handleClose() {
        reset();
        setEmail('');
        setCode('');
        setName('');
        onClose();
    }

    const collecting = status === 'idle' || status === 'starting' || status === 'failed';
    const verifying = status === 'awaitingCode' || status === 'resending' || status === 'confirming';

    return (
        <Dialog open={open} onClose={handleClose} fullWidth maxWidth="xs">
            <DialogTitle>Add email OTP</DialogTitle>
            <DialogContent>
                <Stack spacing={2} sx={{ mt: 1 }}>
                    {context.errorMessage && <Alert severity="error">{context.errorMessage}</Alert>}

                    {collecting && (
                        <Stack component="form" spacing={2} onSubmit={handleStart}>
                            <Typography variant="body2" color="text.secondary">
                                We'll email a one-time code to confirm this address.
                            </Typography>
                            <TextField
                                label="Email address"
                                type="email"
                                value={email}
                                onChange={(e) => setEmail(e.target.value)}
                                autoFocus
                            />
                            <Button type="submit" variant="contained" disabled={status === 'starting' || !email}>
                                {status === 'starting' ? 'Sending…' : 'Send code'}
                            </Button>
                        </Stack>
                    )}

                    {verifying && (
                        <Stack component="form" spacing={2} onSubmit={handleSubmit}>
                            <Typography variant="body2" color="text.secondary">
                                Enter the 6-digit code we emailed to <strong>{context.email}</strong>.
                            </Typography>
                            <TextField
                                label="Name (optional)"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                placeholder="e.g. Work email"
                            />
                            <TextField
                                label="6-digit code"
                                value={code}
                                onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                                inputProps={{ inputMode: 'numeric', autoComplete: 'one-time-code' }}
                                autoFocus
                            />
                            <Button type="submit" variant="contained" disabled={status !== 'awaitingCode' || code.length !== 6}>
                                {status === 'confirming' ? 'Confirming…' : 'Confirm'}
                            </Button>
                            <Button onClick={resend} disabled={status !== 'awaitingCode'}>Resend code</Button>
                        </Stack>
                    )}

                    {status === 'enrolled' && context.recoveryCodes && (
                        <Stack spacing={2}>
                            <Alert severity="success">Email OTP enrolled.</Alert>
                            <RecoveryCodesView codes={context.recoveryCodes} />
                            <Button variant="contained" onClick={handleClose}>I saved my codes</Button>
                        </Stack>
                    )}

                    {status === 'enrolled' && !context.recoveryCodes && <Alert severity="success">Email OTP enrolled. Closing…</Alert>}
                </Stack>
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose}>Close</Button>
            </DialogActions>
        </Dialog>
    );
}
