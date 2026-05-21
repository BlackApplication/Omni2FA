import { createContext } from 'react';
import type { IOmni2Fa } from '@omni2fa/core';

/** React context carrying the Omni2FA core instance to descendants. */
export const Omni2FaContext = createContext<IOmni2Fa | null>(null);
