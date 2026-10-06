import { DOCUMENT } from '@angular/common';
import { Injectable, OnDestroy, inject, signal } from '@angular/core';

/** A form of a page or of a popup: what the user typed and has not saved yet. */
export interface UnsavedChanges {
  hasUnsavedChanges(): boolean;
}

/**
 * What is typed and not saved is never lost without asking (user, 06/10/2026). Every form says
 * itself here while it exists (`watch` / `unwatch`); then leaving the page, logging out or closing
 * a form popup first asks "Hai modifiche non salvate" (`app-unsaved-changes-popup` of the layout),
 * and closing or reloading the browser tab gets the browser's own question.
 * When the session ends without the user's choice (expired, another user signed in) nothing is
 * asked: the data of that session must go.
 */
@Injectable({ providedIn: 'root' })
export class UnsavedChangesBehaviour implements OnDestroy {
  private readonly window = inject(DOCUMENT).defaultView;

  private readonly forms = new Set<UnsavedChanges>();
  private pending: ((leave: boolean) => void) | null = null;

  /** The question is on screen, until `answer`. */
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

  /** Before leaving every form (another page, logout): true at once when nothing is unsaved. */
  confirmLeave(): Promise<boolean> {
    return this.anyUnsaved() ? this.ask() : Promise.resolve(true);
  }

  /** Before closing one form (a popup). */
  confirmDiscard(form: UnsavedChanges): Promise<boolean> {
    return form.hasUnsavedChanges() ? this.ask() : Promise.resolve(true);
  }

  /** From the popup: true to leave without saving, false to stay. */
  answer(leave: boolean): void {
    const pending = this.pending;
    this.pending = null;
    this.asking.set(false);
    pending?.(leave);
  }

  private ask(): Promise<boolean> {
    this.answer(false); // a question still open counts as "stay"
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
