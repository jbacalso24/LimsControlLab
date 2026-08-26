import { Injectable, Signal, signal } from '@angular/core';
import { environment } from '../../../../../environments/environment';
import { BaseApiService } from '../base-api.service';

@Injectable({
  providedIn: 'root',
})
export abstract class LimsApiService extends BaseApiService {
  override get apiBase(): Signal<string> {
    return signal(environment.limsControlLabApiUrl);
  }
}
