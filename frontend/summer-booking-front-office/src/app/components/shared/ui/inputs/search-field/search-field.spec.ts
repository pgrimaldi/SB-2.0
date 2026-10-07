import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { SearchField } from './search-field';

@Component({
  imports: [SearchField],
  template: `<app-search-field
    placeholder="Cerca per nome"
    [texts]="{ submit: 'Cerca', clear: 'Svuota' }"
    (searched)="searches.push($event)"
  />`,
})
class SearchFieldHost {
  readonly searches: string[] = [];
}

describe('SearchField', () => {
  beforeEach(() => vi.useFakeTimers());
  const setup = async () => {
    const fixture = TestBed.createComponent(SearchFieldHost);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    const input = element.querySelector('input')!;
    const type = (text: string) => {
      input.value = text;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    };
    return { fixture, element, input, type, searches: fixture.componentInstance.searches };
  };

  it('should fire search 0.5 seconds after the last key, from 3 characters', async () => {
    const { type, searches } = await setup();

    type('om');
    vi.advanceTimersByTime(500);
    expect(searches).toEqual([]); // shorter than 3: still everything

    type('omb');
    vi.advanceTimersByTime(499);
    expect(searches).toEqual([]);
    vi.advanceTimersByTime(1);
    expect(searches).toEqual(['omb']);

    type('o');
    vi.advanceTimersByTime(500);
    expect(searches).toEqual(['omb', '']); // back under 3: no filter
  });

  it('should stop searching once destroyed', async () => {
    const { fixture, type, searches } = await setup();

    type('omb');
    fixture.destroy();
    vi.advanceTimersByTime(500);

    expect(searches).toEqual([]);
  });

  it('should keep the magnifier button, which searches nothing, and an X that empties the field', async () => {
    const { fixture, element, input, type, searches } = await setup();
    const button = () => element.querySelector<HTMLButtonElement>('.search__field__button')!;
    expect(
      element.querySelector('.search__field__button img, .search__field__button mat-icon'),
    ).toBeNull();

    expect(button().getAttribute('aria-label')).toBe('Cerca'); // empty: magnifier
    type('om');
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    vi.advanceTimersByTime(100);
    expect(searches).toEqual([]); // Enter starts no search

    type('lettino');
    expect(button().getAttribute('aria-label')).toBe('Svuota');

    button().click();
    fixture.detectChanges();
    expect(input.value).toBe('');
    expect(document.activeElement).toBe(input);
    expect(button().getAttribute('aria-label')).toBe('Cerca');

    button().click(); // the magnifier does nothing
    vi.advanceTimersByTime(500);
    expect(searches).toEqual([]);
  });

  it('should show its icons in order [magnifier, X]: Material names if given, else images', async () => {
    const fixture = TestBed.createComponent(CustomIconsHost);
    fixture.detectChanges();
    const element: HTMLElement = fixture.nativeElement;
    const input = element.querySelector('input')!;
    const icon = () =>
      element.querySelector('.search__field__button img, .search__field__button mat-icon')!;
    const type = (text: string) => {
      input.value = text;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    };

    expect(icon().getAttribute('src')).toBe('/assets/images/lente.svg');
    type('lettino');
    expect(icon().getAttribute('src')).toBe('/assets/images/x.svg');

    fixture.componentInstance.matIcon.set(['search', 'close']); // replaces all the images
    fixture.detectChanges();
    expect(icon().textContent?.trim()).toBe('close');
    type('');
    expect(icon().textContent?.trim()).toBe('search');
  });
});

@Component({
  imports: [SearchField],
  template: `<app-search-field placeholder="Cerca" [matIcon]="matIcon()" [pathIcon]="pathIcon" />`,
})
class CustomIconsHost {
  readonly matIcon = signal<string[] | null>(null);
  readonly pathIcon = ['/assets/images/lente.svg', '/assets/images/x.svg'];
}
