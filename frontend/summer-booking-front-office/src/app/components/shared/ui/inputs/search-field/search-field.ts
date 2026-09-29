import { ChangeDetectionStrategy, Component, input, model } from '@angular/core';
import { outputFromObservable, toObservable } from '@angular/core/rxjs-interop';
import { MatButtonModule } from '@angular/material/button';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { TranslatePipe } from '@ngx-translate/core';
import { Subject, debounce, distinctUntilChanged, map, merge, skip, startWith, timer } from 'rxjs';

/**
 * Search field (Angular Material form field) with the magnifier button:
 * `<app-search-field placeholder="…" [(value)]="text" (search)="apply($event)" />`.
 * `search` fires while typing, `delay` ms after the last key, or at once on Enter / magnifier.
 * Texts shorter than `minLength` search for '' (everything). With some text the magnifier becomes
 * an X that empties the field and searches '' at once.
 */
@Component({
  selector: 'app-search-field',
  imports: [MatButtonModule, MatFormFieldModule, MatInputModule, TranslatePipe],
  templateUrl: './search-field.html',
  styleUrl: './search-field.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class SearchField {
  /** Translation key of the placeholder, also used as accessible name. */
  readonly placeholder = input.required<string>();
  readonly value = model('');
  readonly minLength = input(3);
  readonly delay = input(500);

  private readonly submitted = new Subject<string>();

  readonly search = outputFromObservable(
    merge(
      toObservable(this.value).pipe(
        skip(1),
        debounce(() => timer(this.delay())),
      ),
      this.submitted,
    ).pipe(
      map((text) => text.trim()),
      map((text) => (text.length >= this.minLength() ? text : '')),
      // Starts from "no filter" and fires only when the searched text really changes.
      startWith(''),
      distinctUntilChanged(),
      skip(1),
    ),
  );

  protected submit(): void {
    this.submitted.next(this.value());
  }

  /** Empties the field and searches everything at once; the cursor goes back into the field. */
  protected clear(field: HTMLInputElement): void {
    this.value.set('');
    this.submitted.next('');
    field.focus();
  }
}
