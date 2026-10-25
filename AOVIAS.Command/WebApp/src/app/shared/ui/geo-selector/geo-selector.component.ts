import { CommonModule } from '@angular/common';
import { ChangeDetectionStrategy, Component, forwardRef, inject, Input, OnDestroy, OnInit } from '@angular/core';
import {
  ControlValueAccessor,
  FormBuilder,
  FormControl,
  ReactiveFormsModule,
  Validators,
} from '@angular/forms';
import { MatAutocompleteModule } from '@angular/material/autocomplete';
import { MatFormFieldModule } from '@angular/material/form-field';
import { MatInputModule } from '@angular/material/input';
import { MatOptionModule } from '@angular/material/core';
import { MatSelectModule } from '@angular/material/select';
import { NG_VALUE_ACCESSOR } from '@angular/forms';
import { combineLatest, map, Observable, startWith, Subject, takeUntil } from 'rxjs';

import { GEO_CONFIG } from '../../../core/config/geo.config';
import { GeoSelectionValue, GeoStateOption } from '../../models/geo.models';
import { GeoSelectorService } from '../../services/geo-selector.service';

export type CountryMode = 'hidden' | 'disabled' | 'enabled';

@Component({
  selector: 'app-geo-selector',
  standalone: true,
  imports: [
    CommonModule,
    ReactiveFormsModule,
    MatFormFieldModule,
    MatSelectModule,
    MatInputModule,
    MatAutocompleteModule,
    MatOptionModule,
  ],
  templateUrl: './geo-selector.component.html',
  styleUrl: './geo-selector.component.scss',
  changeDetection: ChangeDetectionStrategy.OnPush,
  providers: [
    {
      provide: NG_VALUE_ACCESSOR,
      useExisting: forwardRef(() => GeoSelectorComponent),
      multi: true,
    },
  ],
})
export class GeoSelectorComponent implements ControlValueAccessor, OnInit, OnDestroy {
  private fb = inject(FormBuilder);
  private geoService = inject(GeoSelectorService);
  private config = inject(GEO_CONFIG);
  private destroy$ = new Subject<void>();

  @Input() required = true;
  @Input() countryMode?: CountryMode;

  readonly countryCodeCtrl = new FormControl<string>(this.config.defaultCountryCode, {
    nonNullable: true,
  });
  readonly stateCodeCtrl = new FormControl<string>('', { nonNullable: true });
  readonly cityNameCtrl = new FormControl<string>('', { nonNullable: true });

  readonly form = this.fb.group({
    countryCode: this.countryCodeCtrl,
    stateCode: this.stateCodeCtrl,
    cityName: this.cityNameCtrl,
  });

  states: GeoStateOption[] = [];
  cities: string[] = [];

  filteredCityOptions$!: Observable<string[]>;

  get effectiveCountryMode(): CountryMode {
    if (this.countryMode) {
      return this.countryMode;
    }
    return this.config.allowCountrySelection ? 'enabled' : 'disabled';
  }

  private onChange: (value: GeoSelectionValue | null) => void = () => {};
  private onTouched: () => void = () => {};

  ngOnInit(): void {
    if (this.required) {
      this.stateCodeCtrl.addValidators([Validators.required]);
      this.cityNameCtrl.addValidators([Validators.required]);
      this.stateCodeCtrl.updateValueAndValidity({ emitEvent: false });
      this.cityNameCtrl.updateValueAndValidity({ emitEvent: false });
    }

    // Load initial state options for default country (India-first).
    this.states = this.geoService.getStates(this.countryCodeCtrl.value);

    // When state changes, load cities on-demand and reset city.
    this.stateCodeCtrl.valueChanges
      .pipe(startWith(this.stateCodeCtrl.value), takeUntil(this.destroy$))
      .subscribe((stateCode) => {
        const sc = (stateCode || '').trim();
        this.cities = sc ? this.geoService.getCities(sc, this.countryCodeCtrl.value).map((c) => c.name) : [];
        this.cityNameCtrl.setValue('', { emitEvent: false });
        this.cityNameCtrl.markAsUntouched();
        this.cityNameCtrl.updateValueAndValidity({ emitEvent: false });
      });

    // Autocomplete filtering, capped.
    this.filteredCityOptions$ = combineLatest([
      this.cityNameCtrl.valueChanges.pipe(startWith(this.cityNameCtrl.value)),
    ]).pipe(
      map(([input]) => {
        const q = (input || '').trim().toLowerCase();
        if (q.length < this.config.cityAutocompleteMinChars) {
          return [];
        }
        const matches = this.cities.filter((c) => c.toLowerCase().includes(q));
        return matches.slice(0, this.config.maxCityOptions);
      }),
    );

    // Emit outbound contract via CVA.
    this.form.valueChanges.pipe(takeUntil(this.destroy$)).subscribe(() => {
      const value: GeoSelectionValue = {
        countryCode: this.countryCodeCtrl.value,
        stateCode: this.stateCodeCtrl.value,
        cityName: this.cityNameCtrl.value,
      };
      // Emit `null` when invalid so parent required validators work cleanly.
      this.onChange(this.form.valid ? value : null);
    });
  }

  writeValue(value: GeoSelectionValue | null): void {
    if (!value) {
      this.form.reset(
        {
          countryCode: this.config.defaultCountryCode,
          stateCode: '',
          cityName: '',
        },
        { emitEvent: false },
      );
      this.states = this.geoService.getStates(this.countryCodeCtrl.value);
      this.cities = [];
      return;
    }

    this.countryCodeCtrl.setValue(value.countryCode || this.config.defaultCountryCode, { emitEvent: false });
    this.states = this.geoService.getStates(this.countryCodeCtrl.value);
    this.stateCodeCtrl.setValue(value.stateCode || '', { emitEvent: false });
    this.cities = value.stateCode
      ? this.geoService.getCities(value.stateCode, this.countryCodeCtrl.value).map((c) => c.name)
      : [];
    this.cityNameCtrl.setValue(value.cityName || '', { emitEvent: false });
  }

  registerOnChange(fn: (value: GeoSelectionValue | null) => void): void {
    this.onChange = fn;
  }

  registerOnTouched(fn: () => void): void {
    this.onTouched = fn;
  }

  setDisabledState(isDisabled: boolean): void {
    if (isDisabled) {
      this.form.disable({ emitEvent: false });
    } else {
      this.form.enable({ emitEvent: false });
      if (this.effectiveCountryMode !== 'enabled') {
        this.countryCodeCtrl.disable({ emitEvent: false });
      }
    }
  }

  markTouched(): void {
    this.onTouched();
  }

  ngOnDestroy(): void {
    this.destroy$.next();
    this.destroy$.complete();
  }
}

