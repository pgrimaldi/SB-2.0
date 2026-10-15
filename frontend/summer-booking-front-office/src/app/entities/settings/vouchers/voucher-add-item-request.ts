import { DiscountScope } from '../../enums/discount-scope';
import { DiscountType } from '../../enums/discount-type';
import { VoucherValidityPeriod } from './voucher-validity-period';

export class VoucherAddItemRequest {
  voucherName: string | null = null;
  validityPeriods: VoucherValidityPeriod[] = [];
  discountType: DiscountType = DiscountType.Percentage;
  discountValue: number | null = null;
  discountScope: DiscountScope = DiscountScope.All;
  maxUseQuantity: number | null = 1;
  isEnabled = true;
}
