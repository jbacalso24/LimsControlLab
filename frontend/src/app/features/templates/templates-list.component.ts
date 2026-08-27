import { Component, inject, signal, computed } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { ZardTableComponent, ZardTableHeaderComponent, ZardTableBodyComponent, ZardTableRowComponent, ZardTableHeadComponent, ZardTableCellComponent } from '@/shared/components/table/table.component';
import { ZardCardComponent, ZardCardContentComponent } from '@/shared/components/card/card.component';
import { ZardSpinnerComponent } from '@/shared/components/spinner/spinner.component';
import { ZardEmptyComponent } from '@/shared/components/empty/empty.component';
import { ZardBadgeComponent } from '@/shared/components/badge/badge.component';
import { ZardPaginationComponent } from '@/shared/components/pagination/pagination.component';
import { ZardDialogService } from '@/shared/components/dialog/dialog.service';
import { DetailDialogComponent, DetailRow } from '@/shared/ui/detail-dialog/detail-dialog.component';
import { ToastService } from '@/shared/services/toast/toast.service';
import { TemplatesApiService } from './services/templates-api.service';
import { AnalysisTemplateDto } from '../../shared/generated/models/analysis-template-dto';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { ActivatedRoute, Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideRefreshCw, lucidePlus, lucidePencil, lucideArchive, lucideAlertCircle } from '@ng-icons/lucide';

@Component({
  selector: 'lims-templates-list',
  standalone: true,
  imports: [
    CommonModule,
    RouterModule,
    FormsModule,
    ReactiveFormsModule,
    ZardButtonComponent,
    ZardTableComponent,
    ZardTableHeaderComponent,
    ZardTableBodyComponent,
    ZardTableRowComponent,
    ZardTableHeadComponent,
    ZardTableCellComponent,
    ZardCardComponent,
    ZardCardContentComponent,
    ZardSpinnerComponent,
    ZardEmptyComponent,
    ZardBadgeComponent,
    ZardPaginationComponent,
    DetailDialogComponent,
    NgIcon,
  ],
  templateUrl: './templates-list.component.html',
  styleUrl: './templates-list.component.scss',
  viewProviders: [provideIcons({ lucideRefreshCw, lucidePlus, lucidePencil, lucideArchive, lucideAlertCircle })],
})
export class TemplatesListComponent {
  private apiService = inject(TemplatesApiService);
  private currentUserService = inject(CurrentUserService);
  private dialog = inject(ZardDialogService);
  private toast = inject(ToastService);
  private router = inject(Router);
  private route = inject(ActivatedRoute);

  loading = signal(false);
  error = signal('');
  templates = signal<AnalysisTemplateDto[]>([]);
  retiring = signal(false);

  pageSize = 10;
  pageIndex = signal(1);
  totalPages = computed(() => Math.max(1, Math.ceil(this.templates().length / this.pageSize)));
  pagedTemplates = computed(() => {
    const start = (this.pageIndex() - 1) * this.pageSize;
    return this.templates().slice(start, start + this.pageSize);
  });

  /** Row selected for the details modal. */
  selectedTemplate = signal<AnalysisTemplateDto | null>(null);

  detailRows(template: AnalysisTemplateDto): DetailRow[] {
    return [
      { label: 'Name', value: template.name },
      { label: 'Site', value: template.site },
      { label: 'Version', value: template.version },
      { label: 'Min Tolerance', value: template.minTolerance },
      { label: 'Max Tolerance', value: template.maxTolerance },
      { label: 'Status', value: template.isRetired ? 'Retired' : 'Active' },
      { label: 'Validation rules', value: template.validationRules, full: true, pre: true },
      { label: 'Calculation definitions', value: template.calculationDefinitions, full: true, pre: true },
    ];
  }

  editTemplate(template: AnalysisTemplateDto): void {
    this.router.navigate(['.', template.id, 'edit'], { relativeTo: this.route });
  }

  ngOnInit(): void {
    this.loadTemplates();
  }

  private loadTemplates(): void {
    this.loading.set(true);
    this.error.set('');
    this.apiService.listTemplates().subscribe({
      next: (data) => {
        this.templates.set(data);
        this.pageIndex.set(1);
        this.loading.set(false);
      },
      error: () => {
        this.loading.set(false);
        this.error.set('Failed to load templates. Please try again.');
      },
    });
  }

  reload(): void {
    this.loadTemplates();
  }

  isLabCoordinator(): boolean {
    return this.currentUserService.user()?.role === 'LabCoordinator';
  }

  retire(template: AnalysisTemplateDto): void {
    this.dialog.create({
      zTitle: `Retire "${template.name}"?`,
      zDescription:
        'Retired templates can no longer be used to start new analyses. Existing analyses keep their template version.',
      zOkText: 'Retire template',
      zOkDestructive: true,
      zCancelText: 'Cancel',
      zOnOk: () => {
        this.retiring.set(true);
        this.apiService.retireTemplate(Number(template.id)).subscribe({
          next: () => {
            this.retiring.set(false);
            this.toast.success(`Template "${template.name}" retired.`);
            this.loadTemplates();
          },
          error: () => {
            this.retiring.set(false);
            this.toast.error(`Could not retire "${template.name}". Please try again.`);
          },
        });
      },
    });
  }
}
