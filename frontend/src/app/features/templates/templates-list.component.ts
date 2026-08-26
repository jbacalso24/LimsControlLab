import { Component, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import { RouterModule } from '@angular/router';
import { FormsModule, ReactiveFormsModule } from '@angular/forms';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { ZardTableComponent, ZardTableHeaderComponent, ZardTableBodyComponent, ZardTableRowComponent, ZardTableHeadComponent, ZardTableCellComponent } from '@/shared/components/table/table.component';
import { ZardCardComponent, ZardCardContentComponent } from '@/shared/components/card/card.component';
import { ZardSpinnerComponent } from '@/shared/components/spinner/spinner.component';
import { ZardEmptyComponent } from '@/shared/components/empty/empty.component';
import { ZardBadgeComponent } from '@/shared/components/badge/badge.component';
import { TemplatesApiService } from './services/templates-api.service';
import { AnalysisTemplateDto } from '../../shared/generated/models/analysis-template-dto';
import { CurrentUserService } from '../../shared/services/auth/current-user.service';
import { NgIcon } from '@ng-icons/core';

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
    NgIcon,
  ],
  templateUrl: './templates-list.component.html',
  styleUrl: './templates-list.component.scss',
})
export class TemplatesListComponent {
  private apiService = inject(TemplatesApiService);
  private currentUserService = inject(CurrentUserService);

  loading = signal(false);
  error = signal('');
  templates = signal<AnalysisTemplateDto[]>([]);
  retiring = signal(false);

  ngOnInit(): void {
    this.loadTemplates();
  }

  private loadTemplates(): void {
    this.loading.set(true);
    this.error.set('');
    this.apiService.listTemplates().subscribe({
      next: (data) => {
        this.templates.set(data);
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
    if (!confirm(`Are you sure you want to retire "${template.name}"?`)) {
      return;
    }

    this.retiring.set(true);
    this.apiService.retireTemplate(Number(template.id)).subscribe({
      next: () => {
        this.retiring.set(false);
        this.loadTemplates();
      },
      error: () => {
        this.retiring.set(false);
        this.error.set('Failed to retire template. Please try again.');
      },
    });
  }
}
