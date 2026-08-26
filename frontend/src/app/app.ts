import { Component } from '@angular/core';
import { RouterOutlet } from '@angular/router';
import { ZardToasterComponent } from './shared/components/toast/toaster.component';

@Component({
  selector: 'app-root',
  imports: [RouterOutlet, ZardToasterComponent],
  templateUrl: './app.html',
  styleUrl: './app.css'
})
export class App {}

