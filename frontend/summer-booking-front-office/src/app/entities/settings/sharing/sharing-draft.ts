import { SharingSaveInformationRequest } from './sharing-save-information-request';

/** The settings in the form: the minimum of days has its own switch. */
export class SharingDraft extends SharingSaveInformationRequest {
  isMinBookingDaysEnabled = false;
}
