import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { greaterThan } from './validators';

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
