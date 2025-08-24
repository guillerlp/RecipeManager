import { Recipe } from "@/types";
import React from "react";
import Logo from '../../../../assets/mainPhoto.png';
import styles from './RecipeCard.module.css';

interface RecipeCardProps {
  recipe: Recipe;
  onClick?: (recipe: Recipe) => void;
}

export const RecipeCard : React.FC<RecipeCardProps> = ({ recipe, onClick}) => {

    const formatDuration = (minutes: number): string => {
        if(minutes < 60) return `${minutes} min`;

        const hours = Math.floor(minutes / 60);
        const remainingMinutes = minutes % 60;

        return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}min` : `${hours}h`;
    }

    const getISODuration = (minutes: number): string => {
        if (minutes < 60) return `PT${minutes}M`;

        const hours = Math.floor(minutes / 60);
        const remainingMinutes = minutes % 60;

        return remainingMinutes > 0 ? `PT${hours}H${remainingMinutes}M` : `PT${hours}H`;
    };

    const handleClick = () => {
        if(onClick){
            onClick(recipe);
        }
    }

    const handleKeyDown = (event: React.KeyboardEvent) => {
        if(onClick && (event.key === 'Enter' || event.key === ' ')){
            event.preventDefault();
            onClick(recipe);
        }
    }

    const CardComponent = onClick ? 'button' : 'article';
    const cardProps = onClick ? {
        type: 'button' as const,
        onClick: handleClick,
        onKeyDown: handleKeyDown,
        'aria-label': `View ${recipe.title} recipe`,
        tabIndex: 0
    } : {};

    return (
        <CardComponent className={`${styles.recipesList} ${onClick ? styles.clickable : ''}`}{...cardProps}> 
            <div className={styles.imageBox}>
                <img
                    src={recipe.image || Logo}
                    className={styles.heroImage}
                    alt={`${recipe.title} photo`}
                    loading="lazy"
                    decoding="async"
                />
            </div>

            <section className={styles.recipeInfo}>
                <h3 className={styles.recipeTitle}>{recipe.title}</h3>
                <p className={styles.heroSubtitle}>
                    {recipe.description || 'Delicious homemade recipe'}
                </p>
                <div className={styles.recipeTime}>
                    <p className={styles.metaItem}>
                        Prep: <time dateTime={getISODuration(recipe.preparationTime)}>
                        {formatDuration(recipe.preparationTime)}
                        </time>
                    </p>
                    <p className={styles.metaItem}>
                        Cook: <time dateTime={getISODuration(recipe.cookingTime)}>
                        {formatDuration(recipe.cookingTime)}
                        </time>
                    </p>
                </div>
            </section>
        </CardComponent>
    );
};