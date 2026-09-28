import React, { useEffect, useMemo, useState } from 'react';
import type { UnitSystem } from '@/types';
import { UnitsContext } from './UnitsContext';

const STORAGE_KEY = 'units';

const isUnitSystem = (value: unknown): value is UnitSystem =>
  value === 'asWritten' || value === 'metric' || value === 'imperial';

const readUnitSystem = (): UnitSystem => {
  try {
    const saved = window.localStorage.getItem(STORAGE_KEY);
    return isUnitSystem(saved) ? saved : 'asWritten';
  } catch {
    return 'asWritten';
  }
};

// Context, not page state: Settings writes it and the detail screen reads it — two routes apart.
export const UnitsProvider: React.FC<React.PropsWithChildren> = ({ children }) => {
  const [unitSystem, setUnitSystem] = useState<UnitSystem>(readUnitSystem);

  useEffect(() => {
    try {
      window.localStorage.setItem(STORAGE_KEY, unitSystem);
    } catch {
      // Storage disabled: the choice still holds for this session.
    }
  }, [unitSystem]);

  const value = useMemo(() => ({ unitSystem, setUnitSystem }), [unitSystem]);

  return <UnitsContext.Provider value={value}>{children}</UnitsContext.Provider>;
};
