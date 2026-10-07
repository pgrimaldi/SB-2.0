import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { greaterThan, requiredText } from './validators';

describe('greaterThan', () => {
  it('should accept only numbers above the limit, an empty field included in the errors', () => {
    const model = signal<{ quantity: number | null }>({ quantity: 3 });
    const quantityForm = TestBed.runInInjectionContext(() =>
      form(model, (path) => greaterThan(path.quantity, 0)),
    );

    expect(quantityForm.quantity().valid()).toBe(true);
    for (const quantity of [0, -1, null]) {
      model.set({ quantity });
      expect(quantityForm.quantity().errors()).toEqual([
        expect.objectContaining({ kind: 'greater_than', limit: 0 }),
      ]);
    }
  });
});

describe('requiredText', () => {
  it('should count a text made only of spaces as empty, like null', () => {
    const model = signal<{ name: string | null }>({ name: 'Mario' });
    const nameForm = TestBed.runInInjectionContext(() =>
      form(model, (path) => requiredText(path.name)),
    );

    expect(nameForm.name().valid()).toBe(true);
    for (const name of [null, '', '   ']) {
      model.set({ name });
      expect(nameForm.name().errors()).toEqual([expect.objectContaining({ kind: 'required' })]);
    }
  });
});
