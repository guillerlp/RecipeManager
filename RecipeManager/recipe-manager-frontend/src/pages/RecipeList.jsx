// src/pages/RecipeList.jsx
import React, { useState, useEffect } from 'react';
import { recipeService } from '../services/recipeService';
import List from '@mui/material/List';
import ListItem from '@mui/material/ListItem';
import ListItemText from '@mui/material/ListItemText'
import Typography from '@mui/material/Typography';
import Divider from '@mui/material/Divider';

const RecipeList = () => {
  const [recipes, setRecipes] = useState([]);
  const [loading, setLoading] = useState(true);
  const [error, setError] = useState(null);

  useEffect(() => {
    fetchRecipes();
  }, []);

  const fetchRecipes = async () => {
    try {
      setLoading(true);
      const response = await recipeService.getAllRecipes();
      setRecipes(response.data);
    } catch (error) {
      console.error('Error fetching recipes:', error);
      setError('Failed to fetch recipes');
    } finally {
      setLoading(false);
    }
  };

  if (loading) return <div>Loading...</div>;
  if (error) return <div>Error: {error}</div>;

  return (
    <div>
        <h1>RECIPES</h1>
        <List sx={{ width: '100%', maxWidth: 360}}>
            {recipes.map((recipe, idx) => 
                <React.Fragment key = {recipe.id}>
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
                    
                {/* don’t render divider after the last item */}
                {idx < recipes.length - 1 && <Divider component="li" />}
                </React.Fragment>
            )}
        </List>
    </div>
  );
};

export default RecipeList;