import { ManagementRequest } from '../management/management-request';
import { PageRequest } from '../pagination/page';

export type ExistingBookingListRequest = Pick<ManagementRequest, 'idProperty'> & PageRequest;
