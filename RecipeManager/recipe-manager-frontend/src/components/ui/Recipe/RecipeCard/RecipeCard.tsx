import { Recipe } from "@/types";
import { formatDuration, getISODuration } from "@/utils/duration";
import React from "react";
import styles from './RecipeCard.module.css';

interface RecipeCardProps {
    recipe: Recipe;
    onClick?: (recipe: Recipe) => void;
}

export const RecipeCard : React.FC<RecipeCardProps> = ({ recipe, onClick }) => {

    const handleClick = () => {
        if(onClick){
            onClick(recipe);
        }
    }

    // BUG-10: no detail route exists yet, so nothing ever passes onClick and rows render as
    // <article> rather than a focusable <button> that would do nothing when activated.
    const CardComponent = onClick ? 'button' : 'article';
    const cardProps = onClick ? {
        type: 'button' as const,
        onClick: handleClick,
        'aria-label': `View ${recipe.title} recipe`,
    } : {};

    const totalMinutes = recipe.preparationTime + recipe.cookingTime;

    return (
        <CardComponent className={`${styles.row} ${onClick ? styles.clickable : ''}`} {...cardProps}>
            <div className={styles.thumb} aria-hidden="true" />

            <div className={styles.info}>
                <h3 className={styles.title}>{recipe.title}</h3>
                <p className={styles.description}>
                    {recipe.description || 'Delicious homemade recipe'}
                </p>
            </div>

            <time className={styles.time} dateTime={getISODuration(totalMinutes)}>
                {formatDuration(totalMinutes)}
            </time>
        </CardComponent>
    );
};
