// src/App.test.tsx

import { render, screen } from '@testing-library/react';
import { describe, expect, it } from 'vitest';
import { MemoryRouter } from 'react-router-dom';
import App from './App';
import { NotFoundPage } from '@/pages/NotFound';

describe('NotFoundPage', () => {
  it('explains what happened and offers a way back', () => {
    render(
      <MemoryRouter>
        <NotFoundPage />
      </MemoryRouter>,
    );

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Back to home' })).toBeTruthy();
  });
});

// The test above proves the page renders correctly in isolation, but not that the app's router
// actually reaches it (the real fix for BUG-06). App.tsx mounts its own BrowserRouter, which
// reads from the History API rather than accepting an injected route, so an unknown path is
// simulated the same way BrowserRouter itself would see one: pushing it onto jsdom's history
// before mount.
describe('App routing', () => {
  it('renders the 404 page inside the app shell for an unknown path', () => {
    window.history.pushState({}, '', '/nope');

    render(<App />);

    expect(screen.getByRole('heading', { name: 'Page not found' })).toBeTruthy();
    expect(screen.getByRole('link', { name: 'Back to home' })).toBeTruthy();
    // The shell is still present around the 404 content.
    expect(screen.getByRole('banner')).toBeTruthy();
    expect(screen.getByRole('contentinfo')).toBeTruthy();
  });
});
