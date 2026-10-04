// Add/edit screen, design frame 3d (spec 015). The form model lives in recipeForm.ts; this file wires it to the
// mutation hooks, the router, and the page.
import { useEffect, useReducer, useRef, useState } from 'react';
import { Link, useLocation, useNavigate, useParams } from 'react-router-dom';
import {
  isRecipeId, useCreateRecipe, useDeleteRecipe, usePublishRecipe, useRecipe, useUnpublishRecipe, useUpdateRecipe,
} from '@/hooks';
import { isNotFoundError, readServerErrors } from '@/services';
import type { Recipe } from '@/types';
import { formatDuration } from '@/utils/duration';
import { IngredientsEditor } from './IngredientsEditor';
import {
  FIELD_IDS, FIELD_ORDER, emptyForm, formReducer, fromRecipe, fromServerErrors, hasErrors, toRequest, validate,
  type FieldKey, type FormErrors, type Rules,
} from './recipeForm';
import { StepsEditor } from './StepsEditor';
import { TagsEditor } from './TagsEditor';
import styles from './RecipeFormPage.module.css';

type Intent = 'draft' | 'publish' | 'save' | 'unpublish';
type Verb = 'saving' | 'publishing';

const WHOLE = /^\d+$/;
const minutes = (value: string): number | null => (WHOLE.test(value.trim()) ? Number(value) : null);
const clock = (time: number): string => new Date(time).toLocaleTimeString([], { hour: '2-digit', minute: '2-digit' });
const describedBy = (...ids: (string | false | undefined)[]): string | undefined =>
  ids.filter(id => typeof id === 'string').join(' ') || undefined;

interface FieldErrorProps {
  id: string;
  messages: string[] | undefined;
}

const FieldError = ({ id, messages }: FieldErrorProps) =>
  messages ? <p id={id} className={styles.error}>{messages.join(' ')}</p> : null;

interface RecipeFormProps {
  /** null: a new recipe. */
  recipe: Recipe | null;
}

