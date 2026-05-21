import { Omni2FaContext } from '../context/Omni2FaContext';
import type { IOmni2FaProviderProps } from '../Interfaces/IOmni2FaProviderProps';

/** Root provider — wraps the app and makes the Omni2FA instance available to all descendant hooks. */
export function Omni2FaProvider({ value, children }: IOmni2FaProviderProps) {
    return <Omni2FaContext.Provider value={value}>{children}</Omni2FaContext.Provider>;
}
