import { Component, computed, inject, signal } from '@angular/core';
import { CommonModule } from '@angular/common';
import {
  CalibrationCurveDto,
  CalibrationCurvesApiService,
} from './services/calibration-curves-api.service';
import { ZardTableImports } from '@/shared/components/table/table.imports';
import { ZardSkeletonComponent } from '@/shared/components/skeleton/skeleton.component';
import { ZardAlertComponent } from '@/shared/components/alert/alert.component';
import { ZardEmptyComponent } from '@/shared/components/empty/empty.component';
import {
  ZardCardComponent,
  ZardCardHeaderComponent,
  ZardCardTitleComponent,
  ZardCardDescriptionComponent,
  ZardCardContentComponent,
} from '@/shared/components/card/card.component';
import { ZardButtonComponent } from '@/shared/components/button/button.component';
import { CalibrationChartComponent } from './calibration-chart.component';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideChartSpline, lucideLock } from '@ng-icons/lucide';

@Component({
  selector: 'lims-calibration-curves',
  standalone: true,
  imports: [
    CommonModule,
    ...ZardTableImports,
    ZardSkeletonComponent,
    ZardAlertComponent,
    ZardEmptyComponent,
    ZardCardComponent,
    ZardCardHeaderComponent,
    ZardCardTitleComponent,
    ZardCardDescriptionComponent,
    ZardCardContentComponent,
    ZardButtonComponent,
    CalibrationChartComponent,
    NgIcon,
  ],
  templateUrl: './calibration-curves.component.html',
  viewProviders: [provideIcons({ lucideChartSpline, lucideLock })],
})
export class CalibrationCurvesComponent {
  private apiService = inject(CalibrationCurvesApiService);

  loading = signal(false);
  error = signal('');
  forbidden = signal(false);
  curves = signal<CalibrationCurveDto[]>([]);
  selectedCurveId = signal<number | string | null>(null);

  selectedCurve = computed(
    () => this.curves().find((c) => Number(c.id) === Number(this.selectedCurveId())) ?? null,
  );

  ngOnInit(): void {
    this.loadCurves();
  }

  private loadCurves(): void {
    this.loading.set(true);
    this.error.set('');
    this.forbidden.set(false);
    this.apiService.listCurves().subscribe({
      next: (data) => {
        this.curves.set(data);
        this.selectedCurveId.set(data.length ? data[0].id : null);
        this.loading.set(false);
      },
      error: (err) => {
        this.loading.set(false);
        if (err.status === 403) {
          this.forbidden.set(true);
        } else {
          this.error.set('Failed to load calibration curves. Please try again.');
        }
      },
    });
  }

  reload(): void {
    this.loadCurves();
  }

  selectCurve(curve: CalibrationCurveDto): void {
    this.selectedCurveId.set(curve.id);
  }
}
