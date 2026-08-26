import { Component, inject } from '@angular/core';
import { CommonModule } from '@angular/common';
import { Router } from '@angular/router';
import { NgIcon, provideIcons } from '@ng-icons/core';
import { lucideTriangleAlert } from '@ng-icons/lucide';
import { ZardButtonComponent } from '../components/button/button.component';
import { ZardCardComponent, ZardCardContentComponent } from '../components/card/card.component';

@Component({
  selector: 'lims-error-fallback',
  standalone: true,
  imports: [CommonModule, NgIcon, ZardButtonComponent, ZardCardComponent, ZardCardContentComponent],
  templateUrl: './error-fallback.component.html',
  styleUrl: './error-fallback.component.scss',
  viewProviders: [provideIcons({ lucideTriangleAlert })],
})
export class ErrorFallbackComponent {
  private router = inject(Router);

  goHome() {
    this.router.navigate(['/analysis']);
  }
}
