import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { PasswordField } from './password-field';

describe('PasswordField', () => {
  it('should show and hide the typed password', async () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
    const fixture = TestBed.createComponent(PasswordField);
    fixture.componentRef.setInput('label', 'Password');
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const input = element.querySelector('input')!;
    const toggle = element.querySelector<HTMLButtonElement>('.password__field__toggle')!;

    expect(input.type).toBe('password');
    expect(element.querySelector('label')?.htmlFor).toBe(input.id);

    toggle.click();
    await fixture.whenStable();
    expect(input.type).toBe('text');
    expect(toggle.getAttribute('aria-pressed')).toBe('true');

    toggle.click();
    await fixture.whenStable();
    expect(input.type).toBe('password');
  });
});
