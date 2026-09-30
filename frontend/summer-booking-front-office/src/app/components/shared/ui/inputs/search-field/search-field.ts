import { ChangeDetectionStrategy, Component, computed, input, signal } from '@angular/core';
import { outputFromObservable, toObservable } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { debounceTime, distinctUntilChanged, map, skip, startWith } from 'rxjs';
import { resolveIcons } from '../../icons/icons';

/** Pause after the last key before `search` fires. */
const SEARCH_DELAY = 500;
/** Texts shorter than this search for '' (everything). */
const MIN_LENGTH = 3;

/** Texts of `app-search-field`, already translated; a missing one is left out. */
export interface SearchFieldTexts {
  /** Accessible name of the magnifier button. */
  submit?: string;
  /** Accessible name of the X that empties the field. */
  clear?: string;
}

/**
 * Search field (Angular Material form field) with the magnifier: `<app-search-field placeholder="…" />`.
 * With some text the magnifier becomes an X that empties the field. `search` fires 0.5 seconds after
 * the last key with the typed text ('' under 3 characters), only when it changes. For now nothing
 * listens to it: the search is not wired to any page.
 * Icons (see `Icons`): [magnifier, X]; without them the buttons have no icon. Texts: `SearchFieldTexts`.
 */
@Component({
  selector: 'app-search-field',
  imports: [MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './search-field.html',
  styleUrl: './search-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchField {
  /** Placeholder, already translated, also used as accessible name. */
  readonly placeholder = input<string>();
  readonly texts = input<SearchFieldTexts | null>();
  /** Icons as Material icon names (Material Symbols font): [magnifier, X]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [magnifier, X]. */
  readonly pathIcon = input<readonly string[] | null>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));

  protected readonly text = signal('');

  readonly search = outputFromObservable(
    toObservable(this.text).pipe(
      skip(1),
      debounceTime(SEARCH_DELAY),
      map((text) => text.trim()),
      map((text) => (text.length >= MIN_LENGTH ? text : '')),
      // Starts from "no filter" and fires only when the searched text really changes.
      startWith(''),
      distinctUntilChanged(),
      skip(1),
    ),
  );

  /** Empties the field; the cursor goes back into it. */
  protected clear(field: HTMLInputElement): void {
    this.text.set('');
    field.focus();
  }
}
