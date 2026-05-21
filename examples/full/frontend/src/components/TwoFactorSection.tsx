import { useState } from 'react';
import { Alert, Box, Button, Card, CardContent, IconButton, List, ListItem, ListItemText, Stack, Typography } from '@mui/material';
import DeleteOutlineIcon from '@mui/icons-material/DeleteOutline';
import { useMethods } from '@omni2fa/react';
import { AddTotpDialog } from './AddTotpDialog';

/**
 * Profile-page 2FA card — list of enrolled methods + actions.
 * For v0.1 only TOTP is wired; Email + WebAuthn lanes land in v0.2/v0.3.
 */
export function TwoFactorSection() {
    const { items, status, errorMessage, remove } = useMethods();
    const [dialogOpen, setDialogOpen] = useState(false);
    const hasTotp = items.some((m) => m.type === 'Totp');

    return (
        <Card>
            <CardContent>
                <Stack spacing={2}>
                    <Stack direction="row" alignItems="center" justifyContent="space-between">
                        <Typography variant="h6">Two-factor authentication</Typography>
                        <Button variant="contained" size="small" disabled={hasTotp} onClick={() => setDialogOpen(true)}>
                            Add TOTP
                        </Button>
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
                <AddTotpDialog open={dialogOpen} onClose={() => setDialogOpen(false)} />
            </Box>
        </Card>
    );
}
