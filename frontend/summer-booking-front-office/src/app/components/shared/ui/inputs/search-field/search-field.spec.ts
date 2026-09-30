import { Component } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { provideTranslateService } from '@ngx-translate/core';
import { SearchField } from './search-field';

@Component({
  imports: [SearchField],
  template: `<app-search-field placeholder="Cerca" (search)="searches.push($event)" />`,
})
class SearchFieldHost {
  readonly searches: string[] = [];
}

describe('SearchField', () => {
  beforeEach(() => vi.useFakeTimers());
  afterEach(() => vi.useRealTimers());

  const setup = async () => {
    TestBed.configureTestingModule({ providers: [provideTranslateService()] });
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

  it('should keep the magnifier button, which searches nothing, and an X that empties the field', async () => {
    const { fixture, element, input, type, searches } = await setup();
    const button = () => element.querySelector<HTMLButtonElement>('.search__field__button')!;

    expect(button().getAttribute('aria-label')).toBe('field.search.submit'); // empty: magnifier
    type('om');
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    vi.advanceTimersByTime(100);
    expect(searches).toEqual([]); // Enter starts no search

    type('lettino');
    expect(button().getAttribute('aria-label')).toBe('field.search.clear');

    button().click();
    fixture.detectChanges();
    expect(input.value).toBe('');
    expect(document.activeElement).toBe(input);
    expect(button().getAttribute('aria-label')).toBe('field.search.submit');

    button().click(); // the magnifier does nothing
    vi.advanceTimersByTime(500);
    expect(searches).toEqual([]);
  });
});
