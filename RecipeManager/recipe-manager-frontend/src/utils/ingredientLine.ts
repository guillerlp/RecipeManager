// Turns "2 tbsp butter, cold" into the structured ingredient the API stores (spec 010 §9, spec 015 §8.2).
// The server never parses free text: this is the only parser, and it runs in the browser.
import type { Unit } from '@/types';
import { UNIT_NAMES, formatAmount } from '@/utils/quantity';

export interface ParsedIngredient {
  quantity: number | null;
  unit: Unit | null;
  name: string;
  notes: string | null;
}

export type ParseResult = { ok: true; value: ParsedIngredient } | { ok: false; error: string };

// The API's caps (IngredientInputDtoValidator, Recipe.ValidateProperties).
const MAX_QUANTITY = 100_000;
const MAX_TEXT = 200;

// Every spelling the display uses — "tbsp", "tablespoon", "tablespoons" — maps back to its Unit.
const UNIT_SYMBOLS = new Map<string, Unit>(
  (Object.keys(UNIT_NAMES) as Unit[]).flatMap(unit => {
    const { abbr, one, many } = UNIT_NAMES[unit];
    return [abbr, one, many]
      .filter((symbol): symbol is string => symbol !== undefined)
      .map(symbol => [symbol.toLowerCase(), unit] as [string, Unit]);
  }),
);

// The mixed number first, so "1 1/2" is not read as 1 followed by a name starting "1/2".
const LEADING_NUMBER = /^(\d+\s+\d+\/\d+|\d+\/\d+|\d+(?:\.\d+)?)/;

const toQuantity = (token: string): number =>
  token.split(/\s+/).reduce((sum, part) => {
    const [numerator = 0, denominator] = part.split('/').map(Number);
    return sum + (denominator === undefined ? numerator : numerator / denominator);
  }, 0);

const words = (text: string): string[] => text.split(/\s+/).filter(word => word !== '');

// "fl oz" is the one two-word symbol, so two words are tried before one.
const takeUnit = (tokens: string[]): { unit: Unit; rest: string[] } | null => {
  for (const length of [2, 1]) {
    if (tokens.length < length) continue;
    const unit = UNIT_SYMBOLS.get(tokens.slice(0, length).join(' ').toLowerCase());
    if (unit) return { unit, rest: tokens.slice(length) };
  }
  return null;
};

export const parseIngredientLine = (line: string): ParseResult => {
  const comma = line.indexOf(',');
  const head = (comma === -1 ? line : line.slice(0, comma)).trim();
  const notes = comma === -1 ? null : line.slice(comma + 1).trim() || null;

  let quantity: number | null = null;
  let unit: Unit | null = null;
  let nameWords = words(head);

  const number = LEADING_NUMBER.exec(head);
  if (number) {
    const after = head.slice(number[0].length);
    const attached = after !== '' && !/^\s/.test(after);
    const rest = words(after);
    const taken = takeUnit(rest);
    // "200g flour" is a quantity and a unit; "3-4 eggs" and "2x eggs" are names that start with a digit.
    if (!attached || taken) {
      // numeric(9,3) in the database: round here so the preview shows what will be stored.
      quantity = Math.round(toQuantity(number[0]) * 1000) / 1000;
      unit = taken?.unit ?? null;
      nameWords = taken?.rest ?? rest;
    }
  }

  const name = nameWords.join(' ');
  if (quantity !== null && !Number.isFinite(quantity)) return { ok: false, error: "That fraction doesn't work" };
  if (quantity === 0) return { ok: false, error: 'Quantity must be more than zero' };
  if (quantity !== null && quantity > MAX_QUANTITY) return { ok: false, error: 'Quantity is too large' };
  if (name === '') return { ok: false, error: "Add the ingredient's name" };
  if (name.length > MAX_TEXT) return { ok: false, error: 'Keep the name to 200 characters' };
  if (notes !== null && notes.length > MAX_TEXT) return { ok: false, error: 'Keep the note to 200 characters' };
  return { ok: true, value: { quantity, unit, name, notes } };
};

// Writes the STORED quantity. formatQuantity rounds for display (0.333 cup → "⅓"), and a line built from it would
// rewrite the recipe's quantities on a save nobody edited (spec 015 §8.2).
export const serialiseIngredient = ({ quantity, unit, name, notes }: ParsedIngredient): string => {
  const names = unit ? UNIT_NAMES[unit] : null;
  const unitText = names ? (names.abbr ?? (quantity === 1 ? names.one : names.many)) : null;
  const head = [quantity === null ? null : String(quantity), unitText, name].filter(part => part !== null).join(' ');
  return notes ? `${head}, ${notes}` : head;
};

// The preview under a row. Display rounding is right here: it is never sent.
export const describeIngredient = ({ quantity, unit, name, notes }: ParsedIngredient): string => {
  const amount = formatAmount(quantity, unit);
  const text = notes ? `${name}, ${notes}` : name;
  return amount ? `${amount.visible} · ${text}` : text;
};
