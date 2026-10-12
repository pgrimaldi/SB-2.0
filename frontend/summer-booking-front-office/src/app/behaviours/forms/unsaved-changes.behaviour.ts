import { DOCUMENT } from '@angular/common';
import { Injectable, OnDestroy, inject, signal } from '@angular/core';

/** A form of a page or of a popup: what the user typed and has not saved yet. */
export interface UnsavedChanges {
  hasUnsavedChanges(): boolean;
}

@Injectable({ providedIn: 'root' })
export class UnsavedChangesBehaviour implements OnDestroy {
  private readonly window = inject(DOCUMENT).defaultView;

  private readonly forms = new Set<UnsavedChanges>();
  private pending: ((leave: boolean) => void) | null = null;

  readonly asking = signal(false);

  constructor() {
    this.window?.addEventListener('beforeunload', this.beforeUnload);
  }

  watch(form: UnsavedChanges): void {
    this.forms.add(form);
  }

  unwatch(form: UnsavedChanges): void {
    this.forms.delete(form);
  }

  confirmLeave(): Promise<boolean> {
    return this.anyUnsaved() ? this.ask() : Promise.resolve(true);
  }

  confirmDiscard(form: UnsavedChanges): Promise<boolean> {
    return form.hasUnsavedChanges() ? this.ask() : Promise.resolve(true);
  }

  answer(leave: boolean): void {
    const pending = this.pending;
    this.pending = null;
    this.asking.set(false);
    pending?.(leave);
  }

  private ask(): Promise<boolean> {
    this.answer(false);
    this.asking.set(true);
    return new Promise((resolve) => (this.pending = resolve));
  }

  private anyUnsaved(): boolean {
    return [...this.forms].some((form) => form.hasUnsavedChanges());
  }

  private readonly beforeUnload = (event: BeforeUnloadEvent): void => {
    if (this.anyUnsaved()) {
      event.preventDefault();
    }
  };

  ngOnDestroy(): void {
    this.window?.removeEventListener('beforeunload', this.beforeUnload);
  }
}
