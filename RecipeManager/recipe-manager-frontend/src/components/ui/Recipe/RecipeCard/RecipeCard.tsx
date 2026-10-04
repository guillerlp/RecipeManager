import { useId } from 'react';
import { Link } from 'react-router-dom';
import { ChevronRightIcon } from '@/components/ui/Icon';
import type { Recipe } from '@/types';
import { formatDuration, getISODuration } from '@/utils/duration';
import styles from './RecipeCard.module.css';

interface RecipeCardProps {
    recipe: Recipe;
}

// A link, not a <button>: the row goes somewhere rather than doing something, so middle-click,
// open-in-new-tab and copy-link work, and a screen reader announces a destination (ADR-024,
// reversing the button prescribed by BUG-10's original fix).
export const RecipeCard: React.FC<RecipeCardProps> = ({ recipe }) => {
    const titleId = useId();
    const descriptionId = useId();
    const tagId = useId();
    const tag = recipe.tags[0];
    // The Drafts view lists recipes that may have no description or times yet (ADR-025): absent values
    // render nothing rather than "0 min" or filler text.
    const totalMinutes = (recipe.preparationTime ?? 0) + (recipe.cookingTime ?? 0);
    // aria-describedby replaces the link's content as its description, so text not referenced here is never
    // announced; and no id is referenced unless its element exists.
    const describedBy = [recipe.description ? descriptionId : null, tag ? tagId : null]
        .filter(id => id !== null).join(' ') || undefined;

    return (
        <Link
            to={`/recipes/${recipe.id}`}
            className={styles.row}
            aria-labelledby={titleId}
            aria-describedby={describedBy}
        >
            <div className={styles.thumb} aria-hidden="true" />

            <div className={styles.info}>
                <h3 id={titleId} className={styles.title}>{recipe.title}</h3>
                {recipe.description && (
                    <p id={descriptionId} className={styles.description}>{recipe.description}</p>
                )}
            </div>

            {/* The first tag only, as the design does; the rest are on the detail screen. Plain text, never a
                link: the whole row is already a <Link>, and a link inside a link is invalid HTML (spec 014). */}
            <span className={styles.meta}>
                {totalMinutes > 0 && (
                    <time className={styles.time} dateTime={getISODuration(totalMinutes)}>
                        {formatDuration(totalMinutes)}
                    </time>
                )}
                {tag && <span id={tagId} className={styles.tag} title={tag}>{tag}</span>}
            </span>

            <ChevronRightIcon className={styles.chevron} />
        </Link>
    );
};
