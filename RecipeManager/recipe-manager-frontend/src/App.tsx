// src/App.tsx

import React, { useState } from 'react';
import { RouterProvider, createBrowserRouter } from 'react-router-dom';
import { routes } from './routes';

const App: React.FC = () => {
  // Created on first render, not at import: the router reads window.location when it is created, and App.test
  // sets the location just before rendering.
  const [router] = useState(() => createBrowserRouter(routes));
  return <RouterProvider router={router} />;
};

export default App;
