import { useState } from 'react';
import { Alert, Box, Button, Container, Paper, Stack, TextField, Typography, Link as MuiLink } from '@mui/material';
import { Link as RouterLink, useNavigate } from 'react-router-dom';
import { authClient } from '../api/authClient';
import { useAuth } from '../auth/useAuth';

export function RegisterPage() {
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
            const session = await authClient.register(email, password);
            setSession(session);
            navigate('/profile');
        } catch (err) {
            setError(err instanceof Error ? err.message : 'Registration failed');
        } finally {
            setLoading(false);
        }
    }

    return (
        <Container maxWidth="xs" sx={{ pt: 10 }}>
            <Paper elevation={2} sx={{ p: 4 }}>
                <Stack spacing={3} component="form" onSubmit={submit}>
                    <Typography variant="h5">Create account</Typography>
                    {error && <Alert severity="error">{error}</Alert>}
                    <TextField label="Email" type="email" value={email} onChange={(e) => setEmail(e.target.value)} required autoFocus />
                    <TextField label="Password" type="password" value={password} onChange={(e) => setPassword(e.target.value)} required />
                    <Button type="submit" variant="contained" disabled={loading}>{loading ? 'Registering…' : 'Register'}</Button>
                    <Box textAlign="center">
                        <MuiLink component={RouterLink} to="/login">Already have an account? Sign in</MuiLink>
                    </Box>
                </Stack>
            </Paper>
        </Container>
    );
}
