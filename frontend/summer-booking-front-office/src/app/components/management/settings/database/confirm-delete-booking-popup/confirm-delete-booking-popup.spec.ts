import { HttpErrorResponse } from '@angular/common/http';
import { Component, signal } from '@angular/core';
import { TestBed } from '@angular/core/testing';
import { MATERIAL_ANIMATIONS } from '@angular/material/core';
import { provideTranslateService } from '@ngx-translate/core';
import { Subject, throwError } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { DatabaseService } from '../../../../../services/api/database/database.service';
import { ConfirmDeleteBookingPopup } from './confirm-delete-booking-popup';

@Component({
  imports: [ConfirmDeleteBookingPopup],
  template: `<app-confirm-delete-booking-popup
    [idBookings]="idBookings"
    [(open)]="open"
    (confirmed)="confirmed.push($event)"
  />`,
})
class ConfirmDeleteBookingPopupHost {
  readonly open = signal(true);
  readonly idBookings = ['b1', 'b2'];
  readonly confirmed: (readonly string[])[] = [];
}

describe('ConfirmDeleteBookingPopup', () => {
  const setup = async () => {
    const moveToTrash = vi.fn();
    TestBed.configureTestingModule({
      providers: [
        provideTranslateService(),
        { provide: MATERIAL_ANIMATIONS, useValue: { animationsDisabled: true } },
        { provide: DatabaseService, useValue: { moveToTrash } },
        { provide: AuthBehaviour, useValue: { user: signal({ propertyId: 'p1' }) } },
      ],
    });
    const fixture = TestBed.createComponent(ConfirmDeleteBookingPopupHost);
    await fixture.whenStable();
    const popup = () => document.querySelector<HTMLElement>('.confirm__popup');
    const input = () =>
      popup()!.querySelector<HTMLInputElement>('.confirm__delete__booking__popup__password input')!;
    const confirm = () =>
      popup()!.querySelectorAll<HTMLButtonElement>('.confirm__popup__actions button')[1];
    const settle = async () => {
      await new Promise((resolve) => setTimeout(resolve));
      await fixture.whenStable();
    };
    const type = async (text: string) => {
      input().value = text;
      input().dispatchEvent(new Event('input'));
      await fixture.whenStable();
    };
    return {
      fixture,
      host: fixture.componentInstance,
      popup,
      input,
      confirm,
      settle,
      type,
      moveToTrash,
    };
  };

  afterEach(() => document.querySelector('.cdk-overlay-container')?.replaceChildren());

  it('should send the property, the chosen bookings and the password, then close and tell the page', async () => {
    const { host, popup, input, confirm, settle, type, moveToTrash } = await setup();
    const answer = new Subject<void>();
    moveToTrash.mockReturnValue(answer);

    confirm().click();
    await settle();
    expect(moveToTrash).not.toHaveBeenCalled();

    await type('segreta');
    confirm().click();
    await settle();
    expect(moveToTrash).toHaveBeenCalledWith('p1', {
      idBookings: ['b1', 'b2'],
      password: 'segreta',
    });
    expect(input().readOnly).toBe(true);
    confirm().click();
    expect(moveToTrash).toHaveBeenCalledTimes(1);

    answer.next();
    answer.complete();
    await settle();
    expect(host.open()).toBe(false);
    expect(host.confirmed).toEqual([['b1', 'b2']]);
    expect(popup()).toBeNull();
  });

  it('should show a wrong password under the field and keep the popup open', async () => {
    const { host, popup, confirm, settle, type, moveToTrash } = await setup();
    moveToTrash.mockReturnValue(
      throwError(
        () =>
          new HttpErrorResponse({
            status: 400,
            error: {
              status: 400,
              code: 'validation.invalid_request',
              errors: [{ field: 'password', code: 'auth.invalid_credentials' }],
            },
          }),
      ),
    );

    await type('sbagliata');
    confirm().click();
    await settle();
    // The message of the code (here the fallback key: the test has no translations).
    expect(popup()!.querySelector('.filled__text__field__error')?.textContent?.trim()).toBe(
      'error.unknown',
    );
    expect(popup()!.querySelector('.confirm__popup__error')).toBeNull();
    expect(host.open()).toBe(true);
    expect(host.confirmed).toEqual([]);
  });

  it('should forget the typed password when closed', async () => {
    const { fixture, host, input, type } = await setup();
    await type('segreta');

    host.open.set(false);
    await fixture.whenStable();
    host.open.set(true);
    await fixture.whenStable();
    expect(input().value).toBe('');
  });
});
