// src/pages/Recipe/RecipePage.tsx

import { useRef, useState } from 'react';
import { useSearchParams } from 'react-router-dom';
import styles from './RecipePage.module.css';
import { RecipeList, SearchBar, SegmentedControl } from '@/components';
import type { RecipeStatus } from '@/types';
import { normaliseTag } from '@/utils/tags';

const STATUS_OPTIONS: readonly { value: RecipeStatus; label: string }[] = [
    { value: 'Published', label: 'Published' },
    { value: 'Draft', label: 'Drafts' },
];

export const RecipePage: React.FC = () => {
    const [searchQuery, setSearchQuery] = useState<string>("");
    const [searchParams, setSearchParams] = useSearchParams();
    const searchInput = useRef<HTMLInputElement>(null);
    // The URL, not state, owns the filter: it is linkable from the detail kicker and survives reload.
    const tag = normaliseTag(searchParams.get('tag'));
    // ?status=draft, beside ?tag=: Back and reload keep the view, and Published needs no parameter at all.
    const status: RecipeStatus = searchParams.get('status') === 'draft' ? 'Draft' : 'Published';

    const setStatus = (next: RecipeStatus) => {
        setSearchParams(params => {
            if (next === 'Draft') params.set('status', 'draft');
            else params.delete('status');
            return params;
        });
    };

    const handleSearchChange = (query:string) => {
        setSearchQuery(query);
    }

    const clearTag = () => {
        setSearchParams(params => {
            params.delete('tag');
            return params;
        });
        // The clear button unmounts with the filter; without this, keyboard focus would fall back to <body>.
        searchInput.current?.focus();
    }

    return(
        <section className={styles.heroSection}>
            <section className={styles.searchSection}>
                <SegmentedControl legend="Show" name="status" options={STATUS_OPTIONS} value={status} onChange={setStatus} />
                <SearchBar
                    ref={searchInput}
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
                    status={status}
                />
            </section>
        </section>
    )
};
