import { DOCUMENT } from '@angular/common';
import { HttpStatusCode } from '@angular/common/http';
import {
  ChangeDetectionStrategy,
  Component,
  OnDestroy,
  Signal,
  WritableSignal,
  computed,
  effect,
  inject,
  signal,
  untracked,
} from '@angular/core';
import { toObservable, toSignal } from '@angular/core/rxjs-interop';
import {
  FormField,
  MAX_LENGTH,
  TreeValidationResult,
  form,
  metadata,
  readonly,
  submit,
} from '@angular/forms/signals';
import { TranslatePipe } from '@ngx-translate/core';
import { Observable, Subscription, firstValueFrom, of, switchMap } from 'rxjs';
import { fileLinkOf } from '../../../../behaviours/files/file-link';
import { isUploadable } from '../../../../behaviours/files/uploadable-file';
import { UploadContentType } from '../../../../entities/enums/upload-content-type';
import { ApiErrorCode } from '../../../../entities/errors/api-error-codes';
import {
  IMAGE_UPLOAD_TYPES,
  MAX_IMAGE_UPLOAD_MEGABYTES,
  MEGABYTE,
} from '../../../../entities/shared/image-upload-types';
import { PDF_UPLOAD_TYPES } from '../../../../entities/shared/pdf-upload-types';
import { environment } from '../../../../../environments/environment';
import { AuthBehaviour } from '../../../../behaviours/auth/auth.behaviour';
import { ErrorTextBehaviour } from '../../../../behaviours/errors/error-text.behaviour';
import {
  UnsavedChanges,
  UnsavedChangesBehaviour,
} from '../../../../behaviours/forms/unsaved-changes.behaviour';
import { GoogleMapsBehaviour } from '../../../../behaviours/maps/google-maps.behaviour';
import { DATA_RELOAD } from '../../../../components/shared/ui/data/data-reload';
import { BathhouseConfigurationCacheBehaviour } from '../../../../behaviours/settings/bathhouse-configuration-cache.behaviour';
import { apiFieldErrorsOrGeneral } from '../../../../behaviours/validation/api-field-errors';
import { ValidationTextBehaviour } from '../../../../behaviours/validation/validation-text.behaviour';
import { webLink } from '../../../../components/shared/forms/validators';
import {
  Button,
  ButtonAppearance,
  ButtonSize,
} from '../../../../components/shared/ui/buttons/button/button';
import { IconButton } from '../../../../components/shared/ui/buttons/icon-button/icon-button';
import { ConfirmPopup } from '../../../../components/shared/ui/dialogs/confirm-popup/confirm-popup';
import { FilledTextField } from '../../../../components/shared/ui/inputs/filled-text-field/filled-text-field';
import {
  LocationMap,
  MapPosition,
} from '../../../../components/shared/ui/maps/location-map/location-map';
import { PageSpinner } from '../../../../components/shared/ui/spinners/page-spinner/page-spinner';
import { SectionTitle } from '../../../../components/shared/ui/titles/section-title/section-title';
import { FileDropZone } from '../../../../components/shared/ui/uploads/file-drop-zone/file-drop-zone';
import { BathhouseDocument } from '../../../../entities/enums/bathhouse-document';
import { ApiProblem } from '../../../../entities/errors/api-problem';
import {
  BathhouseMedia,
  MAX_BATHHOUSE_PDF_MEGABYTES,
  MAX_BATHHOUSE_PHOTO_QUANTITY,
  MAX_VIRTUAL_TOUR_URL_LENGTH,
} from '../../../../entities/settings/bathhouse-configuration/bathhouse-media';
import { BathhouseMediaRequest } from '../../../../entities/settings/bathhouse-configuration/bathhouse-media-request';
import { BathhousePhoto } from '../../../../entities/settings/bathhouse-configuration/bathhouse-photo';
import { CatalogService } from '../../../../services/api/catalog/catalog.service';
import { toApiProblem } from '../../../../services/api/errors/to-api-problem';
import { onlyFieldsOf } from '../../../../services/api/only-fields-of';
import { comparableJson } from '../../../../components/shared/forms/text-comparison';
import { VersionConflictPopup } from '../../../../components/management/version-conflict-popup/version-conflict-popup';

