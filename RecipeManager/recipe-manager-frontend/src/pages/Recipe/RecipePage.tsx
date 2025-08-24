// src/pages/Recipe/RecipePage.tsx

import styles from './RecipePage.module.css';
import { RecipeList } from '@/components';

export const RecipePage: React.FC = () => {
    return(
        <section className={styles.heroSection}>
            <section className={styles.searchSecion}>
                Barra busqueda
            </section>
            
            <section className={styles.recipeListSection}>
                  <RecipeList />      
            </section>
        </section>
    )
};