import {
  ChangeDetectionStrategy,
  Component,
  computed,
  effect,
  inject,
  input,
  model,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, TreeValidationResult, form, readonly, submit } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { MoveToTrashRequest } from '../../../../../entities/database/move-to-trash-request';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { DatabaseService } from '../../../../../services/api/database/database.service';
import { requiredText } from '../../../../shared/forms/validators';
import { ConfirmPopup } from '../../../../shared/ui/dialogs/confirm-popup/confirm-popup';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';

@Component({
  selector: 'app-confirm-delete-booking-popup',
  imports: [ConfirmPopup, FilledTextField, FormField, TranslatePipe],
  templateUrl: './confirm-delete-booking-popup.html',
  styleUrl: './confirm-delete-booking-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class ConfirmDeleteBookingPopup {
  readonly open = model(false);
  readonly idBookings = input<readonly string[]>([]);
  readonly confirmed = output<readonly string[]>();

  private readonly database = inject(DatabaseService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly closeIcon = ['/assets/images/Database_Close.svg'] as const;
  protected readonly eyeIcons = [
    '/assets/images/Database_EyeClose.svg',
    '/assets/images/Database_EyeStart.svg',
  ] as const;
  protected readonly username = computed(() => this.auth.user()?.email ?? '');
  protected readonly isLoading = signal(false);
  private readonly request = signal(new MoveToTrashRequest());
  protected readonly requestForm = form(this.request, (path) => {
    requiredText(path.password);
    readonly(path, () => this.isLoading());
  });
  protected readonly passwordError = this.validationText.message(this.requestForm.password);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    // The typed password never stays in memory once the popup is closed.
    effect(() => {
      if (!this.open()) {
        untracked(() => this.reset());
      }
    });
  }

  protected confirm(): void {
    const idProperty = this.auth.user()?.idProperty;
    const idBookings = [...this.idBookings()];
    if (this.isLoading() || !idProperty || !idBookings.length) {
      return;
    }
    void submit(this.requestForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(
          this.database.moveToTrash(idProperty, { ...this.request(), idBookings }),
        );
        this.isLoading.set(false);
        this.open.set(false);
        this.confirmed.emit(idBookings);
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.requestForm, (problem) =>
          this.error.set(problem),
        );
      } finally {
        this.isLoading.set(false);
      }
    });
  }

  private reset(): void {
    this.request.set(new MoveToTrashRequest());
    this.requestForm().reset();
    this.error.set(null);
  }
}
