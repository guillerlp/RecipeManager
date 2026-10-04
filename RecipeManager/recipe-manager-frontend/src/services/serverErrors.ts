// Reads a failed write into messages the form can place (spec 015 §8.5). Here, not in the form, because only
// services/ imports axios; recipeForm.ts decides where each field's messages go.
import axios from 'axios';

export interface ServerErrors {
  /** Field name as the server sends it, camelCase: "title", "preparationTime,cookingTime". */
  fields: Record<string, string[]>;
  /** Messages that belong to no field. */
  form: string[];
}

// ResultExtensions.CreateProblemDetails: "errors" only when there are several, otherwise "field" + "detail".
interface DomainProblem {
  detail?: string;
  field?: string;
  errors?: { message: string; field?: string | null }[];
}

// ASP.NET Core's ValidationProblemDetails: property paths ("Ingredients[2].Name", "$.tags" from the JSON binder).
interface ValidationProblem {
  errors?: Record<string, string[]>;
}

/** What the user was doing, so a failure message names it: "Couldn't delete", not "Couldn't save". */
export type ServerAction = 'save' | 'publish' | 'delete';

const generic = (action: ServerAction): string => `Couldn't ${action}. Check your connection and try again.`;

const lowerFirst = (value: string): string => value.charAt(0).toLowerCase() + value.slice(1);

export const readServerErrors = (error: unknown, action: ServerAction = 'save'): ServerErrors => {
  const result: ServerErrors = { fields: {}, form: [] };
  const add = (field: string | null | undefined, message: string) => {
    if (field) (result.fields[field] ??= []).push(message);
    else result.form.push(message);
  };
  const response = axios.isAxiosError(error) ? error.response : undefined;

  if (response?.status === 422) {
    const body = response.data as DomainProblem;
    if (body.errors) body.errors.forEach(entry => add(entry.field, entry.message));
    else add(body.field, body.detail ?? generic(action));
    return result;
  }

  if (response?.status === 400) {
    const body = response.data as ValidationProblem;
    for (const [path, messages] of Object.entries(body.errors ?? {})) {
      const field = lowerFirst(path.replace(/^\$\./, '').split(/[.[]/)[0] ?? '');
      messages.forEach(message => add(field, message));
    }
    if (result.form.length === 0 && Object.keys(result.fields).length === 0) add(null, generic(action));
    return result;
  }

  // Never the server's own text past this point: a 500 can carry exception detail (SEC-05).
  add(null, response?.status === 404 ? 'This recipe no longer exists.' : generic(action));
  return result;
};
