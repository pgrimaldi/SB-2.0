import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { form } from '@angular/forms/signals';
import {
  emailAddress,
  greaterThan,
  isTextTaken,
  maxHtmlLength,
  maxRichTextLength,
  passwordRules,
  requiredText,
} from './validators';

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

describe('isTextTaken', () => {
  it('should find a name already used, not minding capitals and the spaces at the ends', () => {
    const names = ['Bassa stagione', null, 'Alta stagione'];

    expect(isTextTaken(' bassa STAGIONE ', names)).toBe(true);
    expect(isTextTaken('Media stagione', names)).toBe(false);
    expect(isTextTaken('   ', names)).toBe(false);
    expect(isTextTaken(null, names)).toBe(false);
  });
});

describe('emailAddress', () => {
  it('should ask, like the backend, for one @ that is neither first nor last, leaving an empty field to requiredText', () => {
    const model = signal<{ email: string | null }>({ email: null });
    const emailForm = TestBed.runInInjectionContext(() =>
      form(model, (path) => emailAddress(path.email)),
    );

    for (const email of [null, '', 'info@lido.it', 'a@b']) {
      model.set({ email });
      expect(emailForm.email().valid()).toBe(true);
    }
    for (const email of ['info.lido.it', '@lido.it', 'info@', 'info@lido@it']) {
      model.set({ email });
      expect(emailForm.email().errors()).toEqual([expect.objectContaining({ kind: 'email' })]);
    }
  });
});

describe('maxHtmlLength', () => {
  it('should count the formatting too, as the backend does', () => {
    const model = signal<{ title: string | null }>({ title: null });
    const titleForm = TestBed.runInInjectionContext(() =>
      form(model, (path) => maxHtmlLength(path.title, 10)),
    );

    for (const title of [null, '', '<p>abc</p>']) {
      model.set({ title });
      expect(titleForm.title().valid()).toBe(true);
    }
    model.set({ title: '<p>abcd</p>' });
    expect(titleForm.title().errors()).toEqual([
      expect.objectContaining({ kind: 'max_html_length', max: 10 }),
    ]);
  });
});

describe('maxRichTextLength', () => {
  it('should count only the text the user sees, not the formatting', () => {
    const model = signal<{ description: string | null }>({ description: null });
    const descriptionForm = TestBed.runInInjectionContext(() =>
      form(model, (path) => maxRichTextLength(path.description, 5)),
    );

    for (const description of [null, '<p><strong>abc</strong></p><p>de</p>', '<p>a &amp; b</p>']) {
      model.set({ description });
      expect(descriptionForm.description().valid()).toBe(true);
    }
    model.set({ description: '<p>abc</p><p>def</p>' });
    expect(descriptionForm.description().errors()).toEqual([
      expect.objectContaining({ kind: 'max_length', max: 5 }),
    ]);
  });
});
