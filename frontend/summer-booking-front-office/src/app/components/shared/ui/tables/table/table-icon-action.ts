import { Directive } from '@angular/core';

/**
 * Marks an icon button of the `app-table` bar, shown on the left of the search (e.g. delete,
 * duplicate): `<app-icon-button appTableIconAction [label]="…" [pathIcon]="[…]" (clicked)="…" />`.
 */
@Directive({ selector: '[appTableIconAction]' })
export class TableIconAction {}
