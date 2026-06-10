import { useEffect, useState } from 'react';
import { Alert, Button, Dialog, DialogContent, DialogTitle, MenuItem, Stack, TextField, Typography } from '@mui/material';
import type { IUseStepUpResult } from '@omni2fa/react';

type StepUpDialogProps = Pick<IUseStepUpResult, 'active' | 'methods' | 'status' | 'context' | 'pick' | 'submit' | 'resend' | 'cancel'> & {
    description?: string;
};

/** Shared 2FA step-up prompt — renders the method picker / code form from a `useStepUp()` result. */
export function StepUpDialog({ active, methods, status, context, pick, submit, resend, cancel, description }: StepUpDialogProps) {
    const [selectedMethodId, setSelectedMethodId] = useState('');
    const [code, setCode] = useState('');
    useEffect(() => {
        if (active) {
            setSelectedMethodId(methods[0]?.id ?? '');
            setCode('');
        }
    }, [active, methods]);

    return (
        <Dialog open={active} onClose={cancel} fullWidth maxWidth="xs">
            <DialogTitle>Confirm it's you</DialogTitle>
            <DialogContent>
                <Stack spacing={2} sx={{ mt: 1 }}>
                    <Typography variant="body2" color="text.secondary">
                        {description ?? 'Confirm two-factor authentication to continue.'}
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
    );
}
