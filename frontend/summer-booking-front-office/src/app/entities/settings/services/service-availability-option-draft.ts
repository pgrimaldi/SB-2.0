import { ServiceAvailabilityOption } from './service-availability-option';

/** An availability in the form: one left empty is not sent, unless it was saved so. */
export class ServiceAvailabilityOptionDraft extends ServiceAvailabilityOption {
  isSaved = false;
}
