// Moved out of RecipeCard (R-07) so the boundaries can be unit-tested without rendering.

const toSafeMinutes = (minutes: number): number =>
    Math.max(0, Number.isFinite(minutes) ? minutes : 0);

export const formatDuration = (minutes: number): string => {
    const safeMinutes = toSafeMinutes(minutes);

    if (safeMinutes < 60) return `${safeMinutes} min`;

    const hours = Math.floor(safeMinutes / 60);
    const remainingMinutes = safeMinutes % 60;

    return remainingMinutes > 0 ? `${hours}h ${remainingMinutes}min` : `${hours}h`;
};

export const getISODuration = (minutes: number): string => {
    const safeMinutes = toSafeMinutes(minutes);

    if (safeMinutes < 60) return `PT${safeMinutes}M`;

    const hours = Math.floor(safeMinutes / 60);
    const remainingMinutes = safeMinutes % 60;

    return remainingMinutes > 0 ? `PT${hours}H${remainingMinutes}M` : `PT${hours}H`;
};
