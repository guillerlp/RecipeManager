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
    const totalMinutes = recipe.preparationTime + recipe.cookingTime;

    return (
        <Link
            to={`/recipes/${recipe.id}`}
            className={styles.row}
            aria-labelledby={titleId}
            aria-describedby={descriptionId}
        >
            <div className={styles.thumb} aria-hidden="true" />

            <div className={styles.info}>
                <h3 id={titleId} className={styles.title}>{recipe.title}</h3>
                <p id={descriptionId} className={styles.description}>
                    {recipe.description || 'Delicious homemade recipe'}
                </p>
            </div>

            <time className={styles.time} dateTime={getISODuration(totalMinutes)}>
                {formatDuration(totalMinutes)}
            </time>

            <ChevronRightIcon className={styles.chevron} />
        </Link>
    );
};
