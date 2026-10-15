import { VoucherValidityPeriod } from './voucher-validity-period';

export class VoucherAddItemRequest {
  voucherName: string | null = null;
  validityPeriods: VoucherValidityPeriod[] = [];
}
