import { useContext } from 'react';
import type { IOmni2Fa } from '@omni2fa/core';
import { Omni2FaContext } from '../context/Omni2FaContext';

/** Read the ambient Omni2FA instance. Throws if no <c>Omni2FaProvider</c> is mounted above. */
export function useOmni2Fa(): IOmni2Fa {
    const omni = useContext(Omni2FaContext);
    if (omni === null) {
        throw new Error('useOmni2Fa must be used within an <Omni2FaProvider>.');
    }
    return omni;
}
