// src/contexts/UnitsContext.ts
import { createContext } from 'react';
import type { UnitsContextType } from '@/types';

export const UnitsContext = createContext<UnitsContextType | undefined>(undefined);
