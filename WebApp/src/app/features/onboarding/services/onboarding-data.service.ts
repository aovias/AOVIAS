import { Injectable } from '@angular/core';

@Injectable({
  providedIn: 'root'
})
export class OnboardingDataService {
  getHospitalTypes(): string[] {
    return [
      'Multi Specialty Hospital',
      'Clinic',
      'Diagnostic Center',
      'Laboratory',
      'Specialty Hospital',
      'Primary Health Center'
    ];
  }

  getBedCapacityOptions(): string[] {
    return [
      'Less than 50',
      '50-100',
      '101-200',
      '201-500',
      '500+'
    ];
  }
}
