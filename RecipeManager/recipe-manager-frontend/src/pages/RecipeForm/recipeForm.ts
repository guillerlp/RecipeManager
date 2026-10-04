// The form's state and every rule about it, as pure functions (spec 015 §8.3). RecipeFormPage owns it through
// useReducer; nothing here renders or fetches, so all of it is table-tested in recipeForm.test.ts.
import type { ServerErrors } from '@/services';
import type { IngredientInput, InstructionStepInput, Recipe, UpdateRecipeRequest } from '@/types';
import { parseIngredientLine, serialiseIngredient, type ParseResult, type ParsedIngredient } from '@/utils/ingredientLine';
import { normaliseTag } from '@/utils/tags';

export interface IngredientRow {
  /** Client-only identity: the React key and the target of step references. Never sent. */
  key: string;
  /** The server's id, echoed on update; null for a new row. Never invented (BUG-15). */
  id: string | null;
  text: string;
  /** What the row was loaded with. While `text` still equals `line`, the stored value is sent, not a re-parse. */
  stored: { line: string; value: ParsedIngredient } | null;
}

export interface StepRow {
  key: string;
  text: string;
  duration: string;
  /** Ingredient row keys — not indexes (they move) and not ids (new rows have none). */
  ingredientKeys: string[];
}

export interface RecipeFormState {
  title: string;
  description: string;
  prep: string;
  cook: string;
  servings: string;
  ingredients: IngredientRow[];
  steps: StepRow[];
  tags: string[];
}

export type FieldKey =
  | 'title' | 'description' | 'preparationTime' | 'cookingTime' | 'times' | 'servings' | 'ingredients' | 'instructions' | 'tags';
export type FormErrors = Partial<Record<FieldKey | 'form', string[]>>;
export type Rules = 'draft' | 'publish';

/** The order the error summary lists fields in: the order they appear on the page. */
export const FIELD_ORDER: readonly FieldKey[] = [
  'title', 'description', 'preparationTime', 'cookingTime', 'times', 'servings', 'ingredients', 'instructions', 'tags',
];

/** The element each field's summary link focuses. The times group focuses its first input. */
export const FIELD_IDS: Record<FieldKey, string> = {
  title: 'recipe-title',
  description: 'recipe-description',
  preparationTime: 'recipe-prep',
  cookingTime: 'recipe-cook',
  times: 'recipe-prep',
  servings: 'recipe-servings',
  ingredients: 'recipe-ingredients',
  instructions: 'recipe-steps',
  tags: 'recipe-tags',
};

// A counter, not crypto.randomUUID: keys only need to be unique within one page's lifetime.
let lastKey = 0;
const newKey = (): string => `k${++lastKey}`;

export const emptyIngredient = (): IngredientRow => ({ key: newKey(), id: null, text: '', stored: null });
export const emptyStep = (): StepRow => ({ key: newKey(), text: '', duration: '', ingredientKeys: [] });
export const emptyForm = (): RecipeFormState => ({
  title: '', description: '', prep: '', cook: '', servings: '',
  ingredients: [emptyIngredient()], steps: [emptyStep()], tags: [],
});

const toText = (value: number | null | undefined): string => (value == null ? '' : String(value));

export const fromRecipe = (recipe: Recipe): RecipeFormState => {
  const ingredients = recipe.ingredients.map((ingredient): IngredientRow => {
    const value: ParsedIngredient = {
      quantity: ingredient.quantity ?? null,
      unit: ingredient.unit ?? null,
      name: ingredient.name,
      notes: ingredient.notes ?? null,
    };
    const line = serialiseIngredient(value);
    return { key: newKey(), id: ingredient.id, text: line, stored: { line, value } };
  });
  const keyById = new Map(recipe.ingredients.map((ingredient, index) => [ingredient.id, ingredients[index].key]));

  return {
    title: recipe.title,
    description: recipe.description ?? '',
    prep: toText(recipe.preparationTime),
    cook: toText(recipe.cookingTime),
    servings: toText(recipe.servings),
    ingredients: ingredients.length > 0 ? ingredients : [emptyIngredient()],
    steps: recipe.instructions.length > 0
      ? recipe.instructions.map(step => ({
        key: newKey(),
        text: step.text,
        duration: toText(step.durationMinutes),
        ingredientKeys: step.ingredientIds.flatMap(id => {
          const key = keyById.get(id);
          return key === undefined ? [] : [key];
        }),
      }))
      : [emptyStep()],
    tags: [...recipe.tags],
  };
};

