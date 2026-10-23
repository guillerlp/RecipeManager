// src/Shell.tsx
// The root route's elements (routes.tsx): the providers every page needs, and the app chrome most pages sit in.
// Their own file because fast refresh needs a file that exports only components.
import { Outlet } from 'react-router-dom';
import { AppLayout } from '@/components';
import { ThemeProvider, UnitsProvider } from '@/contexts';

// Providers apart from the chrome: cooking mode needs the theme and units but no header, nav, or footer (ADR-029).
export const Shell = () => (
  <ThemeProvider>
    <UnitsProvider>
      <Outlet />
    </UnitsProvider>
  </ThemeProvider>
);

// Every route sat inside its own AppLayout before; one shared layout route says the same thing once, and keeps the
// header mounted across navigations.
export const Chrome = () => (
  <AppLayout>
    <Outlet />
  </AppLayout>
);
