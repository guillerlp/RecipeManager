// src/Shell.tsx
// The root route's element: the providers and the app chrome every page sits in (routes.tsx). Its own file because
// fast refresh needs a file that exports only components.
import { Outlet } from 'react-router-dom';
import { AppLayout } from '@/components';
import { ThemeProvider, UnitsProvider } from '@/contexts';

// Every route sat inside its own AppLayout before; one shared layout route says the same thing once, and keeps the
// header mounted across navigations.
export const Shell = () => (
  <ThemeProvider>
    <UnitsProvider>
      <AppLayout>
        <Outlet />
      </AppLayout>
    </UnitsProvider>
  </ThemeProvider>
);
