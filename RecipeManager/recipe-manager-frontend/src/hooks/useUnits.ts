import { useContext } from 'react';
import { UnitsContext } from '@/contexts/UnitsContext';

export function useUnits() {
  const ctx = useContext(UnitsContext);
  if (!ctx) {
    throw new Error('useUnits must be used within a UnitsProvider');
  }
  return ctx;
}
