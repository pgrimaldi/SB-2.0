import { formatText } from './format-text';

describe('formatText', () => {
  it('should fill the placeholders and leave the unknown ones', () => {
    expect(formatText('{{index}} di {{total}}', { index: 2, total: 5 })).toBe('2 di 5');
    expect(formatText('{{ index }} di {{other}}', { index: 2 })).toBe('2 di {{other}}');
  });

  it('should give null without a text', () => {
    expect(formatText(undefined, { index: 2 })).toBeNull();
    expect(formatText('', { index: 2 })).toBeNull();
  });
});
