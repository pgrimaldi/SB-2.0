import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  ViewEncapsulation,
  computed,
  effect,
  inject,
  input,
  output,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  TreeValidationResult,
  MAX_LENGTH,
  form,
  metadata,
  readonly,
  submit,
} from '@angular/forms/signals';
import { MatDialogConfig } from '@angular/material/dialog';
import { TranslatePipe } from '@ngx-translate/core';
import { firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../../behaviours/forms/unsaved-changes.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import {
  MAX_SECTOR_NAME_LENGTH,
  SectorItem,
} from '../../../../../entities/settings/rows-and-sectors/sector-item';
import { SectorService } from '../../../../../services/api/catalog/sector.service';
import { toApiProblem } from '../../../../../services/api/errors/to-api-problem';
import { requiredText } from '../../../../shared/forms/validators';
import { Button, ButtonAppearance, ButtonSize } from '../../../../shared/ui/buttons/button/button';
import { BasePopup } from '../../../../shared/ui/dialogs/base-popup/base-popup';
import { FilledTextField } from '../../../../shared/ui/inputs/filled-text-field/filled-text-field';

@Component({
  selector: 'app-sector-edit-popup',
  imports: [Button, FilledTextField, FormField, TranslatePipe],
  templateUrl: './sector-edit-popup.html',
  styleUrl: './sector-edit-popup.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  encapsulation: ViewEncapsulation.None,
})
export class SectorEditPopup extends BasePopup implements UnsavedChanges, OnDestroy {
  /** The row of the pencil, with the name it has now. */
  readonly sector = input<SectorItem | null>(null);
  readonly updated = output<void>();

  private readonly sectors = inject(SectorService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly isLoading = signal(false);
  private readonly draft = signal(new SectorItem());
  protected readonly sectorForm = form(this.draft, (path) => {
    requiredText(path.sectorName);
    metadata(path.sectorName, MAX_LENGTH, () => MAX_SECTOR_NAME_LENGTH);
    readonly(path, () => this.isLoading());
  });
  protected readonly sectorNameError = this.validationText.message(this.sectorForm.sectorName);
  protected readonly canSave = computed(() => this.sectorForm().valid());
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    super();
    this.unsaved.watch(this);
    effect(() => {
      this.open();
      untracked(() => this.reset());
    });
  }

  hasUnsavedChanges(): boolean {
    return this.open() && this.draft().sectorName !== this.original().sectorName;
  }

  protected save(): void {
    const idProperty = this.auth.user()?.propertyId;
    if (this.isLoading() || !idProperty || !this.canSave()) {
      return;
    }
    if (this.draft().sectorName?.trim() === this.original().sectorName) {
      this.close();
      return;
    }
    void submit(this.sectorForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.sectors.updateSectorItem(idProperty, this.draft()));
        this.isLoading.set(false);
        this.close();
        this.updated.emit();
      } catch (error: unknown) {
        return apiFieldErrorsOrGeneral(toApiProblem(error), this.sectorForm, (problem) =>
          this.error.set(problem),
        );
      } finally {
        this.isLoading.set(false);
      }
    });
  }

  protected async leave(): Promise<void> {
    if (!this.isLoading() && (await this.unsaved.confirmDiscard(this))) {
      this.close();
    }
  }

  protected override isCloseDisabled(): boolean {
    return this.isLoading();
  }

  protected override closeRequested(): void {
    void this.leave();
  }

  protected dialogConfig(): MatDialogConfig {
    return {
      width: 'min(25rem, 95vw)',
      maxWidth: 'none',
      panelClass: 'sector__edit__popup__panel',
      disableClose: true,
      autoFocus: 'input',
    };
  }

  private original(): SectorItem {
    return { ...new SectorItem(), ...this.sector() };
  }

  private reset(): void {
    this.draft.set(this.original());
    this.sectorForm().reset();
    this.error.set(null);
  }

  override ngOnDestroy(): void {
    this.unsaved.unwatch(this);
    super.ngOnDestroy();
  }
}
