# Cursor Prompt: Country-to-State Mapping in Enrollment Activation Wizard

## Task
In the enrollment activation wizard component, implement dynamic state/region dropdown population based on country selection. When a user selects a country, the state/region dropdown should automatically populate with the appropriate states/regions for that country.

## Implementation Requirements

1. **Create a country-to-state mapping service/data structure:**
   - Define a mapping object/constant that maps country codes/names to their respective states/regions
   - Example structure: `{ "US": ["Alabama", "Alaska", ...], "IN": ["Andhra Pradesh", "Assam", ...], "CA": ["Alberta", "British Columbia", ...] }`
   - Consider using ISO country codes (e.g., "US", "IN", "CA") for consistency

2. **Update the activation wizard component:**
   - Add a `(change)` or `(selectionChange)` event handler on the country dropdown/select
   - On country selection:
     - Clear the current state/region selection
     - Filter/load states based on selected country
     - Enable the state/region dropdown if states are available
     - Disable the state/region dropdown if no states are available for the selected country

3. **Form validation:**
   - Ensure state/region is required only if the selected country has states/regions
   - Update form validators dynamically based on country selection

4. **Data binding:**
   - The form should bind to `UpdateJurisdictionRequestDto` structure: `{ country: string, region?: string, force: boolean }`
   - Ensure the country and region values match the backend API expectations

## Example Implementation Pattern

```typescript
// In component.ts
onCountryChange(selectedCountry: string): void {
  this.selectedCountry = selectedCountry;
  this.availableStates = this.countryStateMap[selectedCountry] || [];
  this.activationForm.patchValue({ region: null }); // Clear region
  // Update region validator based on whether states are available
}
```

## Notes
- Use reactive forms (FormBuilder/FormGroup) for proper state management
- Consider loading country-state mappings from a service/API if the list is large
- Handle edge cases: countries without states, invalid country selections, etc.
