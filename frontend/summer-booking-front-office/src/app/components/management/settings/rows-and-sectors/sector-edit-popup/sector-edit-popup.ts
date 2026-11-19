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
import {
  apiFieldError,
  apiFieldErrorsOrGeneral,
} from '../../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../../behaviours/validation/validation-text.behaviour';
import { ApiErrorCode } from '../../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../../entities/errors/api-problem';
import { Sector } from '../../../../../entities/settings/rows-and-sectors/sector';
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
import { sameText } from '../../../../shared/forms/text-comparison';

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
  /** Every error of the save, e.g. for the page to reload the table on a 404. */
  readonly failed = output<ApiProblem>();

  private readonly sectors = inject(SectorService);
  private readonly auth = inject(AuthBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly isLoading = signal(false);
  private readonly draft = signal(new Sector());
  protected readonly sectorForm = form(this.draft, (path) => {
    requiredText(path.name);
    metadata(path.name, MAX_LENGTH, () => MAX_SECTOR_NAME_LENGTH);
    readonly(path, () => this.isLoading());
  });
  protected readonly nameError = this.validationText.message(this.sectorForm.name);
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
    return this.open() && !this.isUnchanged();
  }

  protected save(): void {
    const idSector = this.sector()?.idSector;
    if (this.isLoading() || !this.auth.user()?.propertyId || !idSector || !this.canSave()) {
      return;
    }
    if (this.isUnchanged()) {
      this.close();
      return;
    }
    void submit(this.sectorForm, async (): Promise<TreeValidationResult> => {
      this.isLoading.set(true);
      this.error.set(null);
      try {
        await firstValueFrom(this.sectors.updateSector(idSector, this.draft().name ?? ''));
        this.isLoading.set(false);
        this.close();
        this.updated.emit();
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        this.failed.emit(problem);
        if (problem.code === ApiErrorCode.CatalogSectorNameTaken) {
          return apiFieldError(this.sectorForm.name, problem.code);
        }
        return apiFieldErrorsOrGeneral(problem, this.sectorForm, (general) =>
          this.error.set(general),
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

  private isUnchanged(): boolean {
    return sameText(this.draft().name, this.original().name);
  }

  private original(): Sector {
    const row = this.sector();
    return { ...new Sector(), id: row?.idSector ?? null, name: row?.sectorName ?? null };
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