const RecipeForm = ({ recipe }: RecipeFormProps) => {
  const navigate = useNavigate();
  const location = useLocation();
  const [state, dispatch] = useReducer(formReducer, recipe, initial => (initial ? fromRecipe(initial) : emptyForm()));
  const [errors, setErrors] = useState<FormErrors>({});
  const [verb, setVerb] = useState<Verb>('saving');
  // The first draft save redirects here from /recipes/new and remounts this component; the time travels in state.
  const [savedAt, setSavedAt] = useState<number | null>((location.state as { savedAt?: number } | null)?.savedAt ?? null);
  const [saving, setSaving] = useState(false);
  // State lags a render: two clicks in one tick would both read `saving` as false and create two recipes.
  const busy = useRef(false);
  const [failures, setFailures] = useState(0);
  const summaryRef = useRef<HTMLElement>(null);
  const deleteDialog = useRef<HTMLDialogElement>(null);
  const deleteButton = useRef<HTMLButtonElement>(null);

  const create = useCreateRecipe();
  const update = useUpdateRecipe();
  const publish = usePublishRecipe();
  const unpublish = useUnpublishRecipe();
  const remove = useDeleteRecipe();

  // Focus moves to the summary after every failed attempt, so a keyboard or screen-reader user lands on the list
  // of what to fix instead of hunting through a long form.
  useEffect(() => {
    if (failures > 0) summaryRef.current?.focus();
  }, [failures]);

  const fail = (next: FormErrors, nextVerb: Verb) => {
    setErrors(next);
    setVerb(nextVerb);
    setFailures(count => count + 1);
  };

  const submit = async (intent: Intent) => {
    if (busy.current) return;
    // A published recipe is held to the published tier on every save (ADR-025), so Save checks what Publish does.
    const rules: Rules = intent === 'draft' || intent === 'unpublish' ? 'draft' : 'publish';
    const nextVerb: Verb = intent === 'publish' ? 'publishing' : 'saving';
    const local = validate(state, rules);
    if (hasErrors(local)) {
      fail(local, nextVerb);
      return;
    }

    const request = toRequest(state);
    busy.current = true;
    setSaving(true);
    try {
      if (!recipe) {
        const created = await create.mutateAsync({ ...request, status: intent === 'draft' ? 'Draft' : 'Published' });
        // Replace, not push: Back from the edit screen must not land on a blank form that would create a second recipe.
        if (intent === 'draft') {
          void navigate(`/recipes/${created.id}/edit`, { replace: true, state: { savedAt: Date.now() } });
        } else {
          void navigate(`/recipes/${created.id}`);
        }
        return;
      }
      // Unpublish first, so the edit is checked against the draft tier it is moving to rather than the published one.
      if (intent === 'unpublish') await unpublish.mutateAsync(recipe.id);
      await update.mutateAsync({ id: recipe.id, request });
      setSavedAt(Date.now());
      setErrors({});
      // Saved before publishing, so a 422 from publish still leaves the edit saved as a draft.
      if (intent === 'publish') {
        await publish.mutateAsync(recipe.id);
        void navigate(`/recipes/${recipe.id}`);
      }
    } catch (error) {
      fail(fromServerErrors(readServerErrors(error)), nextVerb);
    } finally {
      busy.current = false;
      setSaving(false);
    }
  };

  const confirmDelete = async () => {
    if (!recipe || busy.current) return;
    busy.current = true;
    setSaving(true);
    try {
      await remove.mutateAsync(recipe.id);
      void navigate('/recipes', { replace: true });
    } catch (error) {
      deleteDialog.current?.close();
      fail(fromServerErrors(readServerErrors(error)), 'saving');
    } finally {
      busy.current = false;
      setSaving(false);
    }
  };

  // 'default' is the key of the first entry React Router saw: nothing to go back to inside the app.
  const discard = () => {
    void (location.key === 'default' ? navigate('/recipes') : navigate(-1));
  };

  const status = recipe?.status ?? null;
  const savedText = savedAt === null ? null : `${status === 'Published' ? 'Saved' : 'Draft saved'} ${clock(savedAt)}`;
  const statusText = saving ? 'Saving…' : (savedText ?? (status ?? 'Not saved yet'));

  const formMessages = errors.form ?? [];
  const fieldMessages = FIELD_ORDER.flatMap(field => (errors[field] ?? []).map(message => ({ field, message })));
  const errorCount = formMessages.length + fieldMessages.length;
  const invalid = (field: FieldKey) => (errors[field] ? true : undefined);

  const prep = minutes(state.prep);
  const cook = minutes(state.cook);
  const total = prep !== null && cook !== null && prep + cook > 0 ? formatDuration(prep + cook) : '—';

  return (
    <form className={styles.page} noValidate aria-labelledby="form-heading" onSubmit={event => event.preventDefault()}>
      <h1 id="form-heading" className={styles.visuallyHidden}>{recipe ? `Edit ${recipe.title}` : 'New recipe'}</h1>

      <div className={styles.bar}>
        {/* Present from first render so assistive tech registers it; a save is then announced (cf. UX-18). */}
        <p role="status" className={styles.saveStatus}>{statusText}</p>
        <div className={styles.actions}>
          <button type="button" className={styles.secondary} onClick={discard}>Discard</button>
          {recipe && (
            <button
              ref={deleteButton}
              type="button"
              className={styles.danger}
              disabled={saving}
              onClick={() => deleteDialog.current?.showModal()}
            >
              Delete recipe
            </button>
          )}
          {status === 'Published' ? (
            <>
              <button type="button" className={styles.secondary} disabled={saving} onClick={() => void submit('unpublish')}>Unpublish</button>
              <button type="button" className={styles.primary} disabled={saving} onClick={() => void submit('save')}>Save</button>
            </>
          ) : (
            <>
              <button type="button" className={styles.secondary} disabled={saving} onClick={() => void submit('draft')}>Save draft</button>
              <button type="button" className={styles.primary} disabled={saving} onClick={() => void submit('publish')}>Publish</button>
            </>
          )}
        </div>
      </div>

      {errorCount > 0 && (
        <section ref={summaryRef} tabIndex={-1} className={styles.summary} aria-labelledby="error-summary-heading">
          <h2 id="error-summary-heading" className={styles.summaryHeading}>
            Fix {errorCount} {errorCount === 1 ? 'thing' : 'things'} before {verb}
          </h2>
          <ul className={styles.summaryList}>
            {formMessages.map(message => <li key={message}>{message}</li>)}
            {fieldMessages.map(({ field, message }) => (
              <li key={`${field}:${message}`}>
                <a
                  href={`#${FIELD_IDS[field]}`}
                  onClick={event => {
                    event.preventDefault();
                    document.getElementById(FIELD_IDS[field])?.focus();
                  }}
                >
                  {message}
                </a>
              </li>
            ))}
          </ul>
        </section>
      )}

      <div className={styles.columns}>
        <div className={styles.main}>
          <div className={styles.field}>
            <label htmlFor={FIELD_IDS.title} className={styles.visuallyHidden}>Recipe title</label>
            <input
              id={FIELD_IDS.title}
              className={styles.title}
              value={state.title}
              placeholder="New recipe"
              maxLength={200}
              aria-invalid={invalid('title')}
              aria-describedby={describedBy(status !== 'Published' && 'title-hint', errors.title && 'title-error')}
              onChange={event => dispatch({ type: 'setField', field: 'title', value: event.target.value })}
            />
            {status !== 'Published' && (
              <p id="title-hint" className={styles.hint}>A title is all a draft needs. Publishing needs the rest.</p>
            )}
            <FieldError id="title-error" messages={errors.title} />
          </div>

          <div className={styles.field}>
            <label htmlFor={FIELD_IDS.description} className={styles.fieldLabel}>Description</label>
            <textarea
              id={FIELD_IDS.description}
              className={styles.input}
              value={state.description}
              rows={2}
              maxLength={1000}
              aria-invalid={invalid('description')}
              aria-describedby={describedBy(errors.description && 'description-error')}
              onChange={event => dispatch({ type: 'setField', field: 'description', value: event.target.value })}
            />
            <FieldError id="description-error" messages={errors.description} />
          </div>

          <fieldset className={styles.stats}>
            <legend className={styles.visuallyHidden}>Time and servings</legend>
            <label className={styles.stat}>
              <span className={styles.statLabel}>Hands on</span>
              <input
                id={FIELD_IDS.preparationTime}
                className={styles.number}
                type="number"
                inputMode="numeric"
                min={0}
                max={1439}
                step={1}
                value={state.prep}
                aria-invalid={invalid('preparationTime') ?? invalid('times')}
                aria-describedby={describedBy('times-hint', errors.preparationTime && 'prep-error', errors.times && 'times-error')}
                onChange={event => dispatch({ type: 'setField', field: 'prep', value: event.target.value })}
              />
            </label>
            <label className={styles.stat}>
              <span className={styles.statLabel}>In the oven</span>
              <input
                id={FIELD_IDS.cookingTime}
                className={styles.number}
                type="number"
                inputMode="numeric"
                min={0}
                max={1439}
                step={1}
                value={state.cook}
                aria-invalid={invalid('cookingTime') ?? invalid('times')}
                aria-describedby={describedBy('times-hint', errors.cookingTime && 'cook-error', errors.times && 'times-error')}
                onChange={event => dispatch({ type: 'setField', field: 'cook', value: event.target.value })}
              />
            </label>
            <label className={styles.stat}>
              <span className={styles.statLabel}>Serves</span>
              <input
                id={FIELD_IDS.servings}
                className={styles.number}
                type="number"
                inputMode="numeric"
                min={1}
                max={999}
                step={1}
                value={state.servings}
                aria-invalid={invalid('servings')}
                aria-describedby={describedBy(errors.servings && 'servings-error')}
                onChange={event => dispatch({ type: 'setField', field: 'servings', value: event.target.value })}
              />
            </label>
            <div className={styles.stat}>
              <span className={styles.statLabel}>Total</span>
              <output className={styles.total}>{total}</output>
            </div>
          </fieldset>
          <p id="times-hint" className={styles.hint}>Times in minutes, 0–1439.</p>
          <FieldError id="prep-error" messages={errors.preparationTime} />
          <FieldError id="cook-error" messages={errors.cookingTime} />
          {/* "Not both zero" belongs to neither input alone, so it sits under the group (07-ux-ui checklist). */}
          <FieldError id="times-error" messages={errors.times} />
          <FieldError id="servings-error" messages={errors.servings} />

          <IngredientsEditor rows={state.ingredients} dispatch={dispatch} error={errors.ingredients} />
          <StepsEditor steps={state.steps} ingredients={state.ingredients} dispatch={dispatch} error={errors.instructions} />
        </div>

        <aside className={styles.rail} aria-label="Tags">
          <TagsEditor tags={state.tags} dispatch={dispatch} error={errors.tags} />
        </aside>
      </div>

      {recipe && (
        // Native <dialog> with showModal(): focus trap, Esc, and an inert background without a library — the
        // case ADR-014 named as the one that might justify one.
        <dialog
          ref={deleteDialog}
          className={styles.dialog}
          aria-labelledby="delete-heading"
          onClose={() => deleteButton.current?.focus()}
        >
          <h2 id="delete-heading" className={styles.dialogTitle}>Delete {recipe.title}?</h2>
          <p className={styles.hint}>This can't be undone.</p>
          <div className={styles.actions}>
            <button type="button" className={styles.secondary} onClick={() => deleteDialog.current?.close()}>Cancel</button>
            <button type="button" className={styles.danger} disabled={saving} onClick={() => void confirmDelete()}>Delete</button>
          </div>
        </dialog>
      )}
    </form>
  );
};

