import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  computed,
  input,
  output,
  signal,
} from '@angular/core';
import { toObservable } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatIconModule } from '@angular/material/icon';
import { MatInputModule } from '@angular/material/input';
import { Subscription, debounceTime, distinctUntilChanged, map, skip, startWith } from 'rxjs';
import { resolveIcons } from '../../icons/icons';

const SEARCH_DELAY = 500;
/** Shorter texts search for '' (everything). */
const MIN_LENGTH = 3;

export interface SearchFieldTexts {
  submit?: string;
  clear?: string;
}

/** `app-table` listens to `searched`; the search of the header is not wired to any page yet. */
@Component({
  selector: 'app-search-field',
  imports: [MatButtonModule, MatFormFieldModule, MatIconModule, MatInputModule],
  templateUrl: './search-field.html',
  styleUrl: './search-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchField implements OnDestroy {
  readonly placeholder = input<string>();
  readonly texts = input<SearchFieldTexts | null>();
  /** Icons as Material icon names (Material Symbols font): [magnifier, X]. */
  readonly matIcon = input<readonly string[] | null>();
  /** Icons as image paths, used when `matIcon` is not given: [magnifier, X]. */
  readonly pathIcon = input<readonly string[] | null>();
  /** Not `search`: an `<input type="search">` fires a native `search` event (e.g. on Enter) too. */
  readonly searched = output<string>();

  protected readonly icons = computed(() => resolveIcons(this.matIcon(), this.pathIcon()));
  protected readonly text = signal('');
  private readonly searchSubscription: Subscription;

  constructor() {
    this.searchSubscription = toObservable(this.text)
      .pipe(
        skip(1),
        debounceTime(SEARCH_DELAY),
        map((text) => text.trim()),
        map((text) => (text.length >= MIN_LENGTH ? text : '')),
        // Starts from "no filter" and fires only when the searched text really changes.
        startWith(''),
        distinctUntilChanged(),
        skip(1),
      )
      .subscribe((text) => this.emitSearch(text));
  }

  protected clear(field: HTMLInputElement): void {
    this.text.set('');
    field.focus();
  }

  private emitSearch(text: string): void {
    this.searched.emit(text);
  }

  ngOnDestroy(): void {
    this.searchSubscription.unsubscribe();
  }
}
