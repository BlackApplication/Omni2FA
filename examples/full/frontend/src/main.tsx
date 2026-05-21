import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import { CssBaseline, ThemeProvider } from '@mui/material';
import { BrowserRouter } from 'react-router-dom';
import { Omni2FaProvider } from '@omni2fa/react';
import { App } from './App';
import { AuthProvider } from './auth/AuthContext';
import { omni } from './omni2fa';
import { theme } from './theme';

const rootElement = document.getElementById('root');
if (!rootElement) throw new Error('No #root element');

createRoot(rootElement).render(
    <StrictMode>
        <ThemeProvider theme={theme}>
            <CssBaseline />
            <Omni2FaProvider value={omni}>
                <AuthProvider>
                    <BrowserRouter>
                        <App />
                    </BrowserRouter>
                </AuthProvider>
            </Omni2FaProvider>
        </ThemeProvider>
    </StrictMode>,
);