const RecipeNotFound = () => (
  <section className={styles.message}>
    <h1 className={styles.messageTitle}>Recipe not found</h1>
    <p className={styles.hint}>It may have been deleted, or the link is wrong.</p>
    <Link to="/recipes">← All recipes</Link>
  </section>
);

// Same states as the detail page (RecipeDetailPage): the GUID guard first, because a disabled query stays pending.
const EditRecipe = ({ id }: { id: string }) => {
  const query = useRecipe(id);

  if (!isRecipeId(id)) return <RecipeNotFound />;
  if (query.isPending) return <p className={styles.status}>Loading recipe…</p>;
  if (query.isError) {
    if (isNotFoundError(query.error)) return <RecipeNotFound />;
    // The error's own message is not rendered: it can carry server detail (SEC-05).
    return (
      <section className={styles.message}>
        <p className={styles.hint}>Couldn't load this recipe.</p>
        <button type="button" className={styles.secondary} onClick={() => void query.refetch()}>Retry</button>
      </section>
    );
  }
  // Keyed by id: another recipe starts a fresh form instead of inheriting this one's state.
  return <RecipeForm key={query.data.id} recipe={query.data} />;
};

export const RecipeFormPage: React.FC = () => {
  const { id } = useParams();
  return id === undefined ? <RecipeForm key="new" recipe={null} /> : <EditRecipe id={id} />;
};
