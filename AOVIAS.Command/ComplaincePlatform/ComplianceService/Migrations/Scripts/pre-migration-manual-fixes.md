## Manual Intervention Checklist

- `ComplianceTasks` rows with empty `ObjectiveElementId`:
  - Identify intended OE from audit history and update task linkage manually.
- `Evidences` rows with empty `TaskId`:
  - Reattach evidence to a valid task before enabling unified finding mode.
- `Findings` rows with missing audit linkage (`Scope = Audit` but no journey/OE):
  - Either map to valid journey+OE or move to organization scope.
- `Findings` rows with organization scope but missing tenant:
  - Assign correct tenant/organization ownership.
