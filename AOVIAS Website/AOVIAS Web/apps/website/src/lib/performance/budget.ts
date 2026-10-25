import { performanceBudgets } from '@/config/performance';

export type BudgetCheckResult = {
  name: string;
  actualKb: number;
  budgetKb: number;
  ok: boolean;
  overByKb: number;
};

/**
 * Compare an actual transfer size (KB) against a named budget.
 */
export function checkBudget(
  name: keyof typeof performanceBudgets,
  actualKb: number,
): BudgetCheckResult {
  const budgetKb = performanceBudgets[name];
  const overByKb = Math.max(0, actualKb - budgetKb);
  return {
    name,
    actualKb,
    budgetKb,
    ok: actualKb <= budgetKb,
    overByKb,
  };
}

/**
 * Evaluate multiple measurements; returns failing checks.
 */
export function findBudgetViolations(
  measurements: Partial<Record<keyof typeof performanceBudgets, number>>,
): BudgetCheckResult[] {
  return (Object.keys(measurements) as Array<keyof typeof performanceBudgets>)
    .map((key) => {
      const actual = measurements[key];
      if (actual === undefined) return null;
      return checkBudget(key, actual);
    })
    .filter((result): result is BudgetCheckResult => result !== null && !result.ok);
}

export function formatBudgetReport(results: readonly BudgetCheckResult[]): string {
  if (results.length === 0) return 'All measured budgets within limits.';
  return results
    .map(
      (r) =>
        `✗ ${r.name}: ${r.actualKb.toFixed(1)} KB > budget ${r.budgetKb} KB (+${r.overByKb.toFixed(1)} KB)`,
    )
    .join('\n');
}

export { performanceBudgets };
