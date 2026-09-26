// src/App.tsx

import React from 'react';
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { ThemeProvider } from '@/contexts';
import { HomePage, NotFoundPage, ProfilePage, RecipeDetailPage, RecipePage } from '@/pages';
import { AppLayout } from '@/components';

const App: React.FC = () => {
  return (
    <BrowserRouter>
      <ThemeProvider>
        <Routes>
          <Route path='/' element={
            <AppLayout>
              <HomePage/>
            </AppLayout>
          } />
          <Route path='/recipes' element={
            <AppLayout>
              <RecipePage/>
            </AppLayout>
          } />
          <Route path='/recipes/:id' element={
            <AppLayout>
              <RecipeDetailPage/>
            </AppLayout>
          } />
          <Route path='/profile' element={
            <AppLayout>
              <ProfilePage/>
            </AppLayout>
          } />
          <Route path='*' element={
            <AppLayout>
              <NotFoundPage/>
            </AppLayout>
          } />
        </Routes>
      </ThemeProvider>
    </BrowserRouter>

  );
};

export default App;
