import { useEffect, useState } from 'react';
import { Alert, Button, Dialog, DialogActions, DialogContent, DialogTitle, Stack, TextField, Typography } from '@mui/material';
import { useWebAuthnEnrollment, useMethods } from '@omni2fa/react';
import { RecoveryCodesView } from './RecoveryCodesView';

/**
 * When @omni2fa/react-mui v0.5 ships, replace this whole component with the drop-in
 * <EnrollWebAuthnDialog /> from the styled package — same behavior, no maintenance burden.
 */
export function AddWebAuthnDialog({ open, onClose }: { open: boolean; onClose: () => void }) {
    const { status, context, start, retry, reset } = useWebAuthnEnrollment();
    const { load } = useMethods();
    const [name, setName] = useState('');

    useEffect(() => {
        if (status === 'enrolled') {
            load();
            if (context.recoveryCodes) {
                return undefined;
            }
            const t = setTimeout(() => {
                reset();
                setName('');
                onClose();
            }, 1200);
            return () => clearTimeout(t);
        }
        return undefined;
    }, [status, context.recoveryCodes, load, reset, onClose]);

    function handleClose() {
        reset();
        setName('');
        onClose();
    }

    const busy = status === 'starting' || status === 'registering';

    return (
        <Dialog open={open} onClose={handleClose} fullWidth maxWidth="xs">
            <DialogTitle>Add passkey / security key</DialogTitle>
            <DialogContent>
                <Stack spacing={2} sx={{ mt: 1 }}>
                    {context.errorMessage && status === 'failed' && <Alert severity="error">{context.errorMessage}</Alert>}

                    {(status === 'idle' || status === 'failed') && (
                        <Stack spacing={2}>
                            <Typography variant="body2" color="text.secondary">
                                Use a passkey (Face ID, Touch ID, Windows Hello) or a hardware security key.
                                Your browser will prompt you.
                            </Typography>
                            <TextField
                                label="Name (optional)"
                                value={name}
                                onChange={(e) => setName(e.target.value)}
                                placeholder="e.g. YubiKey 5, MacBook Touch ID"
                            />
                            <Button variant="contained" onClick={() => (status === 'failed' ? retry() : start(name || undefined))}>
                                {status === 'failed' ? 'Try again' : 'Create passkey'}
                            </Button>
                        </Stack>
                    )}

                    {busy && <Typography>Follow your browser's prompt to finish…</Typography>}

                    {status === 'enrolled' && context.recoveryCodes && (
                        <Stack spacing={2}>
                            <Alert severity="success">Passkey enrolled.</Alert>
                            <RecoveryCodesView codes={context.recoveryCodes} />
                            <Button variant="contained" onClick={handleClose}>I saved my codes</Button>
                        </Stack>
                    )}

                    {status === 'enrolled' && !context.recoveryCodes && <Alert severity="success">Passkey enrolled. Closing…</Alert>}
                </Stack>
            </DialogContent>
            <DialogActions>
                <Button onClick={handleClose}>Close</Button>
            </DialogActions>
        </Dialog>
    );
}
