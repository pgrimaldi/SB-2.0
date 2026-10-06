import { ChangeDetectionStrategy, Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ManagementFiltersBehaviour } from '../../behaviours/management/management-filters.behaviour';
import { ManagementHeader } from '../../components/management/header/management-header';
import { ManagementMenu } from '../../components/management/menu/management-menu';
import { UnsavedChangesPopup } from '../../components/management/unsaved-changes-popup/unsaved-changes-popup';

@Component({
  selector: 'app-layout',
  imports: [ManagementHeader, ManagementMenu, RouterOutlet, UnsavedChangesPopup],
  providers: [ManagementFiltersBehaviour],
  templateUrl: './layout.html',
  styleUrl: './layout.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
})
export class Layout {}
