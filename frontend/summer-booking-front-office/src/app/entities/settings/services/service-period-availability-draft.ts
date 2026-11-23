import { ServiceAvailabilityOptionDraft } from './service-availability-option-draft';
import { ServicePeriodAvailability } from './service-period-availability';

export class ServicePeriodAvailabilityDraft extends ServicePeriodAvailability {
  override availabilityOptions: ServiceAvailabilityOptionDraft[] = [];
}
