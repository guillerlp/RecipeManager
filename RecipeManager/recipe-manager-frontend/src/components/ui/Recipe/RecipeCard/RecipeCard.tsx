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
    // The list only ever receives published recipes (spec 013), which always have both times; the type
    // cannot say so, hence the fallback.
    const totalMinutes = (recipe.preparationTime ?? 0) + (recipe.cookingTime ?? 0);

    return (
        <Link
            to={`/recipes/${recipe.id}`}
            className={styles.row}
            aria-labelledby={titleId}
            // The tag joins the description: aria-describedby replaces the link's content as its description,
            // so text not referenced here is never announced. No dangling id when the recipe is untagged.
            aria-describedby={tag ? `${descriptionId} ${tagId}` : descriptionId}
        >
            <div className={styles.thumb} aria-hidden="true" />

            <div className={styles.info}>
                <h3 id={titleId} className={styles.title}>{recipe.title}</h3>
                <p id={descriptionId} className={styles.description}>
                    {recipe.description ?? 'Delicious homemade recipe'}
                </p>
            </div>

            {/* The first tag only, as the design does; the rest are on the detail screen. Plain text, never a
                link: the whole row is already a <Link>, and a link inside a link is invalid HTML (spec 014). */}
            <span className={styles.meta}>
                <time className={styles.time} dateTime={getISODuration(totalMinutes)}>
                    {formatDuration(totalMinutes)}
                </time>
                {tag && <span id={tagId} className={styles.tag} title={tag}>{tag}</span>}
            </span>

            <ChevronRightIcon className={styles.chevron} />
        </Link>
    );
};
