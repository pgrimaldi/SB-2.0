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
    const input: HTMLInputElement = fixture.nativeElement.querySelector('input');
    const type = (text: string) => {
      input.value = text;
      input.dispatchEvent(new Event('input'));
      fixture.detectChanges();
    };
    return { fixture, input, type, searches: fixture.componentInstance.searches };
  };

  it('should search 0.5 seconds after the last key, from 3 characters', async () => {
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

  it('should search at once on Enter', async () => {
    const { input, type, searches } = await setup();

    type('lettino');
    input.dispatchEvent(new KeyboardEvent('keydown', { key: 'Enter' }));
    expect(searches).toEqual(['lettino']);

    vi.advanceTimersByTime(500);
    expect(searches).toEqual(['lettino']); // the same text is not searched twice
  });

  it('should show an X with some text that empties the field and searches everything at once', async () => {
    const { fixture, input, type, searches } = await setup();
    const button = () =>
      fixture.nativeElement.querySelector('.search__field__button') as HTMLElement;

    expect(button().getAttribute('aria-label')).toBe('field.search.submit'); // empty: magnifier

    type('lettino');
    vi.advanceTimersByTime(500);
    expect(searches).toEqual(['lettino']);
    expect(button().getAttribute('aria-label')).toBe('field.search.clear');

    button().click();
    fixture.detectChanges();
    expect(input.value).toBe('');
    expect(searches).toEqual(['lettino', '']); // at once, without waiting
    expect(document.activeElement).toBe(input);
    expect(button().getAttribute('aria-label')).toBe('field.search.submit');

    vi.advanceTimersByTime(500);
    expect(searches).toEqual(['lettino', '']); // the empty search is not repeated
  });
});
