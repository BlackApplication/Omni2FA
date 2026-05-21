import { createTheme } from '@mui/material/styles';

/** Demo theme — teal/amber, light surfaces. Swap freely; the headless hooks don't care. */
export const theme = createTheme({
    palette: {
        mode: 'light',
        primary: { main: '#0d9488' },
        secondary: { main: '#f59e0b' },
        background: { default: '#f5f7f9', paper: '#ffffff' },
    },
    shape: { borderRadius: 10 },
    typography: {
        fontFamily: '"Inter", "Segoe UI", system-ui, sans-serif',
        h5: { fontWeight: 600 },
        h6: { fontWeight: 600 },
    },
});
