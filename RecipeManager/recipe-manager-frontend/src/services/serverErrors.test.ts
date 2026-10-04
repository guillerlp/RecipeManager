import { AxiosError, type AxiosResponse } from 'axios';
import { describe, expect, it } from 'vitest';
import { readServerErrors } from './serverErrors';

const httpError = (status: number, data: unknown) =>
  new AxiosError('Request failed', 'ERR_BAD_REQUEST', undefined, undefined, { status, data } as AxiosResponse);

describe('readServerErrors', () => {
  it('reads a single 422 from field and detail', () => {
    const error = httpError(422, { status: 422, detail: 'Title is required', field: 'title' });
    expect(readServerErrors(error)).toEqual({ fields: { title: ['Title is required'] }, form: [] });
  });

  it('reads every entry of a 422 with errors[], keeping the cross-field name whole', () => {
    const error = httpError(422, {
      status: 422,
      detail: 'Description is required to publish',
      field: 'description',
      errors: [
        { message: 'Description is required to publish', field: 'description', code: 422 },
        { message: 'Preparation and cooking time cannot both be zero', field: 'preparationTime,cookingTime', code: 422 },
      ],
    });
    expect(readServerErrors(error)).toEqual({
      fields: {
        description: ['Description is required to publish'],
        'preparationTime,cookingTime': ['Preparation and cooking time cannot both be zero'],
      },
      form: [],
    });
  });

  it('maps a 400 property path to its first segment, camelCased', () => {
    const error = httpError(400, {
      status: 400,
      errors: {
        'Ingredients[2].Name': ['Ingredient name cannot exceed 200 characters'],
        Title: ['Title cannot exceed 200 characters'],
        '$.tags': ['The tags field is required.'],
      },
    });
    expect(readServerErrors(error)).toEqual({
      fields: {
        ingredients: ['Ingredient name cannot exceed 200 characters'],
        title: ['Title cannot exceed 200 characters'],
        tags: ['The tags field is required.'],
      },
      form: [],
    });
  });

  it('says the recipe is gone on a 404', () => {
    expect(readServerErrors(httpError(404, { status: 404, detail: 'Recipe with ID x was not found' })))
      .toEqual({ fields: {}, form: ['This recipe no longer exists.'] });
  });

  // SEC-05: a 500's body can carry exception detail; it must never reach the page.
  it('never repeats the text of a 500 or a network failure', () => {
    const generic = { fields: {}, form: ["Couldn't save. Check your connection and try again."] };
    expect(readServerErrors(httpError(500, { detail: 'NpgsqlException: secret' }))).toEqual(generic);
    expect(readServerErrors(new AxiosError('Network Error', 'ERR_NETWORK'))).toEqual(generic);
    expect(readServerErrors(new Error('boom'))).toEqual(generic);
  });
});

describe('readServerErrors action', () => {
  it('names the action that failed', () => {
    expect(readServerErrors(new AxiosError('Network Error', 'ERR_NETWORK'), 'delete'))
      .toEqual({ fields: {}, form: ["Couldn't delete. Check your connection and try again."] });
  });
});
