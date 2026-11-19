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
import { firstValueFrom, of, switchMap } from 'rxjs';
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
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { BathhouseServiceItem } from '../../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';
import { onlyFieldsOf } from '../../../../services/api/only-fields-of';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';

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
  imports: [Button, Checkbox, FormField, SectionTitle, TranslatePipe],
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
  protected readonly servicesForm = form(this.services, (path) => {
    disabled(path, () => !this.isEditing() || this.isSaving());
  });
  protected readonly sections = signal<readonly ServiceSection[]>([]);
  protected readonly isLoading = signal(false);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );

  constructor() {
    this.unsaved.watch(this);
    effect((onCleanup) => {
      this.dataReload?.();
      if (!this.auth.user()) {
        return;
      }
      const subscription = untracked(() => {
        this.isLoading.set(true);
        this.error.set(null);
        this.isLoaded.set(false);
        return this.cache.services().subscribe({
          next: (services) => {
            const loaded = services.map((service) => onlyFieldsOf(BathhouseServiceItem, service));
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
      await firstValueFrom(this.catalog.saveBathhouseConfigurationServices(sent));
      this.loaded = sent;
      this.loadedSelection = selectionOf(sent);
      this.cache.keepServices(sent);
      this.servicesForm().reset();
      this.isEditing.set(false);
    } catch (error: unknown) {
      this.error.set(toApiProblem(error));
    } finally {
      this.isSaving.set(false);
    }
  }

  ngOnDestroy(): void {
    this.unsaved.unwatch(this);
  }
}

function selectionOf(services: readonly BathhouseServiceItem[]): string {
  return JSON.stringify(
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
