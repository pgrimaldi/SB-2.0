import { ChangeDetectionStrategy, Component, ViewEncapsulation, input, model } from '@angular/core';
import { MatSelectModule } from '@angular/material/select';

/** An option of `app-select`: `value` is what gets selected, `label` the text shown (translated). */
export interface SelectOption<K extends string = string> {
  value: K;
  label: string;
}

/**
 * Select (Angular Material select) styled as the grey pill of the reference:
 * `<app-select accessibleLabel="…" [options]="options" [(selected)]="value" />`.
 */
@Component({
  selector: 'app-select',
  imports: [MatSelectModule],
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
  /** Accessible name, already translated. */
  readonly accessibleLabel = input<string>();
}
