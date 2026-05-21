import { resolve } from 'node:path';
import { defineConfig } from 'vite';
import dts from 'vite-plugin-dts';

export default defineConfig({
    build: {
        lib: {
            entry: resolve(__dirname, 'src/index.ts'),
            name: 'Omni2FaCore',
            fileName: (format) => (format === 'es' ? 'index.js' : 'index.cjs'),
            formats: ['es', 'cjs'],
        },
        sourcemap: true,
        rollupOptions: {
            external: ['openapi-fetch', 'xstate'],
        },
        target: 'es2022',
    },
    plugins: [
        dts({
            entryRoot: 'src',
            include: ['src/**/*.ts'],
            insertTypesEntry: true,
            rollupTypes: false,
        }),
    ],
});