// null for a blank row: the empty "next line" is not an ingredient and is never sent.
export const parseRow = (row: IngredientRow): ParseResult | null => {
  if (row.stored && row.text === row.stored.line) return { ok: true, value: row.stored.value };
  return row.text.trim() === '' ? null : parseIngredientLine(row.text);
};

export type FormAction =
  | { type: 'setField'; field: 'title' | 'description' | 'prep' | 'cook' | 'servings'; value: string }
  | { type: 'setIngredient'; key: string; text: string }
  // The row arrives ready-made: the reducer stays pure, and the caller knows the new key to focus.
  | { type: 'addIngredient'; row: IngredientRow; after?: string }
  | { type: 'removeIngredient'; key: string }
  | { type: 'addStep'; step: StepRow }
  | { type: 'setStep'; key: string; text?: string; duration?: string }
  | { type: 'removeStep'; key: string }
  | { type: 'toggleStepIngredient'; stepKey: string; ingredientKey: string }
  | { type: 'addTag'; tag: string }
  | { type: 'removeTag'; tag: string };

export const formReducer = (state: RecipeFormState, action: FormAction): RecipeFormState => {
  switch (action.type) {
    case 'setField':
      return { ...state, [action.field]: action.value };
    case 'setIngredient':
      return {
        ...state,
        ingredients: state.ingredients.map(row => (row.key === action.key ? { ...row, text: action.text } : row)),
      };
    case 'addIngredient': {
      const index = state.ingredients.findIndex(row => row.key === action.after);
      const at = index === -1 ? state.ingredients.length : index + 1;
      return { ...state, ingredients: [...state.ingredients.slice(0, at), action.row, ...state.ingredients.slice(at)] };
    }
    case 'removeIngredient':
      // Steps lose the reference in the same update, so no step can ever point at a row that is gone.
      return {
        ...state,
        ingredients: state.ingredients.filter(row => row.key !== action.key),
        steps: state.steps.map(step => ({ ...step, ingredientKeys: step.ingredientKeys.filter(key => key !== action.key) })),
      };
    case 'addStep':
      return { ...state, steps: [...state.steps, action.step] };
    case 'setStep':
      return {
        ...state,
        steps: state.steps.map(step => (step.key === action.key
          ? { ...step, text: action.text ?? step.text, duration: action.duration ?? step.duration }
          : step)),
      };
    case 'removeStep':
      return { ...state, steps: state.steps.filter(step => step.key !== action.key) };
    case 'toggleStepIngredient':
      return {
        ...state,
        steps: state.steps.map(step => {
          if (step.key !== action.stepKey) return step;
          const keys = step.ingredientKeys.includes(action.ingredientKey)
            ? step.ingredientKeys.filter(key => key !== action.ingredientKey)
            : [...step.ingredientKeys, action.ingredientKey];
          return { ...step, ingredientKeys: keys };
        }),
      };
    case 'addTag': {
      const tag = normaliseTag(action.tag);
      return tag === undefined || state.tags.includes(tag) ? state : { ...state, tags: [...state.tags, tag] };
    }
    case 'removeTag':
      return { ...state, tags: state.tags.filter(tag => tag !== action.tag) };
  }
};

const optionalInt = (value: string): number | null => (value.trim() === '' ? null : Number(value));

// Call only after validate() found nothing: an unparseable line here is a programming error, not user input.
export const toRequest = (state: RecipeFormState): UpdateRecipeRequest => {
  // Built in one pass, so each row's index is its position in THIS array: a step reference cannot be computed
  // against a different or stale list (ADR-023: requests address ingredients by index, responses by id).
  const indexByKey = new Map<string, number>();
  const ingredients: IngredientInput[] = [];
  for (const row of state.ingredients) {
    const parsed = parseRow(row);
    if (parsed === null) continue;
    if (!parsed.ok) throw new Error(`toRequest needs a validated form: "${row.text}" does not parse`);
    indexByKey.set(row.key, ingredients.length);
    ingredients.push({ id: row.id, ...parsed.value });
  }

  const instructions = state.steps
    .filter(step => step.text.trim() !== '')
    .map((step): InstructionStepInput => ({
      text: step.text.trim(),
      durationMinutes: optionalInt(step.duration),
      // A key with no index points at a blank row: dropped, not an error — the user may reference a line they
      // have not typed yet.
      ingredientIndexes: step.ingredientKeys.flatMap(key => {
        const index = indexByKey.get(key);
        return index === undefined ? [] : [index];
      }),
    }));

  return {
    title: state.title.trim(),
    description: state.description.trim() || null,
    preparationTime: optionalInt(state.prep),
    cookingTime: optionalInt(state.cook),
    servings: optionalInt(state.servings),
    ingredients,
    instructions,
    tags: state.tags,
  };
};

