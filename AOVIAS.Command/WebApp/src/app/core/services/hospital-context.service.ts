import { Injectable, signal, computed, inject } from '@angular/core';
import { Hospital } from '../../shared/ui/hospital-selector-modal/hospital-selector-modal.component';

@Injectable({
  providedIn: 'root',
})
export class HospitalContextService {
  private readonly hospitals = signal<Hospital[]>([
    {
      id: 'apollo-chennai',
      name: 'Apollo Hospitals, Chennai',
      organizationName: 'Apollo Healthcare Group',
    },
    {
      id: 'fortis-mumbai',
      name: 'Fortis Healthcare, Mumbai',
      organizationName: 'Fortis Healthcare Network',
    },
    {
      id: 'max-delhi',
      name: 'Max Healthcare, Delhi',
      organizationName: 'Max Healthcare Institute',
    },
    {
      id: 'manipal-bangalore',
      name: 'Manipal Hospitals, Bangalore',
      organizationName: 'Manipal Health Enterprises',
    },
  ]);

  private readonly selectedHospitalId = signal<string>('apollo-chennai');

  readonly availableHospitals = computed(() => this.hospitals());

  readonly currentHospital = computed(() => {
    const id = this.selectedHospitalId();
    const hospital = this.hospitals().find((h) => h.id === id);
    return hospital || this.hospitals()[0];
  });

  setCurrentHospital(hospitalId: string): void {
    const hospital = this.hospitals().find((h) => h.id === hospitalId);
    if (hospital) {
      this.selectedHospitalId.set(hospitalId);
      this.persistHospitalSelection(hospitalId);
      this.triggerDataRefresh();
    }
  }

  private persistHospitalSelection(hospitalId: string): void {
    try {
      localStorage.setItem('praman_selected_hospital', hospitalId);
    } catch (error) {
      console.error('Failed to persist hospital selection:', error);
    }
  }

  private triggerDataRefresh(): void {
    window.dispatchEvent(
      new CustomEvent('hospital-changed', {
        detail: { hospitalId: this.selectedHospitalId() },
      }),
    );
  }

  loadPersistedHospital(): void {
    try {
      const stored = localStorage.getItem('praman_selected_hospital');
      if (stored && this.hospitals().some((h) => h.id === stored)) {
        this.selectedHospitalId.set(stored);
      }
    } catch (error) {
      console.error('Failed to load persisted hospital:', error);
    }
  }

  initializeFromApi(hospitals: Hospital[]): void {
    if (hospitals && hospitals.length > 0) {
      this.hospitals.set(hospitals);
      this.loadPersistedHospital();
    }
  }
}
