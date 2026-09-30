import { TestBed } from '@angular/core/testing';
import { PasswordField } from './password-field';

describe('PasswordField', () => {
  it('should show and hide the typed password', async () => {
    const fixture = TestBed.createComponent(PasswordField);
    fixture.componentRef.setInput('label', 'Password');
    fixture.componentRef.setInput('texts', { show: 'Mostra password' });
    fixture.componentRef.setInput('pathIcon', ['/eye.svg', '/eye-slash.svg']);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    const input = element.querySelector('input')!;
    const toggle = element.querySelector<HTMLButtonElement>('.password__field__toggle')!;
    const icon = () => toggle.querySelector('img')?.getAttribute('src');

    expect(input.type).toBe('password');
    expect(icon()).toBe('/eye-slash.svg'); // icons: [password shown, password hidden]
    expect(element.querySelector('label')?.htmlFor).toBe(input.id);
    expect(toggle.getAttribute('aria-label')).toBe('Mostra password');

    toggle.click();
    await fixture.whenStable();
    expect(input.type).toBe('text');
    expect(toggle.getAttribute('aria-pressed')).toBe('true');
    expect(icon()).toBe('/eye.svg');

    toggle.click();
    await fixture.whenStable();
    expect(input.type).toBe('password');
  });
});
