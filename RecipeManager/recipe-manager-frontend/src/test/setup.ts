import { cleanup } from '@testing-library/react';
import { afterEach } from 'vitest';

// RTL unmounts rendered trees automatically only when Vitest globals are on. They are off
// (ADR-018), so without this every test would leak its DOM into the next.
afterEach(() => {
  cleanup();
});
