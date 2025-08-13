// src/App.tsx

import React from 'react';
import { BrowserRouter, Routes, Route } from "react-router-dom";
import { ThemeProvider } from '@/contexts/ThemeContext';
import { HomePage } from '@/pages';
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
              <div>
                Recipes
              </div>
            </AppLayout>
          } />
          <Route path='/profile' element={
            <AppLayout>
              <div>
                Profile
              </div>
            </AppLayout>
          } />
        </Routes>
      </ThemeProvider>
    </BrowserRouter>

  );
};

export default App;
