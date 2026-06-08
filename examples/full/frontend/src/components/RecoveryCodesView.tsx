import { useState } from 'react';
import { Alert, Box, Button, Stack, Typography } from '@mui/material';

/**
 * Shows one-time recovery codes with copy/download. Shown once at generation — the codes are not
 * retrievable later. When @omni2fa/react-mui ships (v0.5), use its styled equivalent instead.
 */
export function RecoveryCodesView({ codes }: { codes: string[] }) {
    const [copied, setCopied] = useState(false);

    function copy() {
        void navigator.clipboard.writeText(codes.join('\n')).then(() => {
            setCopied(true);
            setTimeout(() => setCopied(false), 2000);
        });
    }

    function download() {
        const blob = new Blob([codes.join('\n')], { type: 'text/plain' });
        const url = URL.createObjectURL(blob);
        const a = document.createElement('a');
        a.href = url;
        a.download = 'recovery-codes.txt';
        a.click();
        URL.revokeObjectURL(url);
    }

    return (
        <Stack spacing={1.5}>
            <Alert severity="warning">Save these recovery codes now. Each works once and they won't be shown again.</Alert>
            <Box
                sx={{
                    display: 'grid',
                    gridTemplateColumns: '1fr 1fr',
                    gap: 0.5,
                    fontFamily: 'monospace',
                    p: 1.5,
                    bgcolor: 'action.hover',
                    borderRadius: 1,
                }}
            >
                {codes.map((c) => (
                    <Typography key={c} variant="body2" fontFamily="monospace">{c}</Typography>
                ))}
            </Box>
            <Stack direction="row" spacing={1}>
                <Button size="small" onClick={copy}>{copied ? 'Copied!' : 'Copy'}</Button>
                <Button size="small" onClick={download}>Download</Button>
            </Stack>
        </Stack>
    );
}
