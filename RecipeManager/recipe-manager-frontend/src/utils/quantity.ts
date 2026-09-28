// Presentation only (ADR-022, spec 012 §8.4): nothing here is ever written back to the server.
import type { Unit } from '@/types';

export interface AmountText {
  visible: string; // what is painted: "1¼ tbsp"
  spoken: string;  // what a screen reader reads: "1¼ tablespoons" (tbsp is otherwise read letter by letter)
}

// A Record over the whole union, so adding a member to the C# enum fails the typecheck here
// until it has a name — instead of rendering "undefined".
const UNIT_NAMES: Record<Unit, { abbr?: string; one: string; many: string }> = {
  Gram: { abbr: 'g', one: 'gram', many: 'grams' },
  Kilogram: { abbr: 'kg', one: 'kilogram', many: 'kilograms' },
  Ounce: { abbr: 'oz', one: 'ounce', many: 'ounces' },
  Pound: { abbr: 'lb', one: 'pound', many: 'pounds' },
  Millilitre: { abbr: 'ml', one: 'millilitre', many: 'millilitres' },
  Litre: { abbr: 'l', one: 'litre', many: 'litres' },
  Teaspoon: { abbr: 'tsp', one: 'teaspoon', many: 'teaspoons' },
  Tablespoon: { abbr: 'tbsp', one: 'tablespoon', many: 'tablespoons' },
  FluidOunce: { abbr: 'fl oz', one: 'fluid ounce', many: 'fluid ounces' },
  Cup: { one: 'cup', many: 'cups' },
  Piece: { one: 'piece', many: 'pieces' },
  Clove: { one: 'clove', many: 'cloves' },
  Pinch: { one: 'pinch', many: 'pinches' },
  Slice: { one: 'slice', many: 'slices' },
  Can: { one: 'can', many: 'cans' },
  Bunch: { one: 'bunch', many: 'bunches' },
  Sprig: { one: 'sprig', many: 'sprigs' },
};

// Units people measure with a spoon or a cup, where "0.33 cup" is not something anyone can pour.
const FRACTION_UNITS: ReadonlySet<Unit> = new Set<Unit>(['Teaspoon', 'Tablespoon', 'Cup', 'FluidOunce']);

// Always scale from the STORED quantity: rounding happens only in formatQuantity, so stepping
// 4 → 7 → 4 servings reproduces the original exactly instead of accumulating rounding error.
export const scaleQuantity = (
  quantity: number | null | undefined,
  written: number,
  current: number,
): number | null => (quantity == null ? null : (quantity * current) / written);

const EIGHTHS = ['', '⅛', '¼', '⅜', '½', '⅝', '¾', '⅞'];
const THIRDS = ['', '⅓', '⅔'];

// Turns a spoon/cup amount into something a cook can measure: the whole part plus the nearest of
// ⅛ ¼ ⅓ ⅜ ½ ⅝ ⅔ ¾ ⅞. See quantity.test.ts for the cases it must satisfy.
export const toKitchenFraction = (value: number): string => {
  let whole = Math.floor(value);
  const rest = value - whole;
  const eighths = Math.round(rest * 8);
  const thirds = Math.round(rest * 3);
  // Prefer eighths on a tie: ⅜ and ⅝ are real spoon measures. Ties fall halfway between a third and its
  // nearest eighth (0.0625, 0.2917, 0.3542, 0.6458, 0.7083 — ¼ beats ⅓ at 0.2917).
  const useThirds = Math.abs(rest - thirds / 3) < Math.abs(rest - eighths / 8);
  const [steps, parts, glyphs] = useThirds ? [thirds, 3, THIRDS] : [eighths, 8, EIGHTHS];

  let glyph = glyphs[steps] ?? '';
  if (steps === parts) {
    whole += 1; // the fraction rounded up to a whole: 1.97 → "2", never "1⁸⁄₈"
    glyph = '';
  }

  if (whole === 0 && glyph === '') return value > 0 ? '⅛' : '0'; // never show 0 for a real amount
  return whole === 0 ? glyph : `${whole}${glyph}`;
};

// Below 1, two significant digits (0.25, 0.33, 0.001) so a small positive amount never shows as 0;
// below 10, one decimal place; from 10 up, whole numbers. Number() drops the trailing zeros toFixed adds.
const toTrimmedDecimal = (value: number): string => {
  if (value < 1) return String(Number(value.toPrecision(2)));
  return String(Number(value.toFixed(value < 10 ? 1 : 0)));
};

export const formatQuantity = (value: number, unit: Unit | null | undefined): string =>
  unit && FRACTION_UNITS.has(unit) ? toKitchenFraction(value) : toTrimmedDecimal(value);

// A lone fraction glyph ("½ cup") or a shown number of at most 1 reads singular; "1¼" and "2" do not.
const SINGULAR_FRACTION = /^[⅛¼⅓⅜½⅝⅔¾⅞]$/;

export const formatAmount = (value: number | null, unit: Unit | null | undefined): AmountText | null => {
  if (value === null) return null;

  const number = formatQuantity(value, unit);
  if (!unit) return { visible: number, spoken: number };

  const names = UNIT_NAMES[unit];
  const word = SINGULAR_FRACTION.test(number) || Number(number) <= 1 ? names.one : names.many;
  return { visible: `${number} ${names.abbr ?? word}`, spoken: `${number} ${word}` };
};
