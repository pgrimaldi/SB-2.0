import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { FilledTextarea } from './filled-textarea';

@Component({
  imports: [FilledTextarea],
  template: `<app-filled-textarea label="Messaggio" [error]="error()" [(value)]="message" />`,
})
class FilledTextareaHost {
  readonly message = signal('');
  readonly error = signal<string | null>(null);
}

describe('FilledTextarea', () => {
  const setup = async () => {
    const fixture = TestBed.createComponent(FilledTextareaHost);
    await fixture.whenStable();
    const element: HTMLElement = fixture.nativeElement;
    return {
      fixture,
      host: fixture.componentInstance,
      element,
      textarea: element.querySelector('textarea')!,
    };
  };

  it('should show its label tied to a three-row text area and give back what is typed', async () => {
    const { host, element, textarea } = await setup();

    expect(element.querySelector('label')?.getAttribute('for')).toBe(textarea.id);
    expect(textarea.rows).toBe(3);
    textarea.value = 'Vorrei informazioni.';
    textarea.dispatchEvent(new Event('input'));
    expect(host.message()).toBe('Vorrei informazioni.');
  });

  it('should show the error under the field, tied for screen readers', async () => {
    const { fixture, host, element, textarea } = await setup();
    host.error.set('Questo campo è obbligatorio');
    await fixture.whenStable();
    const message = element.querySelector('.filled__textarea__error')!;

    expect(message.textContent?.trim()).toBe('Questo campo è obbligatorio');
    expect(textarea.getAttribute('aria-invalid')).toBe('true');
    expect(textarea.getAttribute('aria-describedby')).toBe(message.id);
  });
});
