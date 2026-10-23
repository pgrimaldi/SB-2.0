import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import { greaterThan, passwordRules, requiredText } from './validators';

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

describe('passwordRules', () => {
  it('should ask for the length, a digit, an uppercase and a lowercase letter, leaving an empty field to requiredText', () => {
    const model = signal<{ password: string | null }>({ password: 'Sabbia2026' });
    const passwordForm = TestBed.runInInjectionContext(() =>
      form(model, (path) => passwordRules(path.password, 8)),
    );

    expect(passwordForm.password().valid()).toBe(true);
    for (const [password, kind] of [
      ['Sab2026', 'min_length'],
      ['Sabbiadoro', 'missing_digit'],
      ['sabbia2026', 'missing_uppercase'],
      ['SABBIA2026', 'missing_lowercase'],
    ]) {
      model.set({ password });
      expect(passwordForm.password().errors()).toEqual([expect.objectContaining({ kind })]);
    }
    model.set({ password: null });
    expect(passwordForm.password().valid()).toBe(true);
  });
});
