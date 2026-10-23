import type { AmountText } from '@/utils/quantity';
import styles from './IngredientAmount.module.css';

interface IngredientAmountProps {
  amount: AmountText;
  className?: string;
}

// Painted abbreviated ("1¼ tbsp"), read in full ("1¼ tablespoons").
export const IngredientAmount = ({ amount, className }: IngredientAmountProps) => (
  <span className={className}>
    {amount.visible === amount.spoken ? (
      // A bare count ("2.5") reads fine as painted; a second, hidden copy would be announced twice.
      amount.visible
    ) : (
      <>
        <span aria-hidden="true">{amount.visible}</span>
        <span className={styles.spoken}>{amount.spoken}</span>
      </>
    )}
  </span>
);