const WHOLE = /^\d+$/;
const blank = (value: string): boolean => value.trim() === '';
const inRange = (value: string, min: number, max: number): boolean =>
  WHOLE.test(value.trim()) && Number(value) >= min && Number(value) <= max;

// A courtesy mirror of the server's rules, so the user sees them before a round-trip. The server stays the
// authority: whatever it says still reaches the page through fromServerErrors.
export const validate = (state: RecipeFormState, rules: Rules): FormErrors => {
  const errors: FormErrors = {};
  const add = (field: FieldKey, message: string) => {
    (errors[field] ??= []).push(message);
  };
  const parsedRows = state.ingredients.map(parseRow).filter(parsed => parsed !== null);
  const steps = state.steps.filter(step => !blank(step.text));

  if (blank(state.title)) add('title', 'Add a title');
  else if (state.title.trim().length > 200) add('title', 'Keep the title to 200 characters');
  if (state.description.trim().length > 1000) add('description', 'Keep the description to 1000 characters');
  if (!blank(state.prep) && !inRange(state.prep, 0, 1439)) add('preparationTime', 'Hands-on time is whole minutes, 0–1439');
  if (!blank(state.cook) && !inRange(state.cook, 0, 1439)) add('cookingTime', 'Oven time is whole minutes, 0–1439');
  if (!blank(state.servings) && !inRange(state.servings, 1, 999)) add('servings', 'Serves is a whole number from 1 to 999');
  if (parsedRows.length > 50) add('ingredients', 'Up to 50 ingredients');
  if (parsedRows.some(parsed => !parsed.ok)) add('ingredients', 'Fix the ingredient lines marked below');
  if (steps.length > 50) add('instructions', 'Up to 50 steps');
  if (steps.some(step => step.text.trim().length > 2000)) add('instructions', 'Keep each step to 2000 characters');
  if (steps.some(step => !blank(step.duration) && !inRange(step.duration, 1, 1439))) {
    add('instructions', 'A step timer is whole minutes, 1–1439');
  }
  if (state.tags.length > 20) add('tags', 'Up to 20 tags');

  if (rules === 'publish') {
    if (blank(state.description)) add('description', 'Add a description to publish');
    if (blank(state.prep)) add('preparationTime', 'Add the hands-on time to publish');
    if (blank(state.cook)) add('cookingTime', 'Add the oven time to publish');
    if (!blank(state.prep) && !blank(state.cook) && Number(state.prep) === 0 && Number(state.cook) === 0) {
      add('times', "Hands-on and oven time can't both be zero");
    }
    if (blank(state.servings)) add('servings', 'Add servings to publish');
    if (parsedRows.length === 0) add('ingredients', 'Add at least one ingredient to publish');
    if (steps.length === 0) add('instructions', 'Add at least one step to publish');
  }
  return errors;
};

export const hasErrors = (errors: FormErrors): boolean => Object.keys(errors).length > 0;

// The server names fields in camelCase, and the cross-field rule as "preparationTime,cookingTime" (RecipeErrors.cs).
const SERVER_FIELDS = new Map<string, FieldKey>([
  ['title', 'title'],
  ['description', 'description'],
  ['preparationTime', 'preparationTime'],
  ['cookingTime', 'cookingTime'],
  ['preparationTime,cookingTime', 'times'],
  ['servings', 'servings'],
  ['ingredients', 'ingredients'],
  ['instructions', 'instructions'],
  ['tags', 'tags'],
]);

export const fromServerErrors = ({ fields, form }: ServerErrors): FormErrors => {
  const errors: FormErrors = form.length > 0 ? { form: [...form] } : {};
  for (const [field, messages] of Object.entries(fields)) {
    (errors[SERVER_FIELDS.get(field) ?? 'form'] ??= []).push(...messages);
  }
  return errors;
};
