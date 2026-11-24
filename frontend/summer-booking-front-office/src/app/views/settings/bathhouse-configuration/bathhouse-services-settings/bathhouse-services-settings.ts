import { DOCUMENT } from '@angular/common';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import { FormField, disabled, form } from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { Subscription, firstValueFrom, of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../behaviours/errors/error-text.behaviour';
import { fileLinkOf } from '../../../../behaviours/files/file-link';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../behaviours/forms/unsaved-changes.behaviour';
import { DATA_RELOAD } from '../../../../components/shared/ui/data/data-reload';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import { Checkbox } from '../../../../components/shared/ui/checkboxes/checkbox/checkbox';
import { SectionTitle } from '../../../../components/shared/ui/titles/section-title/section-title';
import { BathhouseServiceGroup } from '../../../../entities/enums/bathhouse-service-group';
import { ApiErrorCode } from '../../../../entities/errors/api-error-codes';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { BathhouseServiceItem } from '../../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';
import { onlyFieldsOf } from '../../../../services/api/only-fields-of';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';
import { comparableJson } from '../../../../components/shared/forms/text-comparison';
import { VersionConflictPopup } from '../../../../components/management/version-conflict-popup/version-conflict-popup';

interface ServiceSection {
  readonly group: BathhouseServiceGroup;
  readonly services: readonly ServiceCheckbox[];
}

interface ServiceCheckbox {
  readonly index: number;
  readonly service: BathhouseServiceItem;
  /** `iconUrl` only when it is an https link or ours. */
  readonly iconUrl: string | null;
}

@Component({
  selector: 'app-bathhouse-services-settings',
  imports: [Button, Checkbox, FormField, SectionTitle, TranslatePipe, VersionConflictPopup],
  templateUrl: './bathhouse-services-settings.html',
  styleUrl: './bathhouse-services-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseServicesSettings implements UnsavedChanges, OnDestroy {
  private readonly auth = inject(AuthBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private readonly cache = inject(BathhouseConfigurationCacheBehaviour);
  private readonly catalog = inject(CatalogService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly origin = inject(DOCUMENT).location.origin;

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  protected readonly isEditing = signal(false);
  protected readonly isLoaded = signal(false);
  protected readonly isSaving = signal(false);
  private readonly services = signal<BathhouseServiceItem[]>([]);
  private loaded: BathhouseServiceItem[] = [];
  private loadedSelection = selectionOf([]);
  /** The version of the services read: Salva sends it back. */
  private version: string | null = null;
  protected readonly servicesForm = form(this.services, (path) => {
    disabled(path, () => !this.isEditing() || this.isSaving());
  });
  protected readonly sections = signal<readonly ServiceSection[]>([]);
  protected readonly isLoading = signal(false);
  /** [X]. */
  protected readonly popupIcons = ['/assets/images/GeneralIcon_Close.svg'] as const;
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly isConflictShown = signal(false);
  private reloading?: Subscription;

  constructor() {
    this.unsaved.watch(this);
    effect((onCleanup) => {
      this.dataReload?.();
      if (!this.auth.user()) {
        return;
      }
      const subscription = untracked(() => {
        this.reloading?.unsubscribe();
        return this.fetch();
      });
      onCleanup(() => subscription.unsubscribe());
    });
  }

  hasUnsavedChanges(): boolean {
    return selectionOf(this.services()) !== this.loadedSelection;
  }

  protected edit(): void {
    if (this.isLoaded()) {
      this.isEditing.set(true);
    }
  }

  protected async requestCancel(): Promise<void> {
    if (await this.unsaved.confirmDiscard(this)) {
      this.cancel();
    }
  }

  protected cancel(): void {
    this.services.set(this.loaded);
    this.servicesForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  protected async save(): Promise<void> {
    if (this.isSaving() || !this.isLoaded()) {
      return;
    }
    this.isSaving.set(true);
    this.error.set(null);
    try {
      const sent = this.services();
      const version = await firstValueFrom(
        this.catalog.saveBathhouseConfigurationServices(sent, this.version),
      );
      this.loaded = sent;
      this.loadedSelection = selectionOf(sent);
      this.version = version;
      this.cache.keepServices({ services: sent, version });
      this.servicesForm().reset();
      this.isEditing.set(false);
    } catch (error: unknown) {
      const problem = toApiProblem(error);
      if (problem.code === ApiErrorCode.ResourceVersionConflict) {
        this.isConflictShown.set(true);
        this.reload();
        return;
      }
      this.error.set(problem);
    } finally {
      this.isSaving.set(false);
    }
  }

  private reload(): void {
    this.reloading?.unsubscribe();
    this.cancel();
    this.cache.forgetServices();
    this.reloading = this.fetch();
  }

  /** Modifica and Salva stay off until the services are read. */
  private fetch(): Subscription {
    this.isLoading.set(true);
    this.error.set(null);
    this.isLoaded.set(false);
    return this.cache.services().subscribe({
      next: (answer) => {
        const loaded = answer.services.map((service) =>
          onlyFieldsOf(BathhouseServiceItem, service),
        );
        this.version = answer.version;
        this.loaded = loaded;
        this.loadedSelection = selectionOf(loaded);
        this.services.set(loaded);
        this.servicesForm().reset();
        this.sections.set(sectionsOf(loaded, this.origin));
        this.isLoaded.set(true);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(toApiProblem(error));
        this.isLoading.set(false);
      },
    });
  }

  ngOnDestroy(): void {
    this.reloading?.unsubscribe();
    this.unsaved.unwatch(this);
  }
}

function selectionOf(services: readonly BathhouseServiceItem[]): string {
  return comparableJson(
    services.filter(({ isSelected }) => isSelected).map(({ idService }) => idService),
  );
}

/** The four sections in their order, each with its services in the order of the answer. */
function sectionsOf(services: readonly BathhouseServiceItem[], origin: string): ServiceSection[] {
  return Object.values(BathhouseServiceGroup)
    .map((group) => ({
      group,
      services: services
        .map((service, index) => ({ index, service, iconUrl: fileLinkOf(service.iconUrl, origin) }))
        .filter(({ service }) => service.categoryCode === group),
    }))
    .filter(({ services }) => services.length);
}