const CENTER_OF_ITALY: MapPosition = { latitude: 41.87194, longitude: 12.56738 };
const ITALY_ZOOM = 5;
const POSITION_ZOOM = 15;
/** The backend keeps 6 decimals of latitude and longitude (about 10 cm). */
const COORDINATE_FACTOR = 1_000_000;
const GALLERY_ROW_SLOT_QUANTITY = 4;
/** The same answers the backend gives, before sending a file it would refuse. */
const INVALID_IMAGE: ApiProblem = {
  status: HttpStatusCode.UnprocessableEntity,
  code: ApiErrorCode.CatalogInvalidImage,
  args: { maxBytes: MAX_IMAGE_UPLOAD_MEGABYTES * MEGABYTE },
};
const INVALID_PDF: ApiProblem = {
  status: HttpStatusCode.UnprocessableEntity,
  code: ApiErrorCode.CatalogInvalidPdf,
  args: { maxBytes: MAX_BATHHOUSE_PDF_MEGABYTES * MEGABYTE },
};
const GALLERY_FULL: ApiProblem = {
  status: HttpStatusCode.UnprocessableEntity,
  code: ApiErrorCode.CatalogGalleryFull,
  args: { max: MAX_BATHHOUSE_PHOTO_QUANTITY },
};

/** A call of the tab: each answers with all the media of the property. */
type MediaCall = () => Observable<BathhouseMedia>;

