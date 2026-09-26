import type { InstructionStep } from '@/types';
import styles from './MethodSteps.module.css';

interface MethodStepsProps {
  steps: InstructionStep[];
}

// The number is painted from the index and hidden from assistive technology: the <ol> already
// announces its position, so a spoken "1" as well would read every number twice.
// role="list" looks redundant on an <ol>, but Safari/VoiceOver drops list semantics from a list
// styled with list-style: none.
export const MethodSteps = ({ steps }: MethodStepsProps) => (
  <ol role="list" className={styles.steps}>
    {steps.map((step, index) => (
      <li key={step.id} className={styles.step}>
        <span className={styles.number} aria-hidden="true">{index + 1}</span>
        <p className={styles.text}>{step.text}</p>
      </li>
    ))}
  </ol>
);
