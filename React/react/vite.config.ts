import { resolve } from 'node:path';
import { defineConfig } from 'vite';
import dts from 'vite-plugin-dts';

export default defineConfig({
    esbuild: {
        jsx: 'automatic',
    },
    build: {
        lib: {
            entry: resolve(__dirname, 'src/index.ts'),
            name: 'Omni2FaReact',
            fileName: (format) => (format === 'es' ? 'index.js' : 'index.cjs'),
            formats: ['es', 'cjs'],
        },
        sourcemap: true,
        rollupOptions: {
            external: ['react', 'react/jsx-runtime', '@omni2fa/core', '@xstate/react', 'xstate'],
        },
        target: 'es2022',
    },
    plugins: [
        dts({
            entryRoot: 'src',
            include: ['src/**/*.ts', 'src/**/*.tsx'],
            insertTypesEntry: true,
            rollupTypes: false,
        }),
    ],
});