@Component({
  selector: 'app-bathhouse-media-settings',
  imports: [
    Button,
    ConfirmPopup,
    FileDropZone,
    FilledTextField,
    FormField,
    IconButton,
    LocationMap,
    PageSpinner,
    SectionTitle,
    TranslatePipe,
    VersionConflictPopup,
  ],
  templateUrl: './bathhouse-media-settings.html',
  styleUrl: './bathhouse-media-settings.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class BathhouseMediaSettings implements UnsavedChanges, OnDestroy {
  private readonly auth = inject(AuthBehaviour);
  private readonly dataReload = inject(DATA_RELOAD, { optional: true });
  private readonly cache = inject(BathhouseConfigurationCacheBehaviour);
  private readonly catalog = inject(CatalogService);
  private readonly errorText = inject(ErrorTextBehaviour);
  private readonly googleMaps = inject(GoogleMapsBehaviour);
  private readonly unsaved = inject(UnsavedChangesBehaviour);
  private readonly validationText = inject(ValidationTextBehaviour);
  private readonly origin = inject(DOCUMENT).location.origin;

  protected readonly ButtonAppearance = ButtonAppearance;
  protected readonly ButtonSize = ButtonSize;
  protected readonly positionIcon = ['/assets/images/BathhouseConfiguration_Position.svg'] as const;
  protected readonly imagesIcon = ['/assets/images/GeneralIcon_Images.svg'] as const;
  protected readonly addImageIcon = ['/assets/images/GeneralIcon_AddImage.svg'] as const;
  protected readonly virtualTourIcon = [
    '/assets/images/BathhouseConfiguration_VirtualTour.svg',
  ] as const;
  /** [content shown, content hidden, icon before the text]: only the link icon. */
  protected readonly virtualTourFieldIcons = [
    '',
    '',
    '/assets/images/BathhouseConfiguration_VirtualTourLink.svg',
  ] as const;
  protected readonly documentsIcon = [
    '/assets/images/BathhouseConfiguration_Documents.svg',
  ] as const;
  protected readonly uploadIcon = ['/assets/images/BathhouseConfiguration_Upload.svg'] as const;
  protected readonly removeIcon = ['/assets/images/GeneralIcon_Close.svg'] as const;
  /** [X]. */
  protected readonly popupIcons = ['/assets/images/GeneralIcon_Close.svg'] as const;
  protected readonly imageTypes = IMAGE_UPLOAD_TYPES.join(',');
  protected readonly pdfType = UploadContentType.Pdf;
  protected readonly mapId = environment.googleMapsMapId;
  protected readonly isMapReady = signal(false);
  protected readonly isMapFailed = signal(false);
  private loaded = new BathhouseMediaRequest();
  /** The version of the link and of the position read: Salva sends it back. */
  private version: string | null = null;
  private readonly request = signal(new BathhouseMediaRequest());
  protected readonly mediaForm = form(this.request, (path) => {
    webLink(path.virtualTourUrl);
    metadata(path.virtualTourUrl, MAX_LENGTH, () => MAX_VIRTUAL_TOUR_URL_LENGTH);
    readonly(path, () => !this.isEditing() || this.isSaving());
  });
  protected readonly fieldErrors = Object.fromEntries(
    (Object.keys(this.request()) as (keyof BathhouseMediaRequest)[]).map((name) => [
      name,
      this.validationText.message(this.mediaForm[name]),
    ]),
  ) as Readonly<Record<keyof BathhouseMediaRequest, Signal<string | null>>>;
  protected readonly position = computed<MapPosition | null>(() => {
    const { latitude, longitude } = this.request();
    return latitude !== null && longitude !== null ? { latitude, longitude } : null;
  });
  protected readonly center = computed(() => this.position() ?? CENTER_OF_ITALY);
  protected readonly zoom = computed(() => (this.position() ? POSITION_ZOOM : ITALY_ZOOM));
  /** As the server sent them: the links go into `src` and `href` only through `fileLinkOf`. */
  private readonly media = signal(new BathhouseMedia());
  protected readonly mainPhotoUrl = computed(() =>
    fileLinkOf(this.media().mainPhotoUrl, this.origin),
  );
  /** Also with a link that is not safe: shown without preview, it can still be deleted. */
  protected readonly hasMainPhoto = computed(() => this.media().mainPhotoUrl !== null);
  /** A photo without a safe link stays without preview, so it still counts and can be deleted. */
  protected readonly galleryPhotos = computed(() =>
    this.media().photos.map((photo) => ({ ...photo, url: fileLinkOf(photo.url, this.origin) })),
  );
  /** Rows of the gallery always full, with at least one free place until the gallery is full. */
  protected readonly emptyGallerySlots = computed(() => {
    const photoQuantity = this.galleryPhotos().length;
    const slotQuantity = Math.min(
      GALLERY_ROW_SLOT_QUANTITY - (photoQuantity % GALLERY_ROW_SLOT_QUANTITY),
      MAX_BATHHOUSE_PHOTO_QUANTITY - photoQuantity,
    );
    return Array.from({ length: Math.max(slotQuantity, 0) }, (_, index) => index);
  });
  protected readonly documents = computed(() => [
    {
      kind: BathhouseDocument.PriceList,
      name: 'management.settings.bathhouse_configuration.media.documents.price_list',
      url: fileLinkOf(this.media().priceListPdfUrl, this.origin),
      fileName: this.media().priceListPdfFileName,
      isPresent:
        this.media().priceListPdfUrl !== null || this.media().priceListPdfFileName !== null,
    },
    {
      kind: BathhouseDocument.Menu,
      name: 'management.settings.bathhouse_configuration.media.documents.menu',
      url: fileLinkOf(this.media().menuPdfUrl, this.origin),
      fileName: this.media().menuPdfFileName,
      isPresent: this.media().menuPdfUrl !== null || this.media().menuPdfFileName !== null,
    },
  ]);
  protected readonly isLoading = signal(false);
  private readonly error = signal<ApiProblem | null>(null);
  protected readonly errorMessage = toSignal(
    toObservable(this.error).pipe(
      switchMap((problem) => (problem ? this.errorText.text(problem) : of(null))),
    ),
    { initialValue: null },
  );
  protected readonly isEditing = signal(false);
  protected readonly isLoaded = signal(false);
  protected readonly isSaving = signal(false);
  /** A photo or a PDF is being uploaded or deleted: they are sent at once, one at a time. */
  protected readonly isActionRunning = signal(false);
  /** What the X of a photo or of a PDF deletes, until the popup is answered. */
  protected readonly deletion = signal<MediaCall | null>(null);
  protected readonly isDocumentDeletion = signal(false);
  private readonly imagesError = signal<ApiProblem | null>(null);
  protected readonly imagesErrorMessage = this.messageOf(this.imagesError);
  private readonly documentsError = signal<ApiProblem | null>(null);
  protected readonly documentsErrorMessage = this.messageOf(this.documentsError);
  protected readonly isConflictShown = signal(false);
  private reloading?: Subscription;

  constructor() {
    this.unsaved.watch(this);
    this.googleMaps.load().then(
      () => this.isMapReady.set(true),
      () => this.isMapFailed.set(true),
    );
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
    const current = this.request();
    return (Object.keys(this.loaded) as (keyof BathhouseMediaRequest)[]).some(
      (name) => comparableJson(current[name]) !== comparableJson(this.loaded[name]),
    );
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
    this.request.set(this.loaded);
    this.mediaForm().reset();
    this.error.set(null);
    this.isEditing.set(false);
  }

  protected save(): void {
    if (this.isSaving() || !this.isLoaded()) {
      return;
    }
    void submit(this.mediaForm, async (): Promise<TreeValidationResult> => {
      this.isSaving.set(true);
      this.error.set(null);
      try {
        const sent = {
          ...this.request(),
          virtualTourUrl: this.request().virtualTourUrl?.trim() || null,
        };
        const version = await firstValueFrom(
          this.catalog.saveBathhouseConfigurationMedia(sent, this.version),
        );
        this.loaded = sent;
        this.version = version;
        this.request.set(sent);
        this.media.update((media) => ({ ...media, ...sent }));
        this.cache.keepMedia({ ...this.media(), version });
        this.mediaForm().reset();
        this.isEditing.set(false);
      } catch (error: unknown) {
        const problem = toApiProblem(error);
        if (problem.code === ApiErrorCode.ResourceVersionConflict) {
          this.isConflictShown.set(true);
          this.reload();
          return undefined;
        }
        return apiFieldErrorsOrGeneral(problem, this.mediaForm, (general) =>
          this.error.set(general),
        );
      } finally {
        this.isSaving.set(false);
      }
    });
  }

  /** In Modifica a click on the map moves the position of the bathhouse there. */
  protected placePosition({ latitude, longitude }: MapPosition): void {
    if (!this.isEditing() || this.isSaving()) {
      return;
    }
    this.mediaForm.latitude().value.set(roundCoordinate(latitude));
    this.mediaForm.longitude().value.set(roundCoordinate(longitude));
    this.mediaForm.latitude().markAsDirty();
  }

  protected uploadMainPhoto([image]: File[]): void {
    if (!image) {
      return;
    }
    if (!isImage(image)) {
      this.imagesError.set(INVALID_IMAGE);
      return;
    }
    void this.run([() => this.catalog.uploadBathhouseMainPhoto(image)], this.imagesError);
  }

  /** The valid images, as long as the gallery has room; then the reason of those left out. */
  protected addPhotos(images: File[]): void {
    const valid = images.filter(isImage);
    const room = MAX_BATHHOUSE_PHOTO_QUANTITY - this.galleryPhotos().length;
    const leftOut =
      valid.length < images.length ? INVALID_IMAGE : valid.length > room ? GALLERY_FULL : null;
    void this.run(
      valid.slice(0, Math.max(room, 0)).map((image) => () => this.catalog.addBathhousePhoto(image)),
      this.imagesError,
      leftOut,
    );
  }

  protected uploadDocument(document: BathhouseDocument, input: HTMLInputElement): void {
    const [pdf] = Array.from(input.files ?? []);
    input.value = '';
    if (!pdf) {
      return;
    }
    if (!isUploadable(pdf, PDF_UPLOAD_TYPES, MAX_BATHHOUSE_PDF_MEGABYTES)) {
      this.documentsError.set(INVALID_PDF);
      return;
    }
    void this.run([() => this.catalog.uploadBathhouseDocument(document, pdf)], this.documentsError);
  }

  protected askMainPhotoDeletion(): void {
    this.askDeletion(() => this.catalog.deleteBathhouseMainPhoto(), false);
  }

  protected askPhotoDeletion(idPhoto: string | null): void {
    if (idPhoto) {
      this.askDeletion(() => this.catalog.deleteBathhousePhoto(idPhoto), false);
    }
  }

  protected askDocumentDeletion(document: BathhouseDocument): void {
    this.askDeletion(() => this.catalog.deleteBathhouseDocument(document), true);
  }

  protected confirmDeletion(): void {
    const deletion = this.deletion();
    this.deletion.set(null);
    if (deletion) {
      void this.run([deletion], this.isDocumentDeletion() ? this.documentsError : this.imagesError);
    }
  }

  protected closeDeletion(isOpen: boolean): void {
    if (!isOpen) {
      this.deletion.set(null);
    }
  }

  ngOnDestroy(): void {
    this.reloading?.unsubscribe();
    this.unsaved.unwatch(this);
  }

  private askDeletion(call: MediaCall, isDocument: boolean): void {
    this.isDocumentDeletion.set(isDocument);
    this.deletion.set(call);
  }

  /**
   * The calls one after the other, each answer shown at once; the first error stops them. Without
   * errors, `afterwards` (e.g. files left out) is the message of the section.
   */
  private async run(
    calls: readonly MediaCall[],
    error: WritableSignal<ApiProblem | null>,
    afterwards: ApiProblem | null = null,
  ): Promise<void> {
    if (this.isActionRunning()) {
      return;
    }
    this.imagesError.set(null);
    this.documentsError.set(null);
    this.isActionRunning.set(true);
    try {
      for (const call of calls) {
        this.keep(await firstValueFrom(call()));
      }
      error.set(afterwards);
    } catch (caught: unknown) {
      error.set(toApiProblem(caught));
    } finally {
      this.isActionRunning.set(false);
    }
  }

  /**
   * The answer holds all the media as saved, link of the virtual tour and position too. Photos and
   * PDFs do not change the version of Salva.
   */
  private keep(answer: BathhouseMedia): void {
    const media = mediaOf(answer);
    this.media.set(media);
    this.cache.keepMedia({ ...media, version: this.version });
  }

  private reload(): void {
    this.reloading?.unsubscribe();
    this.cancel();
    this.cache.forgetMedia();
    this.reloading = this.fetch();
  }

  /** Modifica and Salva stay off until the media are read. */
  private fetch(): Subscription {
    this.isLoading.set(true);
    this.error.set(null);
    this.isLoaded.set(false);
    return this.cache.media().subscribe({
      next: (answer) => {
        const media = mediaOf(answer);
        this.version = answer.version;
        this.media.set(media);
        this.loaded = onlyFieldsOf(BathhouseMediaRequest, media);
        this.request.set(this.loaded);
        this.mediaForm().reset();
        this.isLoaded.set(true);
        this.isLoading.set(false);
      },
      error: (error: unknown) => {
        this.error.set(toApiProblem(error));
        this.isLoading.set(false);
      },
    });
  }

  private messageOf(problem: Signal<ApiProblem | null>): Signal<string | null> {
    return toSignal(
      toObservable(problem).pipe(
        switchMap((current) => (current ? this.errorText.text(current) : of(null))),
      ),
      { initialValue: null },
    );
  }
}

function mediaOf(answer: BathhouseMedia): BathhouseMedia {
  const media = onlyFieldsOf(BathhouseMedia, answer);
  return {
    ...media,
    photos: (media.photos ?? []).map((photo) => onlyFieldsOf(BathhousePhoto, photo)),
  };
}

function roundCoordinate(value: number): number {
  return Math.round(value * COORDINATE_FACTOR) / COORDINATE_FACTOR;
}

function isImage(file: File): boolean {
  return isUploadable(file, IMAGE_UPLOAD_TYPES, MAX_IMAGE_UPLOAD_MEGABYTES);
}
