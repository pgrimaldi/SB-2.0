import { ChangeDetectionStrategy, Component } from '@angular/core';

/**
 * One row of a form: what is marked `appFormRowLabel` on the left, everything else (fields, words,
 * buttons) on the right. When the row itself is narrower than 30rem, wherever it is (a popup, a page,
 * a phone), the label goes on top and the controls take the whole row under it.
 */
@Component({
  selector: 'app-form-row',
  templateUrl: './form-row.html',
  styleUrl: './form-row.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class FormRow {}
