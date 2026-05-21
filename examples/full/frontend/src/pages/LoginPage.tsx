import { useState } from 'react';
import { Alert, Box, Button, Container, Paper, Stack, TextField, Typography, Link as MuiLink } from '@mui/material';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { authClient } from '../api/authClient';
import { useAuth } from '../auth/useAuth';
import { omni } from '../omni2fa';

export function LoginPage() {
    const navigate = useNavigate();
    const { setSession } = useAuth();
    const [email, setEmail] = useState('');
    const [password, setPassword] = useState('');
    const [error, setError] = useState<string | null>(null);
    const [loading, setLoading] = useState(false);

    async function submit(e: React.FormEvent) {
        e.preventDefault();
        setError(null);
        setLoading(true);
        try {
            const outcome = await authClient.login(email, password);
            if (outcome.kind === 0) {
                setSession(outcome.session);
                navigate('/profile');
            } else {
                omni.client.setPreAuthToken(outcome.challenge.preAuthToken);
                navigate('/2fa', { state: { availableMethods: outcome.challenge.availableMethods } });
            }
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Login failed');
        } finally {
            setLoading(false);
        }
    }

    return (
        <Container maxWidth="xs" sx={{ pt: 10 }}>
            <Paper elevation={2} sx={{ p: 4 }}>
                <Stack spacing={3} component="form" onSubmit={submit}>
                    <Typography variant="h5">Sign in</Typography>
                    {error && <Alert severity="error">{error}</Alert>}
                    <TextField label="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
                    <TextField label="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
                    <Button type="submit" variant="contained" disabled={loading}>{loading ? 'Signing in…' : 'Sign in'}</Button>
                    <Box textAlign="center">
                        <MuiLink component={RouterLink} to="/register">Need an account? Register</MuiLink>
                    </Box>
                </Stack>
            </Paper>
        </Container>
    );
}
