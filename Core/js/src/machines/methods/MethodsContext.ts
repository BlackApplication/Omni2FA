import type { TwoFactorMethodDto } from '../../types/dtos';

/** Snapshot context of <see cref="createMethodsMachine"/>. */
export interface MethodsContext {
    items: TwoFactorMethodDto[];
    errorCode: string | null;
    errorMessage: string | null;
}
