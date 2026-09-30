import { Directive } from '@angular/core';

/**
 * Marks a text button of the `app-table` bar, shown on the right (e.g. export, print):
 * `<app-button appTableTextAction appearance="secondary" (clicked)="…">…</app-button>`.
 */
@Directive({ selector: '[appTableTextAction]' })
export class TableTextAction {}
