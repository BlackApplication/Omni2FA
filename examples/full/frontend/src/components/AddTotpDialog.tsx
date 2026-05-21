import { useEffect, useState } from 'react';
import { Alert, Box, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField, Typography } from '@mui/material';
import QRCode from 'react-qr-code';
import { useTotpEnrollment, useMethods } from '@omni2fa/react';

/**
 * When @omni2fa/react-mui v0.5 ships, replace this whole component with the drop-in
 * <EnrollTotpDialog /> from the styled package — same behavior, no maintenance burden.
 */
export function AddTotpDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
    const { status, context, start, submit, reset } = useTotpEnrollment();
    const { load } = useMethods();
    const [code, setCode] = useState('');
    const [name, setName] = useState('');

    useEffect(() => {
        if (open && status === 'idle') {
            start();
        }
    }, [open, status, start]);

    useEffect(() => {
        if (status === 'enrolled') {
            load();
            const t = setTimeout(() => {
                reset();
                setCode('');
                setName('');
                onClose();
            }, 1200);
            return () => clearTimeout(t);
        }
        return undefined;
    }, [status, load, reset, onClose]);

    function handleSubmit(e: React.FormEvent) {
        e.preventDefault();
        submit(code, name || undefined);
    }

    function handleClose() {
        reset();
        setCode('');
        setName('');
        onClose();
    }

    return (
        <Dialog open={open} onClose={handleClose} fullWidth maxWidth="xs">
            <DialogTitle>Add authenticator app</DialogTitle>
            <DialogContent>
                <Stack spacing={2} sx={{ mt: 1 }}>
                    {context.errorMessage && <Alert severity="error">{context.errorMessage}</Alert>}

                    {status === 'starting' && <Typography>Generating secret…</Typography>}

                    {(status === 'awaitingCode' || status === 'confirming') && context.otpAuthUri && (
                        <Stack component="form" spacing={2} onSubmit={handleSubmit}>
                            <Typography variant="body2" color="text.secondary">
                                Scan this QR with Google Authenticator, Authy, or 1Password.
                            </Typography>
                            <Box display="flex" justifyContent="center" sx={{ p: 2, bgcolor: '#fff' }}>
                                <QRCode value={context.otpAuthUri} size={180} />
                            </Box>
                            <Typography variant="caption" color="text.secondary" sx={{ wordBreak: 'break-all' }}>
                                Or enter the secret manually: <strong>{context.secret}</strong>
                            </Typography>
                            <TextField
                                label="Name (optional)"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                placeholder="e.g. Personal authenticator"
                            />
                            <TextField
                                label="6-digit code"
                                value={code}
                                onChange={(e) => setCode(e.target.value.replace(/\D/g, '').slice(0, 6))}
                                inputProps={{ inputMode: 'numeric', autoComplete: 'one-time-code' }}
                                autoFocus
                            />
                            <Button type="submit" variant="contained" disabled={status === 'confirming' || code.length !== 6}>
                                {status === 'confirming' ? 'Confirming…' : 'Confirm'}
                            </Button>
                        </Stack>
                    )}

                    {status === 'enrolled' && <Alert severity="success">TOTP enrolled. Closing…</Alert>}

                    {status === 'failed' && (
                        <Stack spacing={2}>
                            <Alert severity="error">Enrollment failed.</Alert>
                            <Button onClick={start} variant="outlined">Try again</Button>
                        </Stack>
                    )}
                </Stack>
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose}>Close</Button>
            </DialogActions>
        </Dialog>
    );
}
