// src/pages/Recipe/RecipePage.tsx

import { useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import styles from './RecipePage.module.css';
import { RecipeList, SearchBar } from '@/components';

// The same normalisation the server applies to stored tags (spec 014), so a hand-typed ?tag=Roast still
// matches "roast". The URL, not state, owns the filter: it is linkable from the detail kicker and survives reload.
// An empty or all-whitespace ?tag= normalises to "" and means no filter at all.
const normaliseTag = (value: string | null) => {
    const tag = value?.trim().replace(/\s+/g, ' ').toLowerCase();
    return tag === '' ? undefined : tag;
};

export const RecipePage: React.FC = () => {
    const [searchQuery, setSearchQuery] = useState<string>("");
    const [searchParams, setSearchParams] = useSearchParams();
    const tag = normaliseTag(searchParams.get('tag'));

    const handleSearchChange = (query:string) => {
        setSearchQuery(query);
    }

    const clearTag = () => {
        setSearchParams(params => {
            params.delete('tag');
            return params;
        });
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

            {tag && (
                <p className={styles.activeTag}>
                    Tagged <span className={styles.tagChip}>{tag}</span>
                    <button
                        type="button"
                        className={styles.clearTag}
                        onClick={clearTag}
                        aria-label={`Clear tag filter: ${tag}`}
                    >
                        ×
                    </button>
                </p>
            )}

            <section className={styles.recipeListSection}>
                <RecipeList
                    searchQuery={searchQuery}
                    tag={tag}
                />
            </section>
        </section>
    )
};
