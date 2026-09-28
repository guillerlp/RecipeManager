// src/types/units.ts

/** How quantities are shown. Persisted to localStorage under 'units'. Never sent to the server (ADR-022). */
export type UnitSystem = 'asWritten' | 'metric' | 'imperial';

export interface UnitsContextType {
    unitSystem: UnitSystem;
    setUnitSystem: (unitSystem: UnitSystem) => void;
}
