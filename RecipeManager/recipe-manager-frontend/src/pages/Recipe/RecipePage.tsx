// src/pages/Recipe/RecipePage.tsx

import { useState } from 'react';
import styles from './RecipePage.module.css';
import { RecipeList, SearchBar } from '@/components';

export const RecipePage: React.FC = () => {
    const [searchQuery, setSearchQuery] = useState<string>("");

    const handleSearchChange = (query:string) => {
        setSearchQuery(query);
    }

    return(
        <section className={styles.heroSection}>
            <section className={styles.searchSection}>
                <SearchBar 
                    searchQuery={searchQuery}
                    onSearchChange={handleSearchChange}
                    placeholder='Search by name, ingredient or a word you remember'
                />
            </section>
            
            <section className={styles.recipeListSection}>
                <RecipeList 
                    searchQuery={searchQuery} 
                />      
            </section>
        </section>
    )
};