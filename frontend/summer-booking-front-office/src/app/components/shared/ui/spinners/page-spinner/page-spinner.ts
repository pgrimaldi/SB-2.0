import {
  ChangeDetectionStrategy,
  Component,
  ElementRef,
  effect,
  input,
  viewChild,
} from '@angular/core';
import { Spinner } from '../spinner/spinner';

/**
 * Covers the whole screen while a request runs (e.g. the actions of a table row). It is a modal
 * `<dialog>`: until the answer nothing can be clicked or reached with the keyboard.
 */
@Component({
  selector: 'app-page-spinner',
  imports: [Spinner],
  templateUrl: './page-spinner.html',
  styleUrl: './page-spinner.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class PageSpinner {
  readonly isLoading = input(false);

  private readonly dialog = viewChild<ElementRef<HTMLDialogElement>>('dialog');

  constructor() {
    effect(() => {
      const dialog = this.dialog()?.nativeElement;
      if (dialog && !dialog.open) {
        dialog.showModal();
      }
    });
  }
}
