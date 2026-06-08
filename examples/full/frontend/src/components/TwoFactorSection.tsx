import { useState } from 'react';
import { Alert, Box, Button, Card, CardContent, IconButton, List, ListItem, ListItemText, Stack, Typography } from '@mui/material';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import { useMethods } from '@omni2fa/react';
import { AddTotpDialog } from './AddTotpDialog';
import { AddEmailDialog } from './AddEmailDialog';

/**
 * Profile-page 2FA card — list of enrolled methods + actions.
 * TOTP and Email are wired; WebAuthn lands in v0.3.
 */
export function TwoFactorSection() {
    const { items, status, errorMessage, remove } = useMethods();
    const [totpDialogOpen, setTotpDialogOpen] = useState(false);
    const [emailDialogOpen, setEmailDialogOpen] = useState(false);
    const hasTotp = items.some((m) => m.type === 'Totp');
    const hasEmail = items.some((m) => m.type === 'Email');

    return (
        <Card>
            <CardContent>
                <Stack spacing={2}>
                    <Stack direction="row" alignItems="center" justifyContent="space-between">
                        <Typography variant="h6">Two-factor authentication</Typography>
                        <Stack direction="row" spacing={1}>
                            <Button variant="contained" size="small" disabled={hasTotp} onClick={() => setTotpDialogOpen(true)}>
                                Add TOTP
                            </Button>
                            <Button variant="outlined" size="small" disabled={hasEmail} onClick={() => setEmailDialogOpen(true)}>
                                Add Email
                            </Button>
                        </Stack>
                    </Stack>

                    {errorMessage && <Alert severity="error">{errorMessage}</Alert>}

                    {status === 'loading' && <Typography color="text.secondary">Loading…</Typography>}

                    {status !== 'loading' && items.length === 0 && (
                        <Typography color="text.secondary">No 2FA methods enrolled yet. Add one to protect your account.</Typography>
                    )}

                    {items.length > 0 && (
                        <List dense>
                            {items.map((m) => (
                                <ListItem
                                    key={m.id}
                                    secondaryAction={
                                        <IconButton edge="end" onClick={() => remove(m.id)} aria-label="Remove method">
                                            <DeleteOutlineIcon />
                                        </IconButton>
                                    }
                                >
                                    <ListItemText
                                        primary={m.name ?? m.type}
                                        secondary={`${m.type} · enrolled ${new Date(m.createdAt).toLocaleDateString()}`}
                                    />
                                </ListItem>
                            ))}
                        </List>
                    )}
                </Stack>
            </CardContent>
            <Box>
                <AddTotpDialog open={totpDialogOpen} onClose={() => setTotpDialogOpen(false)} />
                <AddEmailDialog open={emailDialogOpen} onClose={() => setEmailDialogOpen(false)} />
            </Box>
        </Card>
    );
}
