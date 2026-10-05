import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { Subject, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { WarehouseService } from '../../../../../services/api/warehouse/warehouse.service';
import { DeleteWarehouseItemsPopup } from './delete-warehouse-items-popup';

@Component({
  imports: [DeleteWarehouseItemsPopup],
  template: `<app-delete-warehouse-items-popup
    [idItems]="idItems()"
    [itemName]="itemName()"
    [(open)]="open"
    (deleted)="deleted = $event"
  />`,
})
class DeleteWarehouseItemsPopupHost {
  readonly idItems = signal<readonly string[]>(['a1']);
  readonly itemName = signal<string | null>('Lettino');
  readonly open = signal(false);
  deleted: readonly string[] = [];
}

describe('DeleteWarehouseItemsPopup', () => {
  const setup = async (prepare?: (host: DeleteWarehouseItemsPopupHost) => void) => {
    const deleteWarehouseItems = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: WarehouseService, useValue: { deleteWarehouseItems } },
        { provide: AuthBehaviour, useValue: { user: signal({ idProperty: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(DeleteWarehouseItemsPopupHost);
    prepare?.(fixture.componentInstance);
    fixture.componentInstance.open.set(true);
    await fixture.whenStable();
    const dialog = () => document.querySelector<HTMLElement>('[role="alertdialog"]');
    const confirm = async () => {
      dialog()!.querySelectorAll<HTMLButtonElement>('app-button button')[1].click();
      await fixture.whenStable();
    };
    return { fixture, host: fixture.componentInstance, dialog, confirm, deleteWarehouseItems };
  };

  afterEach(() =>
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove()),
  );

  it('should name the article of a row and count the chosen ones', async () => {
    let popup = await setup();
    expect(popup.dialog()!.querySelector('h2')?.textContent?.trim()).toBe(
      'management.settings.warehouse.delete.one.title',
    );
    TestBed.resetTestingModule();
    document.querySelectorAll('.cdk-overlay-container').forEach((overlay) => overlay.remove());

    popup = await setup((host) => {
      host.idItems.set(['a1', 'a2']);
      host.itemName.set(null);
    });
    expect(popup.dialog()!.querySelector('h2')?.textContent?.trim()).toBe(
      'management.settings.warehouse.delete.many.title',
    );
  });

  it('should delete the ids, then close and tell the page which ones', async () => {
    const { fixture, host, dialog, confirm, deleteWarehouseItems } = await setup((page) =>
      page.idItems.set(['a1', 'a2']),
    );
    const answer = new Subject<void>();
    deleteWarehouseItems.mockReturnValue(answer);

    await confirm();
    expect(deleteWarehouseItems).toHaveBeenCalledWith({ idProperty: 'p1', idItems: ['a1', 'a2'] });
    expect(dialog()!.querySelector('mat-progress-spinner')).not.toBeNull();

    answer.next();
    answer.complete();
    await fixture.whenStable();
    expect(host.open()).toBe(false);
    expect(host.deleted).toEqual(['a1', 'a2']);
  });

  it('should stay open with the error when nothing is deleted', async () => {
    const { host, dialog, confirm, deleteWarehouseItems } = await setup();
    deleteWarehouseItems.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: { status: 400, code: 'validation.invalid_request', title: 'Invalid request' },
          }),
      ),
    );

    await confirm();
    expect(host.open()).toBe(true);
    expect(host.deleted).toEqual([]);
    // The message of the code (here the fallback key: the test has no translations).
    expect(dialog()!.querySelector('[role="alert"]')?.textContent?.trim()).toBe('error.unknown');
  });
});
