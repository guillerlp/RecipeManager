// src/pages/RecipeList.tsx
import React, { useState, useEffect } from 'react';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemText from '@mui/material/ListItemText';
import Divider from '@mui/material/Divider';
import type { Recipe } from '@/types';
import { recipeService } from '@/services';

export const RecipeList: React.FC = () => {
  const [recipes, setRecipes] = useState<Recipe[]>([]);
  const [loading, setLoading] = useState<boolean>(true);
  const [error, setError] = useState<string | null>(null);

  useEffect(() => {
    const fetchRecipes = async (): Promise<void> => {
      setLoading(true);
      try {
        const { data } = await recipeService.getAllRecipes();
        setRecipes(data);
      } catch (err) {
        console.error('Error fetching recipes:', err);
        setError(err instanceof Error ? err.message : 'Unknown error');
      } finally {
        setLoading(false);
      }
    };

    fetchRecipes();
  }, []);

  if (loading) return <div>Loading…</div>;
  if (error)   return <div>Error: {error}</div>;

  return (
    <div>
      <h1>RECIPES</h1>
      <List sx={{ width: '100%', maxWidth: 360 }}>
        {recipes.map((recipe, idx) => (
          <React.Fragment key={recipe.id}>
            <ListItem disablePadding>
              <ListItemText
                disableTypography
                primary={recipe.title}
                secondary={
                  <ul style={{ margin: 0, paddingLeft: '1.25rem' }}>
                    {recipe.ingredients.map((ing, i) => (
                      <li key={i}>{ing}</li>
                    ))}
                  </ul>
                }
              />
            </ListItem>
            {idx < recipes.length - 1 && <Divider component="li" />}
          </React.Fragment>
        ))}
      </List>
    </div>
  );
};