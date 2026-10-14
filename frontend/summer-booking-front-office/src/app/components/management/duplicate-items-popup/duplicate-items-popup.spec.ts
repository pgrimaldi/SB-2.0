import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { Observable, Subject, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../behaviours/auth/auth.behaviour';
import { DuplicateItemsPopup } from './duplicate-items-popup';

@Component({
  imports: [DuplicateItemsPopup],
  template: `<app-duplicate-items-popup
    [idItems]="idItems()"
    [itemName]="itemName()"
    translationKey="management.settings.warehouse.duplicate"
    [action]="action"
    [(open)]="open"
    (duplicated)="duplicated = $event"
  />`,
})
class DuplicateItemsPopupHost {
  readonly idItems = signal<readonly string[]>(['a1']);
  readonly itemName = signal<string | null>('Lettino');
  readonly open = signal(false);
  action: (idProperty: string, idItems: string[]) => Observable<void> = vi.fn();
  duplicated: readonly string[] = [];
}

describe('DuplicateItemsPopup', () => {
  const setup = async (prepare?: (host: DuplicateItemsPopupHost) => void) => {
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(DuplicateItemsPopupHost);
    prepare?.(fixture.componentInstance);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const dialog = () => document.querySelector<HTMLElement>('mat-dialog-container');
    const confirm = async () => {
      dialog()!.querySelectorAll<HTMLButtonElement>('app-button button')[1].click();
      await fixture.whenStable();
    };
    return { fixture, host: fixture.componentInstance, confirm };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should close at once, wait with the page spinner, then tell the page which ones', async () => {
    const answer = new Subject<void>();
    const action = vi.fn(() => answer.asObservable());
    const { fixture, host, confirm } = await setup((page) => {
      page.idItems.set(['a1', 'a2']);
      page.action = action;
    });

    await confirm();
    expect(action).toHaveBeenCalledWith('p1', ['a1', 'a2']);
    expect(host.open()).toBe(false);
    expect(document.querySelector('.page__spinner')).not.toBeNull();

    answer.next();
    answer.complete();
    await fixture.whenStable();
    expect(document.querySelector('.page__spinner')).toBeNull();
    expect(host.duplicated).toEqual(['a1', 'a2']);
  });

  it('should show the error in a popup when nothing is duplicated', async () => {
    const { host, confirm } = await setup((page) => {
      page.action = () =>
        throwError(
          () =>
            new HttpErrorResponse({
              status: 400,
              error: { status: 400, code: 'validation.failed', title: 'Invalid request' },
            }),
        );
    });

    await confirm();
    expect(host.open()).toBe(false);
    expect(host.duplicated).toEqual([]);
    expect(document.querySelector('.page__spinner')).toBeNull();
    // The message of the code (here the fallback key: the test has no translations).
    expect(document.querySelector('.alert__popup__text')?.textContent?.trim()).toBe(
      'error.unknown',
    );
  });
});
