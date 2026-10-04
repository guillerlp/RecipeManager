import { useState, type Dispatch } from 'react';
import { FIELD_IDS, type FormAction } from './recipeForm';
import styles from './RecipeFormPage.module.css';

const MAX_TAGS = 20;

interface TagsEditorProps {
  tags: string[];
  dispatch: Dispatch<FormAction>;
  error: string[] | undefined;
}

export const TagsEditor = ({ tags, dispatch, error }: TagsEditorProps) => {
  const [draft, setDraft] = useState('');
  const full = tags.length >= MAX_TAGS;

  // The reducer normalises and drops blanks and duplicates, the way the server would (spec 014).
  const commit = () => {
    dispatch({ type: 'addTag', tag: draft });
    setDraft('');
  };

  return (
    <section id={FIELD_IDS.tags} tabIndex={-1} className={styles.section} aria-labelledby="tags-heading">
      <div className={styles.sectionHead}>
        <h2 id="tags-heading" className={styles.sectionHeading}>Tags</h2>
        <span className={styles.limit}>Up to 20</span>
      </div>
      {error && <p className={styles.error}>{error.join(' ')}</p>}

      {tags.length > 0 && (
        <ul role="list" className={styles.chips}>
          {tags.map(tag => (
            <li key={tag} className={styles.chip}>
              {tag}
              <button
                type="button"
                className={styles.chipRemove}
                aria-label={`Remove tag ${tag}`}
                onClick={() => dispatch({ type: 'removeTag', tag })}
              >
                ×
              </button>
            </li>
          ))}
        </ul>
      )}

      <div className={styles.field}>
        <label htmlFor="recipe-tag-input" className={styles.fieldLabel}>Add a tag</label>
        {/* Enter or a comma commits. Backspace in an empty input deliberately does nothing: deleting a chip nobody
            is looking at is a destructive shortcut with no undo (spec 015 §8.6). */}
        <input
          id="recipe-tag-input"
          className={styles.input}
          value={draft}
          maxLength={40}
          disabled={full}
          placeholder={full ? 'That’s the limit' : 'roast, weeknight…'}
          onChange={event => setDraft(event.target.value)}
          onKeyDown={event => {
            if (event.key !== 'Enter' && event.key !== ',') return;
            event.preventDefault();
            commit();
          }}
        />
      </div>
    </section>
  );
};
