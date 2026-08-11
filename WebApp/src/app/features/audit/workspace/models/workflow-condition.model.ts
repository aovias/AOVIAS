/**
 * The predicate language workflow rules are written in.
 *
 * <p>
 * **Why a language rather than functions.** Lifecycle rules lived in four `switch` statements over
 * the stage id — completion, outstanding work, prerequisites and recommendations. Each arm was a
 * hand-written boolean expression, so "a stage is done when…" was only answerable by reading
 * TypeScript, and changing it meant a deploy. A rule has to be *data* before it can be
 * configuration, and data needs a grammar.
 * </p>
 *
 * <p>
 * **Deliberately not a general expression language.** No arithmetic, no comparison operators, no
 * user-supplied code. Five tests over named facts plus three combinators is enough to express every
 * rule the lifecycle actually had, and the ceiling is the point: a configuration payload arriving
 * from a tenant cannot do anything but ask questions about the facts the workspace already reports.
 * </p>
 *
 * <p>
 * Every form is plain JSON, so a condition round-trips through the Configuration Engine's `metadata`
 * column unchanged.
 * </p>
 */

/**
 * How a fact is tested.
 *
 * The distinctions here are the ones the old code made with `===`, `!`, `known()` and `has()`, and
 * they are not interchangeable:
 *
 * - `true` / `false` — strict identity against a boolean. `false` does **not** match `undefined`;
 *   that difference is why an unmet prerequisite stayed silent on a page that had not loaded the
 *   fact, instead of announcing a blockage it could not actually know about.
 * - `known` — the fact was reported at all. "Reported as zero" and "never loaded here" are
 *   different claims and the lifecycle treats them differently.
 * - `positive` — reported and greater than zero. The old `has()`.
 * - `zero` — reported and not greater than zero. The old `known(x) && !has(x)`.
 */
export type WorkflowFactTest = 'true' | 'false' | 'known' | 'positive' | 'zero';

/** A test applied to one named fact. */
export interface WorkflowFactCondition {
  readonly fact: string;
  readonly is: WorkflowFactTest;
}

/** Every branch must hold. An empty list is vacuously true, as in ordinary logic. */
export interface WorkflowAllCondition {
  readonly all: readonly WorkflowCondition[];
}

/** At least one branch must hold. An empty list is false — nothing satisfies it. */
export interface WorkflowAnyCondition {
  readonly any: readonly WorkflowCondition[];
}

export interface WorkflowNotCondition {
  readonly not: WorkflowCondition;
}

export type WorkflowCondition =
  | WorkflowFactCondition
  | WorkflowAllCondition
  | WorkflowAnyCondition
  | WorkflowNotCondition;

/** The facts a condition may interrogate. Values are booleans, counts, or absent. */
export type WorkflowFacts = Readonly<Record<string, boolean | number | undefined>>;

/** Always true — the identity condition, for a rule that applies unconditionally. */
export const ALWAYS: WorkflowCondition = { all: [] };

/**
 * Evaluate a condition against reported facts.
 *
 * An unrecognised test returns `false` rather than throwing. Configuration can be wrong, and a
 * malformed rule should make a stage look unfinished — which is visible and recoverable — rather
 * than break the workspace that renders it.
 */
export function evaluateCondition(condition: WorkflowCondition, facts: WorkflowFacts): boolean {
  if ('all' in condition) {
    return condition.all.every((c) => evaluateCondition(c, facts));
  }
  if ('any' in condition) {
    return condition.any.some((c) => evaluateCondition(c, facts));
  }
  if ('not' in condition) {
    return !evaluateCondition(condition.not, facts);
  }

  const value = facts[condition.fact];

  switch (condition.is) {
    case 'true':
      return value === true;
    case 'false':
      return value === false;
    case 'known':
      return typeof value === 'number' || typeof value === 'boolean';
    case 'positive':
      return typeof value === 'number' && value > 0;
    case 'zero':
      return typeof value === 'number' && !(value > 0);
    default:
      return false;
  }
}

/**
 * Substitute `{factName}` placeholders in a message with reported values.
 *
 * Outstanding items such as "2 finding(s) still open" previously built their text with template
 * literals inside the rule, which is the one part of a rule that cannot survive being moved into
 * data. A placeholder keeps the wording configurable — including its language — while the number
 * still comes from the facts.
 *
 * An unknown or unreported placeholder is left as written rather than rendered as `undefined`, so a
 * typo in configuration is legible instead of alarming.
 */
export function formatWorkflowMessage(message: string, facts: WorkflowFacts): string {
  return message.replace(/\{(\w+)\}/g, (whole, key: string) => {
    const value = facts[key];
    return value === undefined ? whole : String(value);
  });
}

/**
 * Narrow unknown JSON to a condition, or `null` when it is not one.
 *
 * Configuration is untrusted input: it arrives from an API and may have been edited by hand. A
 * definition carrying an unparseable condition is rejected at the boundary so the seed stays in
 * place, rather than being admitted and silently evaluating to `false` — which would read as "this
 * stage is never complete" and look like a product defect rather than a configuration error.
 */
export function parseWorkflowCondition(raw: unknown): WorkflowCondition | null {
  if (!raw || typeof raw !== 'object' || Array.isArray(raw)) return null;
  const node = raw as Record<string, unknown>;

  if ('all' in node || 'any' in node) {
    const key = 'all' in node ? 'all' : 'any';
    const branches = node[key];
    if (!Array.isArray(branches)) return null;

    const parsed: WorkflowCondition[] = [];
    for (const branch of branches) {
      const child = parseWorkflowCondition(branch);
      if (!child) return null;
      parsed.push(child);
    }
    return key === 'all' ? { all: parsed } : { any: parsed };
  }

  if ('not' in node) {
    const child = parseWorkflowCondition(node['not']);
    return child ? { not: child } : null;
  }

  if (typeof node['fact'] === 'string' && isFactTest(node['is'])) {
    return { fact: node['fact'], is: node['is'] };
  }

  return null;
}

function isFactTest(value: unknown): value is WorkflowFactTest {
  return (
    value === 'true' ||
    value === 'false' ||
    value === 'known' ||
    value === 'positive' ||
    value === 'zero'
  );
}
