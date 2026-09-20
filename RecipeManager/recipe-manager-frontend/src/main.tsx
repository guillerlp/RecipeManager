// src/main.tsx
import { StrictMode } from 'react';
import { createRoot } from 'react-dom/client';
import App from './App';

// Self-hosted: Vite bundles and hashes the woff2, so no request leaves our origin and the
// future CSP (SEC-10) stays self-only. ADR-021.
import '@fontsource-variable/newsreader';

import './styles/themes/variables.css';
import './styles/themes/light.css';
import './styles/themes/dark.css';
import './styles/globals.css';
import { QueryClient, QueryClientProvider } from '@tanstack/react-query';
import { ReactQueryDevtools } from '@tanstack/react-query-devtools';

const container = document.getElementById('root')!;
const root = createRoot(container);
const queryClient = new QueryClient({
  defaultOptions: {
    queries: {
      staleTime: 5 * 60 * 1000,
      gcTime: 10 * 60 * 1000, 
      retry: 1,
      refetchOnWindowFocus: false,
    }
  }
})

root.render(
  <StrictMode>
    <QueryClientProvider client={queryClient}>
      <App />
      {import.meta.env.DEV && (
        // top-right: bottom-left (the previous UX-04 fix) and bottom-right now sit on top of
        // BottomNav's mobile tab bar below 768px, hiding a real destination (Home or Profile)
        // behind the devtools button — reopened by R-16's bottom nav. top-left was tried next
        // and rejected: the header brand sits there at every width. top-right is clear of the
        // brand and of every BottomNav item at 375x812 (checked with elementFromPoint at each
        // item's centre, as UX-04's re-review does). Trade-off accepted: at desktop widths the
        // button's 48px box clips a ~20px sliver of the "New recipe" pill's right edge, but the
        // pill's own centre still resolves to the link, not the button — a corner still has to
        // lose, and desktop was not the width this defect was reported at.
        <ReactQueryDevtools initialIsOpen={false} buttonPosition="top-right" />
      )}
    </QueryClientProvider>
  </StrictMode>
);
