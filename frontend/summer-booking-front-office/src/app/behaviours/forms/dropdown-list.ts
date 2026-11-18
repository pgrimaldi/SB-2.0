import { Signal, signal } from '@angular/core';
import { Observable, Subscription } from 'rxjs';
import { toApiProblem } from '../../services/api/errors/to-api-problem';
import { ErrorTextBehaviour } from '../errors/error-text.behaviour';

/**
 * The options of a dropdown read from an API, with the text of a failed read for under the field.
 * After a failed read, opening the dropdown (`openRequest` of the select wrappers, bound to
 * `retry()`) reads them again; a list already read is not asked again.
 */
export class DropdownList<T> {
  private readonly itemsValue = signal<readonly T[]>([]);
  private readonly loadErrorValue = signal<string | null>(null);
  readonly items: Signal<readonly T[]> = this.itemsValue.asReadonly();
  readonly loadError: Signal<string | null> = this.loadErrorValue.asReadonly();
  private subscription?: Subscription;

  /** `afterRead` runs at every answer, once `items` holds it. */
  constructor(
    private readonly read: () => Observable<readonly T[]>,
    private readonly errorText: ErrorTextBehaviour,
    private readonly afterRead?: () => void,
  ) {}

  /** Reads the options again: a slower answer of an earlier read never replaces them. */
  load(): void {
    this.subscription?.unsubscribe();
    this.loadErrorValue.set(null);
    this.subscription = this.read().subscribe({
      next: (items) => {
        this.itemsValue.set(items);
        this.afterRead?.();
      },
      error: (error: unknown) => {
        const problem = toApiProblem(error);
        this.itemsValue.set([]);
        this.loadErrorValue.set(this.errorText.instant(problem.code, problem.args));
      },
    });
  }

  retry(): void {
    if (this.loadError()) {
      this.load();
    }
  }

  /** No options and no error, e.g. when a popup is reset; a read in progress stops. */
  clear(): void {
    this.subscription?.unsubscribe();
    this.itemsValue.set([]);
    this.loadErrorValue.set(null);
  }

  destroy(): void {
    this.subscription?.unsubscribe();
  }
}
