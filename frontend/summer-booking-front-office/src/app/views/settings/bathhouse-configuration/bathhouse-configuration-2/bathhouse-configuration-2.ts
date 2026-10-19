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
import { of, switchMap } from 'rxjs';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../behaviours/forms/unsaved-changes.behaviour';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import { Checkbox } from '../../../../components/shared/ui/checkboxes/checkbox/checkbox';
import { SectionTitle } from '../../../../components/shared/ui/titles/section-title/section-title';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import { BathhouseServiceItem } from '../../../../entities/settings/bathhouse-configuration/bathhouse-service-item';
import { onlyFieldsOf } from '../../../../services/api/only-fields-of';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';

interface ServiceSection {
  readonly categoryCode: string;
  readonly services: readonly { readonly index: number; readonly service: BathhouseServiceItem }[];
}

@Component({
  selector: 'app-bathhouse-configuration-2',
  imports: [Button, Checkbox, FormField, SectionTitle, TranslatePipe],
  templateUrl: './bathhouse-configuration-2.html',
  styleUrl: './bathhouse-configuration-2.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseConfiguration2 implements UnsavedChanges, OnDestroy {
  private readonly auth = inject(AuthBehaviour);
  private readonly cache = inject(BathhouseConfigurationCacheBehaviour);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;

  protected readonly isEditing = signal(false);
  private readonly services = signal<BathhouseServiceItem[]>([]);
  private loaded: BathhouseServiceItem[] = [];
  private loadedSelection = selectionOf([]);
  protected readonly servicesForm = form(this.services, (path) => {
    disabled(path, () => !this.isEditing());
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
      const idProperty = this.auth.user()?.propertyId;
      if (!idProperty) {
        return;
      }
      const subscription = untracked(() => {
        this.isLoading.set(true);
        this.error.set(null);
        return this.cache.services(idProperty).subscribe({
          next: (services) => {
            const loaded = services.map((service) => onlyFieldsOf(BathhouseServiceItem, service));
            this.loaded = loaded;
            this.loadedSelection = selectionOf(loaded);
            this.services.set(loaded);
            this.servicesForm().reset();
            this.sections.set(sectionsOf(loaded));
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
    this.isEditing.set(true);
  }

  protected cancel(): void {
    this.services.set(this.loaded);
    this.servicesForm().reset();
    this.isEditing.set(false);
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

function sectionsOf(services: readonly BathhouseServiceItem[]): ServiceSection[] {
  const sections = new Map<string, { index: number; service: BathhouseServiceItem }[]>();
  services.forEach((service, index) => {
    const categoryCode = service.categoryCode ?? '';
    sections.set(categoryCode, [...(sections.get(categoryCode) ?? []), { index, service }]);
  });
  return [...sections].map(([categoryCode, items]) => ({ categoryCode, services: items }));
}
