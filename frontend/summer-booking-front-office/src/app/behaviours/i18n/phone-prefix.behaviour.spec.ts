import { signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { Language } from '../../entities/shared/language';
import { LanguageBehaviour } from './language.behaviour';
import { PhonePrefixBehaviour } from './phone-prefix.behaviour';

function setup(): PhonePrefixBehaviour {
  TestBed.configureTestingModule({
    providers: [{ provide: LanguageBehaviour, useValue: { current: signal(Language.It) } }],
  });
  return TestBed.inject(PhonePrefixBehaviour);
}

describe('PhonePrefixBehaviour', () => {
  it('should show the main country of a prefix shared by several countries', () => {
    const phonePrefix = setup();

    expect(phonePrefix.countryOf('1')).toBe('US');
    expect(phonePrefix.countryOf('+44')).toBe('GB');
    expect(phonePrefix.countryOf('41')).toBe('CH');
    expect(phonePrefix.countryOf(null)).toBe('IT');
  });

  it('should send the prefix only with a number', () => {
    const phonePrefix = setup();

    expect(phonePrefix.prefixFor('340 1112299', '41')).toBe('41');
    expect(phonePrefix.prefixFor('340 1112299', null)).toBe('39');
    expect(phonePrefix.prefixFor('  ', '41')).toBeNull();
  });
});
