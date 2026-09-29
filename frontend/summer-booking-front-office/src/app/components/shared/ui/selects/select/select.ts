import { ChangeDetectionStrategy, Component, ViewEncapsulation, input, model } from '@angular/core';
import { MatSelectModule } from '@angular/material/select';
import { TranslatePipe } from '@ngx-translate/core';

/** An option of `app-select`: `key` is the value, `value` the translation key of the text shown. */
export interface SelectOption<K extends string = string> {
  key: K;
  value: string;
}

/**
 * Select (Angular Material select) styled as the grey pill of the reference:
 * `<app-select label="…" [options]="options" [(selected)]="key" />`.
 */
@Component({
  selector: 'app-select',
  imports: [MatSelectModule, TranslatePipe],
  templateUrl: './select.html',
  styleUrl: './select.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  host: { class: 'select' },
  // The options open in an overlay outside the component: their styles must be global.
  encapsulation: ViewEncapsulation.None,
})
export class Select<K extends string = string> {
  readonly options = input.required<readonly SelectOption<K>[]>();
  readonly selected = model.required<K>();
  /** Translation key of the accessible name. */
  readonly label = input.required<string>();
}
