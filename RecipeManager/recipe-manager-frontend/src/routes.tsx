// src/routes.tsx
// The route table, apart from App so tests can mount it under createMemoryRouter. A data router rather than
// <BrowserRouter>: useBlocker, which the form's unsaved-changes guard needs, only works under one (ADR-027).
import type { RouteObject } from 'react-router-dom';
import { CookingModePage, HomePage, NotFoundPage, ProfilePage, RecipeDetailPage, RecipeFormPage, RecipePage } from '@/pages';
import { Chrome, Shell } from './Shell';

export const routes: RouteObject[] = [
  {
    element: <Shell />,
    children: [
      {
        element: <Chrome />,
        children: [
          { path: '/', element: <HomePage /> },
          { path: '/recipes', element: <RecipePage /> },
          { path: '/recipes/new', element: <RecipeFormPage /> },
          { path: '/recipes/:id', element: <RecipeDetailPage /> },
          { path: '/recipes/:id/edit', element: <RecipeFormPage /> },
          { path: '/profile', element: <ProfilePage /> },
          { path: '*', element: <NotFoundPage /> },
        ],
      },
      // Full screen, outside the chrome (spec 017 §8.1).
      { path: '/recipes/:id/cook', element: <CookingModePage /> },
    ],
  },
];
