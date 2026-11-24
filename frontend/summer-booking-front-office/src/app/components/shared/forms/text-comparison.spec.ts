import { comparableJson, sameText, sameTextIgnoringCase } from './text-comparison';

describe('text comparison', () => {
  it('should compare texts without the spaces at the sides', () => {
    expect(sameText(' Fila 2 ', 'Fila 2')).toBe(true);
    expect(sameText('Fila 2', 'fila 2')).toBe(false);
    expect(sameText('   ', null)).toBe(true);
    expect(sameTextIgnoringCase(' FILA 2', 'fila 2 ')).toBe(true);
    expect(comparableJson({ name: 'Fila 2 ', note: '  ' })).toBe(
      comparableJson({ name: 'Fila 2', note: null }),
    );
    expect(comparableJson({ name: 'Fila 2' })).not.toBe(comparableJson({ name: 'Fila 3' }));
  });
});
