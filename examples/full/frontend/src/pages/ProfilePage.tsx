import { AppBar, Box, Button, Card, CardContent, Container, Stack, Toolbar, Typography } from '@mui/material';
import { useAuth } from '../auth/useAuth';
import { TwoFactorSection } from '../components/TwoFactorSection';
import { ChangePasswordCard } from '../components/ChangePasswordCard';

export function ProfilePage() {
    const { session, logout } = useAuth();

    return (
        <Box>
            <AppBar position="static" color="default" elevation={1}>
                <Toolbar>
                    <Typography variant="h6" sx={{ flexGrow: 1 }}>Omni2FA Example</Typography>
                    <Typography variant="body2" sx={{ mr: 2 }}>{session?.email}</Typography>
                    <Button color="inherit" onClick={logout}>Sign out</Button>
                </Toolbar>
            </AppBar>

            <Container maxWidth="sm" sx={{ pt: 4 }}>
                <Stack spacing={3}>
                    <Card>
                        <CardContent>
                            <Typography variant="h5" gutterBottom>Profile</Typography>
                            <Typography color="text.secondary">{session?.email}</Typography>
                        </CardContent>
                    </Card>

                    <TwoFactorSection />

                    <ChangePasswordCard />
                </Stack>
            </Container>
        </Box>
    );
}
